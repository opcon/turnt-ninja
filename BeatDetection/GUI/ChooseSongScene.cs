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

            //SetUpFileBrowser();
            //EnterDirectory(_drives.First().Path);

            _directoryBrowser = new DirectoryBrowser(SceneManager, this);
            _directoryBrowser.AddFileSystem(new LocalFileSystem());

            Loaded = true;
        }

        //public void StartGame(string path)
        //{
        //    if (!_fileFilter.Any(s => path.EndsWith(s, StringComparison.OrdinalIgnoreCase)) && !path.Contains("SOUNDCLOUD")) return;
        //    SceneManager.RemoveScene(this);
        //    SceneManager.AddScene(
        //        new LoadingScene((string)SceneManager.GameSettings["SonicAnnotatorPath"], (string)SceneManager.GameSettings["PluginPath"],
        //            (float)SceneManager.GameSettings["AudioCorrection"],
        //            (float)SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram, path), this);
        //}

        public void SongChosen(Song song)
        {
            SceneManager.RemoveScene(this);
            SceneManager.AddScene(
                new LoadingScene((string)SceneManager.GameSettings["SonicAnnotatorPath"], (string)SceneManager.GameSettings["PluginPath"],
                    (float)SceneManager.GameSettings["AudioCorrection"],
                    (float)SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram, song.InternalName), this);
        }

        //private void SetUpFileBrowser()
        //{
        //    _drives = new List<FileBrowserEntry>();
        //    _fileBrowserEntries = new List<FileBrowserEntry>();
        //    foreach (var driveInfo in DriveInfo.GetDrives().Where(d => d.IsReady))
        //    {
        //        _drives.Add(new FileBrowserEntry { Path = driveInfo.RootDirectory.FullName, EntryType = FileBrowserEntryType.Directory | FileBrowserEntryType.Drive | FileBrowserEntryType.Special,
        //            Name = string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? driveInfo.Name : string.Format("{1} ({0})", driveInfo.Name, driveInfo.VolumeLabel) });
        //    }
        //}

        //private void EnterDirectory(string directoryPath)
        //{
        //    if (!Directory.Exists(directoryPath)) throw new Exception("Directory doesn't exist");
        //    var directories = Directory.EnumerateDirectories(directoryPath).Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.Hidden));
        //    var files = Directory.EnumerateFiles(directoryPath).Where(p => _fileFilter.Any(f => p.EndsWith(f, StringComparison.OrdinalIgnoreCase)));

        //    _fileBrowserEntries.Clear();
        //    _fileBrowserEntries.Add(new FileBrowserEntry { Path = Path.Combine(directoryPath, "../"), EntryType = FileBrowserEntryType.Directory | FileBrowserEntryType.Special, Name = "Parent Directory" });
        //    _fileBrowserEntries.AddRange(_drives);

        //    _fileBrowserEntries.Add(new FileBrowserEntry { Path = "SOUNDCLOUD", Name = "Soundcloud", EntryType = FileBrowserEntryType.Song });
        //    _fileBrowserEntries.Add(new FileBrowserEntry { Path = "./", Name = "--------------------", EntryType = FileBrowserEntryType.Separator });

        //    foreach (var dir in directories.OrderBy(d => Path.GetDirectoryName(d)))
        //    {
        //        _fileBrowserEntries.Add(new FileBrowserEntry { Path = dir, Name = Path.GetFileName(dir), EntryType = FileBrowserEntryType.Directory });
        //    }

        //    //add separator
        //    _fileBrowserEntries.Add(new FileBrowserEntry { Path = "./", Name = "--------------------", EntryType = FileBrowserEntryType.Separator });

        //    foreach (var file in files.OrderBy(f => Path.GetFileName(f)))
        //    {
        //        _fileBrowserEntries.Add(new FileBrowserEntry { Path = file, Name = Path.GetFileNameWithoutExtension(file), EntryType = FileBrowserEntryType.Song });
        //    }

        //    _index = _drives.Count;

        //}

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Escape)) SceneManager.RemoveScene(this);

            _directoryBrowser.Update(time);

            //if (InputSystem.NewKeys.Contains(Key.Enter))
            //{
            //    if (_fileBrowserEntries[_index].EntryType.HasFlag(FileBrowserEntryType.Directory))
            //        EnterDirectory(_fileBrowserEntries[_index].Path);
            //    else if (_fileBrowserEntries[_index].EntryType.HasFlag(FileBrowserEntryType.Song))
            //        StartGame(_fileBrowserEntries[_index].Path);
            //}

            //if (frameCount == 60) _guiComponents.Renderer.FlushTextCache();

            //if (InputSystem.NewKeys.Contains(Key.Up))
            //    _index--;
            //if (InputSystem.NewKeys.Contains(Key.Down))
            //    _index++;
            //if (InputSystem.NewKeys.Contains(Key.Left))
            //    _index -= 10;
            //if (InputSystem.NewKeys.Contains(Key.Right))
            //    _index += 10;

            //foreach (var c in InputSystem.PressedChars)
            //{
            //    int match = _fileBrowserEntries.FindIndex(fbe => fbe.Name.StartsWith(c.ToString(), StringComparison.CurrentCultureIgnoreCase) && !fbe.EntryType.HasFlag(FileBrowserEntryType.Special));
            //    if (match >= 0) _index = match;
            //}

            //if (_index < 0) _index = 0;
            //if (_index >= _fileBrowserEntries.Count) _index = _fileBrowserEntries.Count - 1;
        }

        public override void Draw(double time)
        {
            _directoryBrowser.Draw(time);
            //float ypos = 30 * 8;
            //for (int i = _index - 8; i < _index + 8; i++)
            //{
            //    if (i >= 0 && i < _fileBrowserEntries.Count && i != _index) SceneManager.DrawTextLine(_fileBrowserEntries[i].Name, new Vector3(0, ypos, 0), Color4.Black, QuickFont.QFontAlignment.Centre);
            //    ypos -= 30;
            //}
            //SceneManager.DrawTextLine(_fileBrowserEntries[_index].Name, new Vector3(0, 0, 0), Color4.White, QuickFont.QFontAlignment.Centre);
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
