using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Core;
using Gwen;
using Gwen.Control;
using Gwen.Input;
using Substructio.Core;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using Key = OpenTK.Input.Key;
using OpenTK;
using OpenTK.Graphics;

namespace BeatDetection.GUI
{
    class ChooseSongScene : Scene
    {
        private GUIComponentContainer _guiComponents;
        private readonly PolarPolygon _centerPolygon;
        private readonly Player _player;
        private readonly ShaderProgram _shaderProgram;
        private Canvas _canvas;
        private OpenTKAlternative _input;

        private string[] _fileFilter;

        private TreeControl tV;

        private int frameCount = 0;

        List<FileBrowserEntry> _fileBrowserEntries;
        List<FileBrowserEntry> _drives;
        int _index = 0;

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

            _fileFilter = ((string)SceneManager.GameSettings["FileFilter"]).Split(',');
            var ext = CSCore.Codecs.CodecFactory.Instance.GetSupportedFileExtensions();
            _fileFilter = ext;

            _guiComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _canvas = new Canvas(_guiComponents.Skin);
            _canvas.SetSize(WindowWidth, WindowHeight);
            _input = new OpenTKAlternative(_canvas);
            InputSystem.AddGUIInput(_input);


            tV = new TreeControl(_canvas){AutoUpdateBounds = true};
            tV.Selected += (sender, arguments) =>
            {
                TreeNode n = sender as TreeNode;
                string path = n.UserData as string;
                //var ext = Path.GetExtension(path);
                StartGame(path);
            };

            tV.Expanded += (sender, arguments) =>
            {
                TreeNode n = sender as TreeNode;
                string path = n.UserData as string;
                //This is a directory, not a file
                LoadFiles(n, path);
                n.Invalidate();
                n.InvalidateParent();
                n.SizeToChildren(false, true);
            };

            tV.Collapsed += (sender, arguments) =>
            {
                TreeNode n = sender as TreeNode;
                foreach (var c in n.Children)
                {
                    tV.RemoveChild(c, true);
                }
            };

            foreach (var driveInfo in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var n = tV.AddNode(string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? driveInfo.Name : string.Format("{1} ({0})", driveInfo.Name, driveInfo.VolumeLabel));
                n.UserData = driveInfo.RootDirectory.FullName;
                n.ForceShowToggle = true;
            }

            tV.AutoUpdateBounds = true;
            tV.UpdateBounds();

            tV.Dock = Pos.Fill;

            tV.Hide();

            SetUpFileBrowser();
            EnterDirectory(_drives.First().Path);

            Loaded = true;
        }

        private void StartGame(string path)
        {
            if (!_fileFilter.Any(s => path.EndsWith(s, StringComparison.OrdinalIgnoreCase))) return;
            SceneManager.RemoveScene(this);
            SceneManager.AddScene(
                new LoadingScene((string)SceneManager.GameSettings["SonicAnnotatorPath"], (string)SceneManager.GameSettings["PluginPath"],
                    (float)SceneManager.GameSettings["AudioCorrection"],
                    (float)SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram, path), this);
        }

