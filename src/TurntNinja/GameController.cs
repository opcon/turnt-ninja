using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using TurntNinja.Game;
using TurntNinja.GUI;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using QuickFont;
using QuickFont.Configuration;
using Substructio.Core;
using Substructio.Core.Settings;
using Substructio.GUI;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TurntNinja
{
    public sealed class GameController : GameWindow
    {
        private SceneManager _gameSceneManager;
        private const float prefWidth = 1920;
        private const float prefHeight = 1080;

        float correction = 0.0f;

        private Stopwatch _watch;

        private double _lag = 0.0;
        private double _dt = 16.0 / 1000;

        private Stage _stage;

        private IGameSettings _gameSettings;
        private IDirectoryHandler _directoryHandler;

        public ValueWrapper<bool> DebugMode = new ValueWrapper<bool>();

        private const int MIN_WIDTH = 600;
        private const int MIN_HEIGHT = 400;

        public GameController(IGameSettings gameSettings, int rX, int rY, GraphicsMode graphicsMode,
            string title, int major, int minor, IDirectoryHandler directoryHandler)
            : base(rX, rY, graphicsMode, title, GameWindowFlags.Default, DisplayDevice.Default,
                major, minor, GraphicsContextFlags.Default)
        {
            // Add OpenGL/GPU information tags to sentry
            var glVersion = GL.GetString(StringName.Version);
            var glslVersion = GL.GetString(StringName.ShadingLanguageVersion);
            var gpuVendor = GL.GetString(StringName.Vendor);
            var renderer = GL.GetString(StringName.Renderer);
            ServiceLocator.ErrorReporting.AddTags(new Dictionary<string, string>
            {
                { "opengl", glVersion },
                { "glsl", glslVersion },
                { "vendor", gpuVendor },
                { "gpu", renderer }
            });

            // Add backend information tags to sentry
            var sdl = Configuration.RunningOnSdl2;
            Console.WriteLine("Running on SDL2: {0}", sdl);
            ServiceLocator.ErrorReporting.AddTags(new Dictionary<string, string>
            {
                { "sdl", sdl.ToString() }
            });

            KeyDown += Keyboard_KeyDown;
            this.VSync = (bool)gameSettings["VSync"] ? VSyncMode.On : VSyncMode.Off;
            this.WindowState = (WindowState)Enum.Parse(typeof(WindowState), (string)gameSettings["WindowState"]);
            DebugMode.Value = (bool)gameSettings["Debug"];
            _gameSettings = gameSettings;
            _directoryHandler = directoryHandler;
        }

        /// <summary>
        /// Occurs when a key is pressed.
        /// </summary>
        /// <param name="sender">The KeyboardDevice which generated this event.</param>
        /// <param name="e">The key that was pressed.</param>
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
                _gameSettings["WindowState"] = WindowState.ToString();
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (Focused)
                InputSystem.KeyPressed(e);
            base.OnKeyPress(e);
        }

        /// <summary>
        /// Setup OpenGL and load resources here.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            //fontPath = Path.Combine(_directoryHandler["Fonts"].FullName, "./Chamgagne Limousines/Champagne & Limousines Italic.ttf");
            //fontPath = Path.Combine(_directoryHandler["Fonts"].FullName, "./Ostrich Sans/OstrichSans-Black.otf");
            var menuFontPath = Path.Combine(_directoryHandler["Fonts"].FullName, "./Oswald-Bold.ttf");
            var versionFontPath = _directoryHandler.Locate("Fonts", "./Oswald-Regular.ttf");
            var bodyFontPath = _directoryHandler.Locate("Fonts", "./Open Sans/OpenSans-Light.ttf");
            var largeBodyFontPath = _directoryHandler.Locate("Fonts", "./Open Sans/OpenSans-Regular.ttf");
            //var selectedFontPath = _directoryHandler.Locate("Fonts", "./Open Sans/OpenSans-Bold.ttf");
            var selectedFontPath = Path.Combine(_directoryHandler["Fonts"].FullName, "./Oswald-Bold.ttf");

            Console.WriteLine("Initializing");

            Console.WriteLine("Creating game camera");
            var gameCamera = new Camera(prefWidth, prefHeight, Width, Height, Mouse);
            gameCamera.CameraBounds = gameCamera.OriginalBounds = new Polygon(new Vector2(-prefWidth * 10, -prefHeight * 10), (int)prefWidth * 20, (int)(prefHeight * 20));

            // Register OSX codecs if running on OSX
            if (PlatformDetection.RunningPlatform() == Platform.MacOSX)
            {
                Console.WriteLine("Registering OSX Audio codecs");
                CSCore.OSXCoreAudio.OSXAudio.RegisterCodecs();
            }

            Console.WriteLine("Creating font library");
            var fontLibrary = new FontLibrary();

            Console.WriteLine("Loading fonts");
            // Default font
            var gameFont = new QFont(bodyFontPath, 18, new QFontBuilderConfiguration() { SuperSampleLevels = 2 });
            fontLibrary.AddFont(new GameFont(gameFont, GameFontType.Default, new Vector2(Width, Height)));

            // Menu font
            fontLibrary.AddFont(
                new GameFont(new QFont(menuFontPath, 50, new QFontBuilderConfiguration(true)),
                    GameFontType.Menu,
                    new Vector2(Width, Height)));

            // Menu World font
            fontLibrary.AddFont(
                new GameFont(new QFont(menuFontPath, 70, new QFontBuilderConfiguration(true) { SuperSampleLevels = 2, PageMaxTextureSize =  8192, Characters = CharacterSet.BasicSet }),
                    "menuworld",
                    new Vector2(Width, Height)));

            // Heading font
            fontLibrary.AddFont(
                new GameFont(new QFont(menuFontPath, 20, new QFontBuilderConfiguration(true) { SuperSampleLevels = 2 }),
                    GameFontType.Heading,
                    new Vector2(Width, Height)));

            // Large body font
            fontLibrary.AddFont(
                new GameFont(new QFont(largeBodyFontPath, 25, new QFontBuilderConfiguration() { SuperSampleLevels = 1 }),
                    "largebody",
                    new Vector2(Width, Height)));

            // Version text font
            fontLibrary.AddFont(
                new GameFont(new QFont(versionFontPath, 15, new QFontBuilderConfiguration()),
                    "versiontext",
                    new Vector2(Width, Height)));

            // Selected text font (song browser
            fontLibrary.AddFont(
                new GameFont(new QFont(selectedFontPath, 34, new QFontBuilderConfiguration() { SuperSampleLevels = 2, Characters = CharacterSet.General }),
                    "selected",
                    new Vector2(Width, Height)));

            Console.WriteLine("Creating scene manager");
            _gameSceneManager = new SceneManager(this, gameCamera, fontLibrary, bodyFontPath, _directoryHandler, _gameSettings, DebugMode);
            _gameSceneManager.AddScene(new MenuScene(), null);

            Console.WriteLine("Checking for first run scene");
            if ((bool)ServiceLocator.Settings["FirstRun"])
                _gameSceneManager.AddScene(new FirstRunScene(), null);

            Console.WriteLine("Setting up input system callbacks");
            Keyboard.KeyDown += (o, args) => InputSystem.KeyDown(args);
            Keyboard.KeyUp += (o, args) => InputSystem.KeyUp(args);
            Mouse.ButtonDown += (o, args) => InputSystem.MouseDown(args);
            Mouse.ButtonUp += (o, args) => InputSystem.MouseUp(args);
            Mouse.WheelChanged += (o, args) => InputSystem.MouseWheelChanged(args);
            Mouse.Move += (o, args) => InputSystem.MouseMoved(args);

            Console.WriteLine("Setting clear color");
            GL.ClearColor(Color.Black);

            Console.WriteLine("Creating stopwatch");
            _watch = new Stopwatch();

            Console.WriteLine("Finished OnLoad");
        }

        /// <summary>
        /// Respond to resize events here.
        /// </summary>
        /// <param name="e">Contains information on the new GameWindow size.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnResize(EventArgs e)
        {
            if (Size.Width < MIN_WIDTH)
                Size = new Size(MIN_WIDTH, Size.Height);
            if (Size.Height < MIN_HEIGHT)
                Size = new Size(Size.Width, MIN_HEIGHT);

            GL.Viewport(0, 0, Width, Height);

            _gameSceneManager.Resize(e);
        }

        /// <summary>
        /// Add your game logic here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (_gameSceneManager.ExitRequested)
            {
                Console.WriteLine("Exiting");
                Exit();
            }

            _lag += e.Time;
            while (_lag >= _dt)
            {
                _gameSceneManager.Update(_dt);
                if (InputSystem.NewKeys.Contains(Key.F12))
                    DebugMode.Value = !DebugMode.Value;

                InputSystem.Update(this.Focused, _dt);

                _lag -= _dt;
            }
        }

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
            try
            {
                Console.WriteLine("Unloading app");
                ServiceLocator.Analytics.TrackApplicationShutdown();
                _gameSceneManager.Dispose();
                _gameSettings["Debug"] = DebugMode.Value;
                ServiceLocator.Settings.Save();
                base.OnUnload(e);
                Console.WriteLine("Finished unloading");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught while unloading:");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}