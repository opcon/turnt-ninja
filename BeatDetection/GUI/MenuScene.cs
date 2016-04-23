using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Core;
using Gwen.Skin;
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
        private bool _selectedItemChanged;

        private QFont _menuFont;
        private QFontDrawing _menuFontDrawing;
        private QFontRenderOptions _menuRenderOptions;

        private GUIComponentContainer _GUIComponents;

        public MenuScene(string sonicAnnotator, string pluginPath)
        {
            _sonicAnnotator = sonicAnnotator;
            _pluginPath = pluginPath;
        }

        public override void Load()
        {
            SceneManager.GameWindow.Cursor = MouseCursor.Default;

            //load shaders
            var vert = new Shader(Path.Combine(SceneManager.Directories["Shaders"].FullName, "simple.vs"));
            var frag = new Shader(Path.Combine(SceneManager.Directories["Shaders"].FullName, "simple.fs"));
            _shaderProgram = new ShaderProgram();
            _shaderProgram.Load(vert, frag);

            _player = new Player();
            _player.ShaderProgram = _shaderProgram;
            _centerPolygon = new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 50, 80, 0);
            _centerPolygon.ShaderProgram = _shaderProgram;

            _singlePlayerPolygon = new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 20, 10, 0);
            _singlePlayerPolygon.Translate = PolarVector.ToCartesianCoordinates(new PolarVector(_singlePlayerPolygon.AngleBetweenSides*((int)MainMenuOptions.SinglePlayer + 0.5f), 270)); 
            _singlePlayerPolygon.ShaderProgram = _shaderProgram;
            _singlePlayerPolygon.PulseMultiplier = 25;
            _singlePlayerPolygon.PulseWidthMax = 7;

            _menuFont = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Regular);
            _menuFontDrawing = new QFontDrawing();
            _menuFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _menuRenderOptions = new QFontRenderOptions {DropShadowActive = true};

            var guiRenderer = new Gwen.Renderer.OpenTK();
            var skin = new TexturedBase(guiRenderer, Path.Combine(SceneManager.Directories["Images"].FullName, "DefaultSkin.png"));
            skin.DefaultFont = new Gwen.Font(guiRenderer, SceneManager.FontPath, 30);
            _GUIComponents = new GUIComponentContainer(guiRenderer, skin);

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
            _GUIComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _menuFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _selectedItemChanged = true;
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Escape)) Exit();

            _player.Update(time);
            _centerPolygon.Update(time, false);

            _singlePlayerPolygon.Position.Azimuth += time*0.5f;
            _singlePlayerPolygon.Update(time, false);

            DoGUI();

            if (_selectedItemChanged)
            {
                _menuFontDrawing.DrawingPrimitives.Clear();
                _menuFontDrawing.Print(_menuFont, _selectedMenuItemText, new Vector3(0, -SceneManager.Height*0.5f + 80, 0), QFontAlignment.Centre, Color.White);
                _selectedItemChanged = false;
            }
        }

        public void Exit()
        {
            SceneManager.RemoveScene(this);
            SceneManager.GameWindow.Exit();
        }

        private void DoGUI()
        {
            //are we using the mouse to navigate?
            if (InputSystem.HasMouseMoved)
            {
                _player.Position = new PolarVector(Math.Atan2(-InputSystem.MouseXY.Y + SceneManager.Height/2.0f, InputSystem.MouseXY.X - SceneManager.Width / 2.0f) - _player.Length*0.5f, _player.Position.Radius);

            }
            var nTheta = MathUtilities.Normalise(_player.Position.Azimuth + _player.Length*0.5f);
            int n = (int) Math.Floor(nTheta / _centerPolygon.AngleBetweenSides);
            if (_selectedMenuItem != (MainMenuOptions) n) _selectedItemChanged = true;
            _selectedMenuItem = (MainMenuOptions) n;

            switch (_selectedMenuItem)
            {
                case MainMenuOptions.SinglePlayer:
                    _selectedMenuItemText = "Play";
                    if (!_singlePlayerPolygon.Pulsing) _singlePlayerPolygon.BeginPulse();
                    break;
                case MainMenuOptions.Scores:
                    _selectedMenuItemText = "Scores";
                break;
                case MainMenuOptions.Options:
                    _selectedMenuItemText = "Options";
                break;
                case MainMenuOptions.Exit:
                    _selectedMenuItemText = "Exit";
                    break;
                case MainMenuOptions.None:
                default:
                    _selectedMenuItem = MainMenuOptions.None;
                    _selectedMenuItemText = "";
                    break;
            }

            // we have selected the current menu item
            if (InputSystem.NewKeys.Contains(Key.Enter) || InputSystem.ReleasedButtons.Contains(MouseButton.Left))
            {
                switch (_selectedMenuItem)
                {
                    case MainMenuOptions.SinglePlayer:
                        //SceneManager.GameWindow.WindowState = WindowState.Normal;
                        //SceneManager.AddScene(new LoadingScene(_sonicAnnotator, _pluginPath, (float) SceneManager.GameSettings["AudioCorrection"], (float) SceneManager.GameSettings["MaxAudioVolume"], _centerPolygon, _player, _shaderProgram), this);
                        SceneManager.AddScene(new ChooseSongScene(_GUIComponents, _centerPolygon, _player, _shaderProgram), this);
                        break;
                    case MainMenuOptions.Options:
                        SceneManager.AddScene(new OptionsScene(_GUIComponents), this);
                        break;
                    case MainMenuOptions.Exit:
                        Exit();
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
        Scores = 2,
        Options = 3,
        Exit = 4,
        None = -1
    }
}
