using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatDetection.Core;
using BeatDetection.Game;
using OpenTK;
using OpenTK.Graphics;
using QuickFont;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using OpenTK.Graphics.OpenGL4;

namespace BeatDetection.GUI
{
    class LoadingScene : Scene
    {
        private string _sonicAnnotatorPath;
        private string _pluginPath;
        private float _correction;
        private Task _loadTask;
        private Stage _stage;
        private Player _player;
        private PolarPolygon _centerPolygon;
        private ProcessedText _loadingText;
        private ProcessedText _songText;
        private Vector2 _loadingTextPosition;
        private QFont _loadingFont;
        private ShaderProgram _shaderProgram;
        private VertexBuffer _buffer;
        private VertexArray _vertexArray;
        private BufferDataSpecification _positionSpec;
        private BufferDataSpecification _colorSpec;

        private string _loadingStatus;

        public LoadingScene(string sonicAnnotatorPath, string pluginPath, float correction)
        {
            _sonicAnnotatorPath = sonicAnnotatorPath;
            _pluginPath = pluginPath;
            _correction = correction;
        }

        public override void Load()
        {
            var vert = new Shader(Directories.ShaderDirectory + "/simple.vs");
            var frag = new Shader(Directories.ShaderDirectory + "/simple.fs");
            _shaderProgram = new ShaderProgram();
            _shaderProgram.Load(vert, frag);

            _positionSpec = new BufferDataSpecification
            {
                Count = 2,
                Name = "in_position",
                Offset = 0,
                ShouldBeNormalised = false,
                Stride = 0,
                Type = VertexAttribPointerType.Float
            };
            _colorSpec = new BufferDataSpecification
            {
                Count = 4,
                Name = "in_color",
                Offset = 2,
                ShouldBeNormalised = false,
                Stride = 24,
                Type = VertexAttribPointerType.Float
            };

            var size = 6*4*(2*sizeof (float) + 0*sizeof (float));

            _vertexArray = new VertexArray();
            _vertexArray.Bind();

            _buffer = new VertexBuffer(){BufferUsage = BufferUsageHint.StreamDraw, MaxSize = size, DrawableIndices = 48};
            _buffer.Bind();
            _buffer.Initialise();
            _buffer.DataSpecifications.Add(_positionSpec);
            //_buffer.DataSpecifications.Add(_colorSpec);

            _vertexArray.Load(_shaderProgram, _buffer);

            _player = new Player();
            _player.ShaderProgram = _shaderProgram;
            _centerPolygon =  new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 50, 80, 0 );
            _centerPolygon.ShaderProgram = _shaderProgram;
            _stage = new Stage(_player, _centerPolygon);
            _stage.ShaderProgram = _shaderProgram;

            string file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Audio Files (*.mp3, *.flac, *.wav)|*.mp3;*.flac;*.wav|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                file = ofd.FileName;
                file = file.Replace(@"\", "/");
                //file.Replace("\\", "/");
            }
            else
            {
                SceneManager.RemoveScene(this);
                SceneManager.GameWindow.Exit();
                return;
            }

            _loadingFont = new QFont(SceneManager.FontPath, 30);
            _loadingText = _loadingFont.ProcessText("Loading", 200, QFontAlignment.Centre);
            //_loadingTextPosition = CalculateTextPosition(new Vector2(SceneManager.ScreenCamera.PreferredWidth / 2, SceneManager.ScreenCamera.PreferredHeight / 2), _loadingText);
            _loadingTextPosition = CalculateTextPosition(new Vector2((float)SceneManager.GameWindow.Width/ 2, SceneManager.GameWindow.Height/ 2), _loadingText);

            _songText = _loadingFont.ProcessText(Path.GetFileNameWithoutExtension(file), SceneManager.GameWindow.Width,
                QFontAlignment.Centre);

            var progress = new Progress<string>(status =>
            {
                _loadingStatus = status;
            });
            _loadTask = Task.Factory.StartNew(() => _stage.LoadAsync(file, _sonicAnnotatorPath, _pluginPath, _correction, progress));

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Resize(EventArgs e)
        {
            _loadingText = _loadingFont.ProcessText("Loading", 1000, QFontAlignment.Centre);
            _loadingTextPosition = CalculateTextPosition(new Vector2(SceneManager.ScreenCamera.PreferredWidth / 2, SceneManager.ScreenCamera.PreferredHeight / 2), _loadingText);
        }

        public override void Update(double time, bool focused = false)
        {
            if (_loadTask.IsCompleted)
            {
                _stage.UpdateColours();
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(new GameScene(_stage){ShaderProgram = _shaderProgram});
            }

            _player.Update(time);
            _centerPolygon.Update(time, false);
            //SceneManager.ScreenCamera.TargetScale += new Vector2(0.1f, 0.1f);

            _buffer.Bind();
            _buffer.Initialise();
            _buffer.SetData(_centerPolygon.GetVertices(), _positionSpec);
            _buffer.UnBind();

        }

        public override void Draw(double time)
        {
            ErrorCode errorCode = GL.GetError();
            GL.Disable(EnableCap.CullFace);
            _shaderProgram.Bind();
            _shaderProgram.SetUniform("mvp", SceneManager.ScreenCamera.ModelViewProjection);
            _shaderProgram.SetUniform("in_color", Color4.White);

            //Draw the player
            _player.Draw(time);

            //Draw the center polygon
            _centerPolygon.Draw(time);

            //Cleanup the program
            _shaderProgram.UnBind();

            //SceneManager.ScreenCamera.EnableScreenDrawing();
            //SceneManager.DrawProcessedText(_loadingText, _loadingTextPosition, _loadingFont);
            //SceneManager.DrawProcessedText(_songText, new Vector2(SceneManager.GameWindow.Width/2, SceneManager.GameWindow.Height - 100), _loadingFont);
            //SceneManager.DrawTextLine(_loadingStatus, new Vector2(SceneManager.GameWindow.Width/2, 100), _loadingFont);
            //GL.Disable(EnableCap.Texture2D);
            //SceneManager.ScreenCamera.EnableWorldDrawing();
            //_player.Draw(time);
            //_centerPolygon.Draw(time);
        }

        public override void UnLoad()
        {
        }

        private Vector2 CalculateTextPosition(Vector2 center, ProcessedText text)
        {
            var size = _loadingFont.Measure(text);
            return new Vector2(center.X, center.Y + size.Height/2);
        }
    }
}
