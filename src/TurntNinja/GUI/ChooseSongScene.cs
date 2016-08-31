using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TurntNinja.Core;
using Substructio.Core;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using Key = OpenTK.Input.Key;
using OpenTK;
using OpenTK.Graphics;
using TurntNinja.FileSystem;
using TurntNinja.Audio;
using System.Xml.Serialization;
using Substructio;

namespace TurntNinja.GUI
{
    class ChooseSongScene : Scene
    {
        private GUIComponentContainer _guiComponents;
        private readonly PolarPolygon _centerPolygon;
        private readonly Player _player;
        private readonly ShaderProgram _shaderProgram;
        private List<SongBase> _recentSongs;
        
        private const string RECENT_SONGS_FILE = "recent-songs.xml";
        private string _recentSongsFile = "";

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
            InputSystem.RepeatingKeys.Add(Key.BackSpace, KeyRepeatSettings.Default);

            _directoryBrowser = new DirectoryBrowser(SceneManager, this);
            _directoryBrowser.AddFileSystem(new LocalFileSystem(SceneManager.Directories));
            _directoryBrowser.AddFileSystem(new SoundCloudFileSystem());
            
            // Find recent songs file path
            _recentSongsFile = ServiceLocator.Directories.Locate("AppData", RECENT_SONGS_FILE);
            
            // Load recent songs
            LoadRecentSongs();

            // Make sure to add recent file system last!
            _directoryBrowser.AddFileSystem(new RecentFileSystem(_recentSongs));

            _directoryBrowser.Resize(WindowWidth, WindowHeight);
            Loaded = true;
        }
        
        private void LoadRecentSongs()
        {
            if (File.Exists(_recentSongsFile))
            {
                var serializer = new XmlSerializer(typeof(List<SongBase>));
                using (TextReader f = new StreamReader(_recentSongsFile))
                {
                    _recentSongs = (List<SongBase>)serializer.Deserialize(f);
                }
            }
            else
                _recentSongs = new List<SongBase>();
        }
        
        private void SaveRecentSongs()
        {
            var serializer = new XmlSerializer(typeof(List<SongBase>));
            using (TextWriter f = new StreamWriter(_recentSongsFile))
            {
                serializer.Serialize(f, _recentSongs);
            }
        }

        public void SongChosen(Song song)
        {
            // Track song play with analytics
            ServiceLocator.Analytics.SetCustomVariable(1, "File System", song.FileSystem.FriendlyName, Substructio.Logging.CustomVariableScope.ApplicationView);
            ServiceLocator.Analytics.TrackEvent("Song", "Play", song.FileSystem.FriendlyName);
            
            // Remove the currently selected song if it already exists in the recent songs list
            // so that we can insert it again at the head, retaining the list order
            _recentSongs.Remove(song.SongBase);
            
            // Remove the oldest song if we are over the maximum recent song count
            if (_recentSongs.Count >= (int)SceneManager.GameSettings["MaxRecentSongCount"])
                _recentSongs.RemoveAt(_recentSongs.Count - 1);

            // Insert the new song at the head of the list
            _recentSongs.Insert(0, song.SongBase);
            
            // Save recent songs file
            SaveRecentSongs();
            
            // Refresh recent songs filesystem
            _directoryBrowser.RefreshRecentSongFilesystem();

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
            _directoryBrowser.Resize(WindowWidth, WindowHeight);
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
            InputSystem.RepeatingKeys.Remove(Key.BackSpace);
        }
    }
}
