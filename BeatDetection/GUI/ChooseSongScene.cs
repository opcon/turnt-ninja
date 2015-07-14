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
            _fileFilter = ((string)SceneManager.GameSettings["FileFilter"]).Split(',');

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
                if (!_fileFilter.Any(s => path.EndsWith(s, StringComparison.OrdinalIgnoreCase))) return;
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(
                    new LoadingScene((string) SceneManager.GameSettings["SonicAnnotatorPath"], (string) SceneManager.GameSettings["PluginPath"],
                        (float) SceneManager.GameSettings["AudioCorrection"],
                        (float) SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram, path), this);
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

            Loaded = true;
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

            if (frameCount == 60) _guiComponents.Renderer.FlushTextCache();

        }

        public override void Draw(double time)
        {
            _canvas.RenderCanvas();
        }

        public override void Dispose()
        {
            InputSystem.RemoveGUIInput(_input);
            _canvas.Dispose();
        }
    }
}
