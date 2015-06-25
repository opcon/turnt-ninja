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
            _guiComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _canvas = new Canvas(_guiComponents.Skin);
            _canvas.SetSize(WindowWidth, WindowHeight);
            _input = new OpenTKAlternative(_canvas);
            InputSystem.AddGUIInput(_input);


            var tV = new Gwen.Control.TreeControl(_canvas);
            tV.ShouldDrawBackground = false;
            tV.Selected += (sender, arguments) =>
            {
                TreeNode n = sender as TreeNode;
                string path = n.UserData as string;
                if (Path.GetExtension(path) != ".mp3") return;
                SceneManager.RemoveScene(this);
                SceneManager.AddScene(
                    new LoadingScene((string) SceneManager.GameSettings["SonicAnnotatorPath"], (string) SceneManager.GameSettings["PluginPath"], (float) SceneManager.GameSettings["AudioCorrection"],
                        (float) SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram, path), this);
            };

            LoadFiles(tV, @"D:\Patrick\Music\My Music");
            

            //var n1 = tV.AddNode("test");
            ////n1.SetSize(400, 300);
            //n1.AddNode("Node1 Child 1");
            //var n12 = n1.AddNode("Node1 Child 2");
            //n12.AddNode("Node 1 Child 2 Child 1");
            tV.Dock = Pos.Fill;

            //tV.Invalidate();
            //tV.ExpandAll();

            Loaded = true;
        }

        private void LoadFiles(TreeNode parentNode, string directory)
        {
            var directories = Directory.EnumerateDirectories(directory);
            foreach (var dir in directories)
            {
                var allFiles = Directory.EnumerateFiles(dir, "*.mp3", SearchOption.AllDirectories);
                if (!allFiles.Any()) continue;
                var dNode = parentNode.AddNode(Path.GetFileName(dir));
                dNode.UserData = dir;
                var files = Directory.EnumerateFiles(dir, "*.mp3");
                foreach (var file in files)
                {
                    var fNode = dNode.AddNode(Path.GetFileName(file));
                    fNode.UserData = file;
                }

                LoadFiles(dNode, dir);
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
