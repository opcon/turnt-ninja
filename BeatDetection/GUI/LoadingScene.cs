using System;
using System.Collections.Generic;
using System.Drawing;
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
        private Vector3 _loadingTextPosition;
        private QFont _loadingFont;
        private ShaderProgram _shaderProgram;

        private ShaderProgram _shaderProgram1;
        Substructio.Graphics.Lines.StraightLine _testLine;
        VertexArray _testVAO;
        VertexBuffer _testVBO1, _testVBO2;

        private string _loadingStatus = "";

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
            var cvert = new Shader(Directories.ShaderDirectory + "/colour.vs");
            _shaderProgram = new ShaderProgram();
            _shaderProgram1 = new ShaderProgram();
            _shaderProgram.Load(vert, frag);
            _shaderProgram1.Load(cvert, frag);

            _player = new Player();
            _player.ShaderProgram = _shaderProgram;
            _centerPolygon =  new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 50, 80, 0 );
            _centerPolygon.ShaderProgram = _shaderProgram;
            _stage = new Stage(this.SceneManager);
            _stage.ShaderProgram = _shaderProgram;

//            _testLine = new Substructio.Graphics.Lines.StraightLine();
//            _testLine.Line(new Vector2(-100, -100), new Vector2(100, 100), 20, Color4.Black, Color4.White, true);
//
//            var _testSpec1 = new BufferDataSpecification
//            {
//                Count = 2,
//                Name = "in_position",
//                Offset = 0,
//                ShouldBeNormalised = false,
//                Stride = 0,
//                Type = VertexAttribPointerType.Float,
//                SizeInBytes = sizeof(float)
//            };
//
//            var _testSpec2 = new BufferDataSpecification
//            {
//                Count = 4,
//                Name = "in_color",
//                Offset = 0,
//                ShouldBeNormalised = false,
//                Stride = 0,
//                Type = VertexAttribPointerType.Float,
//                SizeInBytes = sizeof(float)
//            };
//
//            _testVAO = new VertexArray { DrawPrimitiveType = PrimitiveType.LineStrip };
//            _testVAO.Bind();
//
//            _testVBO1 = new VertexBuffer
//            {
//                BufferUsage = BufferUsageHint.StaticDraw,
//                DrawableIndices = 20,
//                MaxDrawableIndices = 20
//            };
//            _testVBO1.AddSpec(_testSpec1);
//            _testVBO1.CalculateMaxSize();
//            _testVBO1.Bind();
//            _testVBO1.Initialise();
//
//            _testVBO2 = new VertexBuffer
//            {
//                BufferUsage = BufferUsageHint.StaticDraw,
//                DrawableIndices = 20,
//                MaxDrawableIndices = 20
//            };
//            _testVBO2.AddSpec(_testSpec2);
//            _testVBO2.CalculateMaxSize();
//            _testVBO2.Bind();
//            _testVBO2.Initialise();
//
//            _testVAO.Load(_shaderProgram1, new []{ _testVBO1, _testVBO2 });
//
//            _testVBO1.Bind();
//            _testVBO1.Initialise();
//            var data1 = _testLine.line_vertex.SelectMany(x => new []{ x.X, x.Y });
//            data1 = _testLine.line_cap_vertex.SelectMany(x => new[]{ x.X, x.Y }).Take(12);
//            _testVBO1.DrawableIndices = data1.Count();
//            _testVBO1.SetData(data1.ToArray(), _testSpec1);
//
//            _testVBO2.Bind();
//            _testVBO2.Initialise();
//            var data2 = _testLine.line_colour.SelectMany(c => new []{ c.R, c.G, c.B, c.A });
//            data2 = _testLine.line_cap_colour.SelectMany(c => new []{ c.R, c.G, c.B, c.A }).Take(24);
//            _testVBO2.DrawableIndices = data2.Count();
//            _testVBO2.SetData(data2, _testSpec2);
//            _testVBO2.UnBind();
//            _testVAO.UnBind();

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

            _loadingFont = new QFont(SceneManager.FontPath, 30, new QFontBuilderConfiguration(true), FontStyle.Italic);
            _loadingFont.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _loadingText = _loadingFont.ProcessText("Loading", new SizeF(200, -1), QFontAlignment.Centre);
            //_loadingTextPosition = CalculateTextPosition(new Vector2(SceneManager.ScreenCamera.PreferredWidth / 2, SceneManager.ScreenCamera.PreferredHeight / 2), _loadingText);
            _loadingTextPosition = CalculateTextPosition(new Vector3((float)SceneManager.GameWindow.Width/ 2, SceneManager.GameWindow.Height/ 2, 0f), _loadingText);

            _songText = _loadingFont.ProcessText(Path.GetFileNameWithoutExtension(file), new SizeF(SceneManager.GameWindow.Width - 40, -1), QFontAlignment.Centre);

            var dOptions = new DifficultyOptions(600f, 0.2f, 0.4f, 1.5f);

            var progress = new Progress<string>(status =>
            {
                _loadingStatus = status;
            });
            _loadTask = Task.Factory.StartNew(() => _stage.LoadAsync(file, _sonicAnnotatorPath, _pluginPath, _correction, progress, _centerPolygon, _player, dOptions));

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Resize(EventArgs e)
        {
            _loadingText = _loadingFont.ProcessText("Loading", new SizeF(1000, -1), QFontAlignment.Centre);
            _loadingFont.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _loadingTextPosition = CalculateTextPosition(new Vector3(SceneManager.ScreenCamera.PreferredWidth / 2, SceneManager.ScreenCamera.PreferredHeight / 2, 0f), _loadingText);
        }

        public override void Update(double time, bool focused = false)
        {
            if (_loadTask.IsCompleted)
            {
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(new GameScene(_stage){ShaderProgram = _shaderProgram});
            }

            _player.Update(time);
            _centerPolygon.Update(time, false);
            //SceneManager.ScreenCamera.TargetScale += new Vector2(0.1f, 0.1f);
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

            _shaderProgram.SetUniform("in_color", Color4.Black);
            _centerPolygon.DrawOutline(time);

            //Cleanup the program
            _shaderProgram.UnBind();

            _loadingFont.ResetVBOs();
            float yOffset = 0;
            yOffset += _loadingFont.Print(_loadingText, _loadingTextPosition).Height;
            yOffset = MathHelper.Clamp(yOffset + 200 - 50*SceneManager.ScreenCamera.Scale.Y, yOffset, SceneManager.GameWindow.Height*0.5f); 
            var pos = new Vector3(0, -yOffset, 0);
            yOffset += _loadingFont.Print(_songText, pos).Height;
            yOffset += _loadingFont.Print(_loadingStatus, new Vector3(0, -yOffset, 0), QFontAlignment.Centre).Height;
            _loadingFont.Draw();

//            _shaderProgram1.Bind();
//            _shaderProgram1.SetUniform("mvp", SceneManager.ScreenCamera.ModelViewProjection);
//
//            _testVAO.Draw(time);
        }

        public override void UnLoad()
        {
        }

        private Vector3 CalculateTextPosition(Vector3 center, ProcessedText text)
        {
            var size = _loadingFont.Measure(text);
            return new Vector3(0, size.Height/2, 0f);
        }
    }
}
