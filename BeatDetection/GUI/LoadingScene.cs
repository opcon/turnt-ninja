using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatDetection.Game;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class LoadingScene : Scene
    {
        private string _sonicAnnotatorPath;
        private string _pluginPath;
        private float _correction;

        public LoadingScene(string sonicAnnotatorPath, string pluginPath, float correction)
        {
            _sonicAnnotatorPath = sonicAnnotatorPath;
            _pluginPath = pluginPath;
            _correction = correction;

            _stage = new Stage();
        }

        private Stage _stage;
        public override void Load()
        {
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
            _stage.Load(file, _sonicAnnotatorPath, _pluginPath, _correction);

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
            if (Loaded)
            {
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(new GameScene(_stage));
            }

        }

        public override void Draw(double time)
        {
        }

        public override void UnLoad()
        {
        }
    }
}