        private void LoadFiles(TreeNode parentNode, string directory)
        {
            var directories = Directory.EnumerateDirectories(directory);
            foreach (var dir in directories.Where(Directory.Exists))
            {
                TreeNode dNode = null;
                try
                {
                    if (new DirectoryInfo(dir).Attributes.HasFlag(FileAttributes.Hidden)) continue;
                    dNode = parentNode.AddNode(Path.GetFileName(dir));
                    dNode.ForceShowToggle = true;
                    dNode.UserData = dir;
                    var files = Directory.EnumerateFiles(dir);
                    foreach (var file in files.Where(p => _fileFilter.Any(f => p.EndsWith(f, StringComparison.OrdinalIgnoreCase))))
                    {
                        var fNode = dNode.AddNode(Path.GetFileNameWithoutExtension(file));
                        fNode.UserData = file;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (dNode != null) dNode.TreeControl.RemoveChild(dNode, true);
                }
            }
        }

        private void SetUpFileBrowser()
        {
            _drives = new List<FileBrowserEntry>();
            _fileBrowserEntries = new List<FileBrowserEntry>();
            foreach (var driveInfo in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                _drives.Add(new FileBrowserEntry { Path = driveInfo.RootDirectory.FullName, EntryType = FileBrowserEntryType.Directory | FileBrowserEntryType.Drive | FileBrowserEntryType.Special,
                    Name = string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? driveInfo.Name : string.Format("{1} ({0})", driveInfo.Name, driveInfo.VolumeLabel) });
            }
        }

        private void EnterDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) throw new Exception("Directory doesn't exist");
            var directories = Directory.EnumerateDirectories(directoryPath).Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.Hidden));
            var files = Directory.EnumerateFiles(directoryPath).Where(p => _fileFilter.Any(f => p.EndsWith(f, StringComparison.OrdinalIgnoreCase)));

            _fileBrowserEntries.Clear();
            _fileBrowserEntries.Add(new FileBrowserEntry { Path = Path.Combine(directoryPath, "../"), EntryType = FileBrowserEntryType.Directory | FileBrowserEntryType.Special, Name = "Parent Directory" });
            _fileBrowserEntries.AddRange(_drives);

            _fileBrowserEntries.Add(new FileBrowserEntry { Path = "./", Name = "--------------------", EntryType = FileBrowserEntryType.Separator });

            foreach (var dir in directories.OrderBy(d => Path.GetDirectoryName(d)))
            {
                _fileBrowserEntries.Add(new FileBrowserEntry { Path = dir, Name = Path.GetFileName(dir), EntryType = FileBrowserEntryType.Directory });
            }

            //add separator
            _fileBrowserEntries.Add(new FileBrowserEntry { Path = "./", Name = "--------------------", EntryType = FileBrowserEntryType.Separator });

            foreach (var file in files.OrderBy(f => Path.GetFileName(f)))
            {
                _fileBrowserEntries.Add(new FileBrowserEntry { Path = file, Name = Path.GetFileNameWithoutExtension(file), EntryType = FileBrowserEntryType.File });
            }

            _index = _drives.Count;

        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
            _guiComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _canvas.SetSize(WindowWidth, WindowHeight);
        }

        public override void Update(double time, bool focused = false)
        {
            frameCount++;
            if (frameCount > 60) frameCount = 0;
            if (InputSystem.NewKeys.Contains(Key.Escape)) SceneManager.RemoveScene(this);

            _guiComponents.Renderer.Update(time);

            if (InputSystem.NewKeys.Contains(Key.Enter))
            {
                if (_fileBrowserEntries[_index].EntryType.HasFlag(FileBrowserEntryType.Directory))
                    EnterDirectory(_fileBrowserEntries[_index].Path);
                else if (_fileBrowserEntries[_index].EntryType.HasFlag(FileBrowserEntryType.File))
                    StartGame(_fileBrowserEntries[_index].Path);
            }

            if (frameCount == 60) _guiComponents.Renderer.FlushTextCache();

            if (InputSystem.NewKeys.Contains(Key.Up))
                _index--;
            if (InputSystem.NewKeys.Contains(Key.Down))
                _index++;
            if (InputSystem.NewKeys.Contains(Key.Left))
                _index -= 10;
            if (InputSystem.NewKeys.Contains(Key.Right))
                _index += 10;

            foreach (var c in InputSystem.PressedChars)
            {
                int match = _fileBrowserEntries.FindIndex(fbe => fbe.Name.StartsWith(c.ToString(), StringComparison.CurrentCultureIgnoreCase) && !fbe.EntryType.HasFlag(FileBrowserEntryType.Special));
                if (match >= 0) _index = match;
            }


            if (_index < 0) _index = 0;
            if (_index >= _fileBrowserEntries.Count) _index = _fileBrowserEntries.Count - 1;
        }

        public override void Draw(double time)
        {
            _canvas.RenderCanvas();
            float ypos = 30 * 8;
            for (int i = _index - 8; i < _index + 8; i++)
            {
                if (i >= 0 && i < _fileBrowserEntries.Count && i != _index) SceneManager.DrawTextLine(_fileBrowserEntries[i].Name, new Vector3(0, ypos, 0), Color4.Black, QuickFont.QFontAlignment.Centre);
                ypos -= 30;
            }
            SceneManager.DrawTextLine(_fileBrowserEntries[_index].Name, new Vector3(0, 0, 0), Color4.White, QuickFont.QFontAlignment.Centre);
        }

        public override void Dispose()
        {
            InputSystem.RemoveGUIInput(_input);

            InputSystem.RepeatingKeys.Remove(Key.Down);
            InputSystem.RepeatingKeys.Remove(Key.Up);
            InputSystem.RepeatingKeys.Remove(Key.Left);
            InputSystem.RepeatingKeys.Remove(Key.Right);

            _canvas.Dispose();
        }
    }

    struct FileBrowserEntry
    {
        public string Path;
        public string Name;
        public FileBrowserEntryType EntryType;
    }

    [Flags]
    enum FileBrowserEntryType
    {
        File = 0,
        Directory = 1 << 0,
        Drive = 1 << 1,
        Special = 1 << 2,
        Separator = 1 << 3
    }
}
