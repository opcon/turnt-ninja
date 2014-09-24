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
using QuickFont;
using Substructio.GUI;
using OpenTK.Graphics.OpenGL;

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

        private string _loadingStatus;

        public LoadingScene(string sonicAnnotatorPath, string pluginPath, float correction)
        {
            _sonicAnnotatorPath = sonicAnnotatorPath;
            _pluginPath = pluginPath;
            _correction = correction;
        }

        public override void Load()
        {
            _player = new Player();
            _centerPolygon = new PolarPolygon(6, 6, 0, 1, 0, 80);
            _stage = new Stage(_player, _centerPolygon);

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
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(new GameScene(_stage));
            }

            _player.Update(time);
            _centerPolygon.Update(time, false);

        }

        public override void Draw(double time)
        {
            SceneManager.ScreenCamera.EnableScreenDrawing();
            //SceneManager.DrawTextLine("hello", new Vector2(100, 100));
            SceneManager.DrawProcessedText(_loadingText, _loadingTextPosition, _loadingFont);
            SceneManager.DrawProcessedText(_songText, new Vector2(SceneManager.GameWindow.Width/2, SceneManager.GameWindow.Height - 100), _loadingFont);
            SceneManager.DrawTextLine(_loadingStatus, new Vector2(SceneManager.GameWindow.Width/2, 100), _loadingFont);
            //SceneManager.DrawProcessedText(_loadingText, new Vector2(0, 0), _loadingFont);
            //QFont.Begin();
            //_loadingFont.Print(_loadingText, _loadingTextPosition);
            //QFont.End();
            GL.Disable(EnableCap.Texture2D);
            SceneManager.ScreenCamera.EnableWorldDrawing();
            _player.Draw(time);
            _centerPolygon.Draw(time);
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
