﻿using System;
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
using QuickFont.Configuration;
using Substructio.Core;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;

namespace BeatDetection.GUI
{
    class MenuScene : Scene
    {
        private ShaderProgram _shaderProgram;
        private Player _player;

        private PolarPolygon _centerPolygon;

        private string _selectedMenuItemText = "";
        private MainMenuOptions _selectedMenuItem = MainMenuOptions.SinglePlayer;
        private bool _selectedItemChanged;

        private GameFont _menuFont;
        private GameFont _versionFont;
        private QFontDrawing _menuFontDrawing;
        private QFontRenderOptions _menuRenderOptions;
        private QFontDrawingPrimitive _menuFDP;

        private GUIComponentContainer _GUIComponents;

        private double _totalTime;

        private string _gameVersion;

        List<MenuEntry> _menuEntries;

        public override void Load()
        {
            SceneManager.GameWindow.Cursor = MouseCursor.Default;

            _gameVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //load shaders
            var vert = new Shader(Path.Combine(SceneManager.Directories["Shaders"].FullName, "simple.vs"));
            var frag = new Shader(Path.Combine(SceneManager.Directories["Shaders"].FullName, "simple.fs"));
            _shaderProgram = new ShaderProgram();
            _shaderProgram.Load(vert, frag);

            _player = new Player();
            _player.ShaderProgram = _shaderProgram;
            _centerPolygon = new PolarPolygon(Enumerable.Repeat(true, 6).ToList(), new PolarVector(0.5, 0), 50, 80, 0);
            _centerPolygon.ShaderProgram = _shaderProgram;

            _menuEntries = new List<MenuEntry>();
            _menuFont = SceneManager.GameFontLibrary.GetFirstOrDefault("menuworld");
            _menuFontDrawing = new QFontDrawing();
            _menuFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.WorldModelViewProjection;
            _menuRenderOptions = new QFontRenderOptions { DropShadowActive = true, Colour = Color.White };

            _menuEntries.Add(new MenuEntry("PLAY", MainMenuOptions.SinglePlayer, _centerPolygon.AngleBetweenSides, _player, _menuFont.Font) { NewState = MenuState.Selected });
            _menuEntries.Add(new MenuEntry("SCORES", MainMenuOptions.Scores, _centerPolygon.AngleBetweenSides, _player, _menuFont.Font));
            _menuEntries.Add(new MenuEntry("OPTIONS", MainMenuOptions.Options, _centerPolygon.AngleBetweenSides, _player, _menuFont.Font));
            _menuEntries.Add(new MenuEntry("EXIT", MainMenuOptions.Exit, _centerPolygon.AngleBetweenSides, _player, _menuFont.Font));
            _menuEntries.Add(new MenuEntry("UPDATE", MainMenuOptions.Update, _centerPolygon.AngleBetweenSides, _player, _menuFont.Font));
            _menuEntries.Add(new MenuEntry("COMING SOON", MainMenuOptions.ComingSoon, _centerPolygon.AngleBetweenSides, _player, _menuFont.Font));

            _versionFont = SceneManager.GameFontLibrary.GetFirstOrDefault("versiontext");

            var guiRenderer = new Gwen.Renderer.OpenTK();
            var skin = new TexturedBase(guiRenderer, Path.Combine(SceneManager.Directories["Images"].FullName, "DefaultSkin.png"));
            skin.DefaultFont = new Gwen.Font(guiRenderer, SceneManager.FontPath, 30);
            _GUIComponents = new GUIComponentContainer(guiRenderer, skin);

            var desiredCoords = SceneManager.GameWindow.PointToScreen(new Point(WindowWidth/2, WindowHeight/8));
            Mouse.SetPosition(desiredCoords.X, desiredCoords.Y);

            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
            _GUIComponents.Resize(SceneManager.ScreenCamera.ScreenProjectionMatrix, WindowWidth, WindowHeight);
            _menuFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.WorldModelViewProjection;
            _selectedItemChanged = true;
        }

        public override void Update(double time, bool focused = false)
        {
            // Update total elapsed time
            _totalTime += time;

            if (InputSystem.NewKeys.Contains(Key.Escape)) Exit();

            _player.Update(time);
            _centerPolygon.Update(time, false);

            DoGUI();

            _menuFontDrawing.DrawingPrimitives.Clear();
            foreach (var me in _menuEntries)
            {
                me.Update(time);
                _menuFontDrawing.DrawingPrimitives.Add(me.Print(_menuRenderOptions));
            }

            //// Update next if needed
            //if (_selectedItemChanged)
            //{
            //    // Reset elapsed time
            //    _totalTime = 0;

            //    _menuFontDrawing.DrawingPrimitives.Clear();
            //    _menuFDP = new QFontDrawingPrimitive(_menuFont.Font, _menuRenderOptions);


            //    _menuFDP.Print(_selectedMenuItemText.ToUpper(), Vector3.Zero, QFontAlignment.Centre);
            //    _menuFontDrawing.DrawingPrimitives.Add(_menuFDP);
            //    _selectedItemChanged = false;
            //}

            //// Pulse text
            //var size = _menuFont.Font.Measure(_selectedMenuItemText.ToUpper());
            //var selectedSide = GetSelectedSide();
            //var newPos = new PolarVector(selectedSide * _centerPolygon.AngleBetweenSides + _centerPolygon.AngleBetweenSides * 0.5f, _player.Position.Radius + _player.Width + size.Height*0.9);

            //var extraRotation = (selectedSide >= 0 && selectedSide < 3) ? (-Math.PI / 2.0) : (Math.PI / 2.0);
            //var extraOffset = (selectedSide >= 0 && selectedSide < 3) ? (0) : (-size.Height / 4);

            //newPos.Radius += extraOffset;
            //var cart = newPos.ToCartesianCoordinates();
            //var mvm = Matrix4.CreateTranslation(0, size.Height / 2, 0)
            //            * Matrix4.CreateScale(0.90f + (float)Math.Pow(Math.Sin(_totalTime*3), 2)*0.10f)
            //            * Matrix4.CreateRotationZ((float)(newPos.Azimuth + extraRotation))
            //            * Matrix4.CreateTranslation(cart.X, cart.Y, 0);
            //_menuFDP.ModelViewMatrix = mvm;
        }

