using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Core;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using QuickFont;
using Substructio.Core;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class MenuScene : Scene
    {
        private string _sonicAnnotator;
        private string _pluginPath;
        private ShaderProgram _shaderProgram;
        private Player _player;

        private PolarPolygon _centerPolygon;
        private PolarPolygon _singlePlayerPolygon;

        private string _selectedMenuItemText = "";
        private MainMenuOptions _selectedMenuItem = MainMenuOptions.None;
        private bool _selectedItemChanged = false;

        private QFont _menuFont;
        private QFontDrawing _menuFontDrawing;
        private QFontRenderOptions _menuRenderOptions;

        public MenuScene(string sonicAnnotator, string pluginPath, float correction)
        public MenuScene(string sonicAnnotator, string pluginPath)
        {
            _sonicAnnotator = sonicAnnotator;
            _pluginPath = pluginPath;
        }

        public override void Load()
        {
            SceneManager.GameWindow.Cursor = MouseCursor.Default;
            var vert = new Shader(Directories.ShaderDirectory + "/simple.vs");
            var frag = new Shader(Directories.ShaderDirectory + "/simple.fs");
            _shaderProgram = new ShaderProgram();
            _shaderProgram.Load(vert, frag);

            _player = new Player();
            _player.ShaderProgram = _shaderProgram;
            _centerPolygon = new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 50, 80, 0);
            _centerPolygon.ShaderProgram = _shaderProgram;

            _singlePlayerPolygon = new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 20, 10, 0);
            _singlePlayerPolygon.Translate = PolarVector.ToCartesianCoordinates(new PolarVector(_singlePlayerPolygon.AngleBetweenSides*1.5, 270)); 
            _singlePlayerPolygon.ShaderProgram = _shaderProgram;

            _menuFont = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Italic);
            _menuFontDrawing = new QFontDrawing();
            _menuFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _menuRenderOptions = new QFontRenderOptions {DropShadowActive = true};

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
            _menuFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _selectedItemChanged = true;
        }

        public override void Update(double time, bool focused = false)
        {
            _player.Update(time);
            _centerPolygon.Update(time, false);
            _singlePlayerPolygon.Position.Azimuth += time*0.5f;
            _singlePlayerPolygon.Update(time, false);

            DoGUI();

            if (_selectedItemChanged)
            {
                _menuFontDrawing.DrawingPimitiveses.Clear();
                _menuFontDrawing.Print(_menuFont, _selectedMenuItemText, new Vector3(0, -SceneManager.Height*0.5f + 80, 0), QFontAlignment.Centre, Color.White);
                _selectedItemChanged = false;
            }
        }

        private void DoGUI()
        {
            var nTheta = MathUtilities.Normalise(_player.Position.Azimuth);
            int n = (int) Math.Floor(nTheta / _centerPolygon.AngleBetweenSides);
            if (_selectedMenuItem != (MainMenuOptions) n) _selectedItemChanged = true;
            _selectedMenuItem = (MainMenuOptions) n;

            switch (_selectedMenuItem)
            {
                case MainMenuOptions.SinglePlayer:
                    _selectedMenuItemText = "Single Player";
                    break;
                case MainMenuOptions.None:
                default:
                    _selectedMenuItem = MainMenuOptions.None;
                    _selectedMenuItemText = "No Option Selected";
                    break;
            }

            if (InputSystem.NewKeys.Contains(Key.Enter))
            {
                switch (_selectedMenuItem)
                {
                    case MainMenuOptions.SinglePlayer:
                        SceneManager.GameWindow.WindowState = WindowState.Normal;
                        SceneManager.AddScene(new LoadingScene(_sonicAnnotator, _pluginPath, _correction, _centerPolygon, _player, _shaderProgram));
                        break;
                    case MainMenuOptions.None:
                    default:
                        break;
                }
            }
        }

        public override void Draw(double time)
        {
            _shaderProgram.Bind();
            _shaderProgram.SetUniform("mvp", SceneManager.ScreenCamera.ModelViewProjection);
            _shaderProgram.SetUniform("in_color", Color4.White);

            //Draw the player
            _player.Draw(time);

            //Draw the center polygon
            _centerPolygon.Draw(time);
            _singlePlayerPolygon.Draw(time);

            _shaderProgram.SetUniform("in_color", Color4.Black);
            _centerPolygon.DrawOutline(time);
            _singlePlayerPolygon.DrawOutline(time);

            //Cleanup the program
            _shaderProgram.UnBind();

            _menuFontDrawing.RefreshBuffers();
            _menuFontDrawing.Draw();
        }

        public override void Dispose()
        {
        }
    }

    enum MainMenuOptions
    {
        SinglePlayer = 1,
        None = -1
    }
}
