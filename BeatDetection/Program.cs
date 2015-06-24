
using System;
using System.Drawing;
using System.Threading;
using BeatDetection.Core;
using BeatDetection.Game;
using BeatDetection.GUI;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BeatDetection.Core.Settings;
using QuickFont;
using Substructio.Core;
using Substructio.Core.Settings;
using Substructio.GUI;

namespace BeatDetection
{
    /// <summary>
    /// Demonstrates the GameWindow class.
    /// </summary>
    public sealed class GameController : GameWindow
    {

        private SceneManager _gameSceneManager;
        private const float prefWidth = 1920;
        private const float prefHeight = 1080;

        private string sonicAnnotator = "../../External Programs/sonic-annotator-1.0-win32/sonic-annotator.exe";
        private string pluginPath = "../../External Programs/Vamp Plugins";
        private string fontPath = "";
        float correction = 0.0f;

        private Stopwatch _watch;

        private double _lag = 0.0;
        private double _dt = 16.0/1000;

        private Stage _stage;

        private IGameSettings _gameSettings;

        public GameController(IGameSettings gameSettings, int rX, int rY, GraphicsMode graphicsMode)
            : base(rX, rY, graphicsMode)
        {
            KeyDown += Keyboard_KeyDown;
            this.VSync = (bool) gameSettings["VSync"] ? VSyncMode.On : VSyncMode.Off;
            this.WindowState = (WindowState) gameSettings["WindowState"];
            _gameSettings = gameSettings;
        }

        #region Keyboard_KeyDown

        /// <summary>
        /// Occurs when a key is pressed.
        /// </summary>
        /// <param name="sender">The KeyboardDevice which generated this event.</param>
        /// <param name="e">The key that was pressed.</param>
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                _gameSettings["WindowState"] = WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
            }
        }

        #endregion

        protected override void OnKeyPress(OpenTK.KeyPressEventArgs e)
        {
            if (Focused)
                InputSystem.KeyPressed(e);
            base.OnKeyPress(e);
        }

        #region OnLoad

        /// <summary>
        /// Setup OpenGL and load resources here.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            var directoryHandler = new DirectoryHandler();
            directoryHandler.AddPath("Resources", @"..\..\Resources");
            directoryHandler.AddPath("Fonts", Path.Combine(directoryHandler["Resources"].FullName, @"Fonts"));
            directoryHandler.AddPath("Shaders", Path.Combine(directoryHandler["Resources"].FullName, @"Shaders"));
            directoryHandler.AddPath("Images", Path.Combine(directoryHandler["Resources"].FullName, @"Images"));

            fontPath = Path.Combine(directoryHandler["Fonts"].FullName, "./Chamgagne Limousines/Champagne & Limousines Italic.ttf");

            var gameCamera = new Camera(prefWidth, prefHeight, this.Width, this.Height, this.Mouse);
            gameCamera.CameraBounds = gameCamera.OriginalBounds = new Polygon(new Vector2(-prefWidth * 10, -prefHeight * 10), (int)prefWidth * 20, (int) (prefHeight * 20));
            var gameFont = new QFont(fontPath, 18, new QFontBuilderConfiguration(), FontStyle.Italic);
            _gameSceneManager = new SceneManager(this, gameCamera, gameFont, fontPath, directoryHandler, _gameSettings);
            _gameSceneManager.AddScene(new MenuScene(sonicAnnotator, pluginPath), null);

            Keyboard.KeyDown += (o, args) => InputSystem.KeyDown(args);
            Keyboard.KeyUp += (o, args) => InputSystem.KeyUp(args);
            Mouse.ButtonDown += (o, args) => InputSystem.MouseDown(args);
            Mouse.ButtonUp += (o, args) => InputSystem.MouseUp(args);
            Mouse.WheelChanged += (o, args) => InputSystem.MouseWheelChanged(args);
            Mouse.Move += (o, args) => InputSystem.MouseMoved(args);

            GL.ClearColor(Color.CornflowerBlue);

            _watch = new Stopwatch();
        }

        #endregion

        #region OnResize

        /// <summary>
        /// Respond to resize events here.
        /// </summary>
        /// <param name="e">Contains information on the new GameWindow size.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            _gameSceneManager.Resize(e);
        }


        #endregion

        #region OnUpdateFrame

        /// <summary>
        /// Add your game logic here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            //_stage.Update(e.Time);
            //_watch.Start();

            _lag += e.Time;
            while (_lag >= _dt)
            {
                _gameSceneManager.Update(_dt);

                InputSystem.Update(this.Focused);

                _lag -= _dt;
            }

            //_watch.Stop();

            //Debug.Write((_watch.ElapsedTicks/TimeSpan.TicksPerMillisecond).ToString("0.00") + ", ");

            //_watch.Reset();

            //Thread.Sleep(1);
        }

        #endregion

        #region OnRenderFrame

        /// <summary>
        /// Add your game rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnRenderFrame(FrameEventArgs e)
        {

            GL.Clear(ClearBufferMask.ColorBufferBit);

            _gameSceneManager.Draw(e.Time);

            this.SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            _gameSceneManager.Dispose();
            _gameSettings.Save();
            base.OnUnload(e);
        }

        #endregion

        #region public static void Main()

        /// <summary>
        /// Entry point of this example.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            IGameSettings gameSettings = new PropertySettings();
            gameSettings.Load();

            int rX = (int) gameSettings["ResolutionX"];
            int rY = (int) gameSettings["ResolutionY"];
            int FSAASamples = (int) gameSettings["AntiAliasingSamples"];
            GraphicsMode graphicsMode = new GraphicsMode(32, 24, 8, FSAASamples);

            using (GameController game = new GameController(gameSettings, rX, rY, graphicsMode))
            {
                game.Title = "Codename: turnt-ninja";
                game.Run();
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        #endregion
    }
}