        public void Exit()
        {
            SceneManager.RemoveScene(this, true);
            SceneManager.GameWindow.Exit();
        }

        private int GetSelectedSide()
        {
            var nTheta = MathUtilities.Normalise(_player.Position.Azimuth + _player.Length * 0.5f);
            return (int)Math.Floor(nTheta / _centerPolygon.AngleBetweenSides);
        }

        private void DoGUI()
        {
            //are we using the mouse to navigate?
            if (InputSystem.HasMouseMoved)
            {
                _player.Position = new PolarVector(Math.Atan2(-InputSystem.MouseXY.Y + SceneManager.Height/2.0f, InputSystem.MouseXY.X - SceneManager.Width / 2.0f) - _player.Length*0.5f, _player.Position.Radius);
            }

            int selectedSide = GetSelectedSide();
            if (_selectedMenuItem != (MainMenuOptions) selectedSide) _selectedItemChanged = true;
            _selectedMenuItem = (MainMenuOptions) selectedSide;

            _menuEntries.ForEach(me => me.NewState = MenuState.Unselected);
            _menuEntries.First(me => me.Option == _selectedMenuItem).NewState = MenuState.Selected;
            switch (_selectedMenuItem)
            {
                case MainMenuOptions.SinglePlayer:
                    _selectedMenuItemText = "Play";
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
                case MainMenuOptions.Update:
                    _selectedMenuItemText = "Update";
                    break;
                case MainMenuOptions.ComingSoon:
                    _selectedMenuItemText = "Coming Soon";
                    break;
                default:
                    _selectedMenuItemText = "";
                    break;
            }

            // we have selected the current menu item
            if (InputSystem.NewKeys.Contains(Key.Enter) || InputSystem.ReleasedButtons.Contains(MouseButton.Left))
            {
                switch (_selectedMenuItem)
                {
                    case MainMenuOptions.SinglePlayer:
                        var cs = SceneManager.SceneList.Find(s => s.GetType() == typeof(ChooseSongScene));
                        if (cs == null)
                        {
                            cs = new ChooseSongScene(_GUIComponents, _centerPolygon, _player, _shaderProgram);
                            SceneManager.AddScene(cs, this);
                        }
                        cs.Visible = true;
                        break;
                    case MainMenuOptions.Options:
                        SceneManager.AddScene(new OptionsScene(_GUIComponents), this);
                        break;
                    case MainMenuOptions.Exit:
                        Exit();
                        break;
                    case MainMenuOptions.Update:
                        SceneManager.AddScene(new UpdateScene(), this);
                        break;
                }
            }
        }

        public override void Draw(double time)
        {
            _shaderProgram.Bind();
            _shaderProgram.SetUniform("mvp", SceneManager.ScreenCamera.WorldModelViewProjection);
            _shaderProgram.SetUniform("in_color", Color4.White);

            //Draw the player
            _player.Draw(time);

            //Draw the center polygon
            _centerPolygon.Draw(time);

            _shaderProgram.SetUniform("in_color", Color4.Black);
            _centerPolygon.DrawOutline(time);

            //Cleanup the program
            _shaderProgram.UnBind();

            _menuFontDrawing.RefreshBuffers();
            _menuFontDrawing.Draw();

            SceneManager.DrawTextLine("TURNT NINJA " + _gameVersion, new Vector3(-WindowWidth / 2+5, -WindowHeight / 2 + _versionFont.Font.MaxLineHeight, 0), Color.White, QFontAlignment.Left, _versionFont.Font);
        }

        public override void Dispose()
        {
            _GUIComponents.Dispose();
            if (_shaderProgram != null)
            {
                _shaderProgram.Dispose();
                _shaderProgram = null;
            }
            _menuFontDrawing.Dispose();
            _menuFont.Dispose();
        }
    }

    enum MainMenuOptions
    {
        SinglePlayer = 1,
        Scores = 2,
        Options = 3,
        Exit = 4,
        Update = 5,
        ComingSoon = 0,
    }
}
