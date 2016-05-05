using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Core;
using Substructio.Core;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using Key = OpenTK.Input.Key;
using OpenTK;
using OpenTK.Graphics;
using BeatDetection.FileSystem;
using BeatDetection.Audio;

namespace BeatDetection.GUI
{
    class ChooseSongScene : Scene
    {
        private GUIComponentContainer _guiComponents;
        private readonly PolarPolygon _centerPolygon;
        private readonly Player _player;
        private readonly ShaderProgram _shaderProgram;
        private List<SongBase> _recentSongs;

        DirectoryBrowser _directoryBrowser;

        public ChooseSongScene(GUIComponentContainer guiComponents, PolarPolygon centerPolygon, Player player, ShaderProgram shaderProgram)
        {
            _guiComponents = guiComponents;
            _centerPolygon = centerPolygon;
            _player = player;
            _shaderProgram = shaderProgram;
            Exclusive = true;
        }

        public override void Load()
        {

            InputSystem.RepeatingKeys.Add(Key.Down, KeyRepeatSettings.Default);
            InputSystem.RepeatingKeys.Add(Key.Up, KeyRepeatSettings.Default);
            InputSystem.RepeatingKeys.Add(Key.Left, KeyRepeatSettings.Default);
            InputSystem.RepeatingKeys.Add(Key.Right, KeyRepeatSettings.Default);

            _directoryBrowser = new DirectoryBrowser(SceneManager, this);
            _directoryBrowser.AddFileSystem(new LocalFileSystem());
            _directoryBrowser.AddFileSystem(new SoundCloudFileSystem());

            if (SceneManager.GameSettings["RecentSongs"] == null)
                SceneManager.GameSettings["RecentSongs"] = _recentSongs = new List<SongBase>();
            _recentSongs = (List<SongBase>)SceneManager.GameSettings["RecentSongs"];

            // Make sure to add recent file system last!
            _directoryBrowser.AddFileSystem(new RecentFileSystem(_recentSongs));
            Loaded = true;
        }

        public void SongChosen(Song song)
        {
            if (_recentSongs.Count >= (int)SceneManager.GameSettings["MaxRecentSongCount"])
                _recentSongs.RemoveAt(_recentSongs.Count - 1);
            _recentSongs.Remove(song.SongBase);

            _recentSongs.Insert(0, song.SongBase);

            SceneManager.GameSettings["RecentSongs"] = _recentSongs;
            //SceneManager.RemoveScene(this);
            this.Visible = false;
            SceneManager.AddScene(
                new LoadingScene(
                    (float)SceneManager.GameSettings["AudioCorrection"],
                    (float)SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram, song), this);
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
        }

        public override void Update(double time, bool focused = false)
        {
            if (focused)
            {
                if (InputSystem.NewKeys.Contains(Key.Escape)) this.Visible = false;

                _directoryBrowser.Update(time);
            }
        }

        public override void Draw(double time)
        {
            _directoryBrowser.Draw(time);
        }

        public override void Dispose()
        {
            InputSystem.RepeatingKeys.Remove(Key.Down);
            InputSystem.RepeatingKeys.Remove(Key.Up);
            InputSystem.RepeatingKeys.Remove(Key.Left);
            InputSystem.RepeatingKeys.Remove(Key.Right);
        }
    }
}
