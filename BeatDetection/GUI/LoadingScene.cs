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
            _shaderProgram = new ShaderProgram();
            _shaderProgram.Load(vert, frag);

            _player = new Player();
            _player.ShaderProgram = _shaderProgram;
            _centerPolygon =  new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 50, 80, 0 );
            _centerPolygon.ShaderProgram = _shaderProgram;
            _stage = new Stage(this.SceneManager);
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

            _loadingFont = new QFont(SceneManager.FontPath, 30, new QFontBuilderConfiguration(true), FontStyle.Italic);
            _loadingFont.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _loadingText = _loadingFont.ProcessText("Loading", new SizeF(200, -1), QFontAlignment.Centre);
            //_loadingTextPosition = CalculateTextPosition(new Vector2(SceneManager.ScreenCamera.PreferredWidth / 2, SceneManager.ScreenCamera.PreferredHeight / 2), _loadingText);
            _loadingTextPosition = CalculateTextPosition(new Vector3((float)SceneManager.GameWindow.Width/ 2, SceneManager.GameWindow.Height/ 2, 0f), _loadingText);

            _songText = _loadingFont.ProcessText(Path.GetFileNameWithoutExtension(file), new SizeF(SceneManager.GameWindow.Width - 40, -1), QFontAlignment.Centre);

            var progress = new Progress<string>(status =>
            {
                _loadingStatus = status;
            });
            _loadTask = Task.Factory.StartNew(() => _stage.LoadAsync(file, _sonicAnnotatorPath, _pluginPath, _correction, progress, _centerPolygon, _player));

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
