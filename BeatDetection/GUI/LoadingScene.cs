using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatDetection.Core;
using BeatDetection.Game;
using Substructio.GUI;

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
                return;
            }

            //TODO implement loading of stage in a background thread
            _loadTask = Task.Factory.StartNew(() => _stage.Load(file, _sonicAnnotatorPath, _pluginPath, _correction));
            //_stage.Load(file, _sonicAnnotatorPath, _pluginPath, _correction);

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Resize(EventArgs e)
        {
        }

        public override void Update(double time, bool focused = false)
        {
            if (_loadTask.IsCompleted)
            {
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(new GameScene(_stage));
            }

            _player.Update(time);
            _centerPolygon.Update(time);

        }

        public override void Draw(double time)
        {
            SceneManager.ScreenCamera.EnableWorldDrawing();
            _player.Draw(time);
            _centerPolygon.Draw(time);
        }

        public override void UnLoad()
        {
        }
    }
}
