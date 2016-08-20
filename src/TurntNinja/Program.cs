using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using TurntNinja.Core.Settings;
using TurntNinja.Game;
using TurntNinja.GUI;
using TurntNinja.Logging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using QuickFont;
using QuickFont.Configuration;
using Substructio.Core;
using Substructio.Core.Settings;
using Substructio.GUI;
using Substructio.IO;
using Squirrel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TurntNinja
{
    /// <summary>
    /// Demonstrates the GameWindow class.
    /// </summary>
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
        private static CrashReporter _crashReporter;

        public ValueWrapper<bool> DebugMode = new ValueWrapper<bool>();

        public GameController(IGameSettings gameSettings, int rX, int rY, GraphicsMode graphicsMode,
                string title, int major, int minor, IDirectoryHandler directoryHandler)
            : base(rX, rY, graphicsMode, title, GameWindowFlags.Default, DisplayDevice.Default,
                    major, minor, GraphicsContextFlags.Default)
        {
            KeyDown += Keyboard_KeyDown;
            this.VSync = (bool)gameSettings["VSync"] ? VSyncMode.On : VSyncMode.Off;
            this.WindowState = (WindowState)gameSettings["WindowState"];
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
                _gameSettings["WindowState"] = WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
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
            var gameCamera = new Camera(prefWidth, prefHeight, Width, Height, Mouse);
            gameCamera.CameraBounds = gameCamera.OriginalBounds = new Polygon(new Vector2(-prefWidth * 10, -prefHeight * 10), (int)prefWidth * 20, (int)(prefHeight * 20));

            var fontLibrary = new FontLibrary();

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

            _gameSceneManager = new SceneManager(this, gameCamera, fontLibrary, bodyFontPath, _directoryHandler, _gameSettings, DebugMode);
            _gameSceneManager.AddScene(new MenuScene(), null);

            if ((bool)ServiceLocator.Settings["FirstRun"])
                _gameSceneManager.AddScene(new FirstRunScene(), null);

            Keyboard.KeyDown += (o, args) => InputSystem.KeyDown(args);
            Keyboard.KeyUp += (o, args) => InputSystem.KeyUp(args);
            Mouse.ButtonDown += (o, args) => InputSystem.MouseDown(args);
            Mouse.ButtonUp += (o, args) => InputSystem.MouseUp(args);
            Mouse.WheelChanged += (o, args) => InputSystem.MouseWheelChanged(args);
            Mouse.Move += (o, args) => InputSystem.MouseMoved(args);

            GL.ClearColor(Color.CornflowerBlue);

            _watch = new Stopwatch();
        }

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

        /// <summary>
        /// Add your game logic here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
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
            ServiceLocator.Analytics.TrackApplicationShutdown();
            _gameSceneManager.Dispose();
            _gameSettings["Debug"] = DebugMode;
            _gameSettings.Save();
            base.OnUnload(e);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            // Load services
            ServiceLocator.Settings = new PropertySettings();
            ServiceLocator.Settings.Load();

            // DEBUG SETTINGS - Force first run
            //ServiceLocator.Settings["Analytics"] = false;
            //ServiceLocator.Settings["FirstRun"] = true;

            string PiwikURL = AESEncryption.Decrypt((string)ServiceLocator.Settings["Piwik"]);
            string SentryURL = AESEncryption.Decrypt((string)ServiceLocator.Settings["Sentry"]);

#if DEBUG
            string sentryEnvironment = "debug";
#else
            string sentryEnvironment = "release";
#endif
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string gameVersion = $"{version.Major}.{version.Minor}.{version.Build}";

            // Generate or load user ID for Piwik
            Guid userGUID;

            var userID = (string)ServiceLocator.Settings["UserID"];
            if (!Guid.TryParse(userID, out userGUID)) userGUID = Guid.NewGuid();

            // Save user GUID
            ServiceLocator.Settings["UserID"] = userGUID.ToString();

            Platform runningPlatform = PlatformDetection.RunningPlatform();
            string platformVersion = PlatformDetection.GetVersionName();

            // Load Sentry service
            if ((bool)ServiceLocator.Settings["Analytics"] || (bool)ServiceLocator.Settings["FirstRun"])
            {
                ServiceLocator.ErrorReporting = new SentryErrorReporting(
                    SentryURL,
                    sentryEnvironment,
                    gameVersion,
                    userGUID.ToString(),
                    runningPlatform,
                    platformVersion);
            }

            int PiwikAppID = 3;

            // Load Piwik service
            if ((bool)ServiceLocator.Settings["Analytics"] || (bool)ServiceLocator.Settings["FirstRun"])
            {
                ServiceLocator.Analytics = new PiwikAnalytics(
                    PiwikAppID,
                    PiwikURL,
                    runningPlatform,
                    platformVersion,
                    DisplayDevice.Default.Width,
                    DisplayDevice.Default.Height,
                    gameVersion,
                    userGUID.ToString(),
                    "http://turntninja");
            }

            ServiceLocator.Directories = new DirectoryHandler();

            var directoryHandler = ServiceLocator.Directories;
            var gameSettings = ServiceLocator.Settings;

            //set application path
            directoryHandler.AddPath("Application", AppDomain.CurrentDomain.BaseDirectory);
            //set base path
            if (Directory.Exists(directoryHandler.Locate("Application", "Content")))
                directoryHandler.AddPath("Base", directoryHandler["Application"].FullName);
            else if (Directory.Exists(directoryHandler.Locate("Application", @"../../src/TurntNinja/Content")))
                directoryHandler.AddPath("Base", directoryHandler.Locate("Application", @"../../src/TurntNinja"));
            else
            {
                throw new Exception("Couldn't find resource folder location");
            }

            directoryHandler.AddPath("AppData",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), (string)gameSettings["AppDataFolderName"]));

            directoryHandler.AddPath("Resources", Path.Combine(directoryHandler["Base"].FullName, "Content"));
            directoryHandler.AddPath("Fonts", Path.Combine(directoryHandler["Resources"].FullName, @"Fonts"));
            directoryHandler.AddPath("Shaders", Path.Combine(directoryHandler["Resources"].FullName, @"Shaders"));
            directoryHandler.AddPath("Images", Path.Combine(directoryHandler["Resources"].FullName, @"Images"));
            directoryHandler.AddPath("Crash", Path.Combine(directoryHandler["AppData"].FullName, "CrashLogs"));
            directoryHandler.AddPath("BundledSongs", Path.Combine(directoryHandler["Resources"].FullName, @"Songs"));
            directoryHandler.AddPath("ProcessedSongs", Path.Combine(directoryHandler["AppData"].FullName, @"Processed Songs"));

            if (!Directory.Exists(directoryHandler["AppData"].FullName)) Directory.CreateDirectory(directoryHandler["AppData"].FullName);

            if (!Debugger.IsAttached)
            {
                //initialise crash reporter
                _crashReporter = new CrashReporter(directoryHandler["Crash"].FullName);

                //attach exception handlers
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }

            //init logging
            Splat.DependencyResolverMixins.RegisterConstant(Splat.Locator.CurrentMutable,
                new SimpleLogger(directoryHandler["Application"].FullName, Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".log")
                {
                    Level = Splat.LogLevel.Debug
                },
                typeof(Splat.ILogger));

            int rX = (int)gameSettings["ResolutionX"];
            int rY = (int)gameSettings["ResolutionY"];
            int FSAASamples = (int)gameSettings["AntiAliasingSamples"];
            GraphicsMode graphicsMode = new GraphicsMode(32, 24, 8, FSAASamples, GraphicsMode.Default.AccumulatorFormat, 3);

            // Choose right OpenGL version for mac
            int major = 3;
            int minor = 0;
            if (PlatformDetection.RunningPlatform() == Platform.MacOSX)
                major = 4;

            if ((bool)ServiceLocator.Settings["Analytics"] || (bool)ServiceLocator.Settings["FirstRun"])
                ServiceLocator.Analytics.TrackApplicationStartup();

            using (GameController game = new GameController(gameSettings, rX, rY, graphicsMode, "Turnt Ninja",
                        major, minor, directoryHandler))
            {
                game.Title = "Turnt Ninja";
                game.Run();
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;

                // Try to report exception
                try
                {
                    string eventID = ServiceLocator.ErrorReporting.ReportError(ex);
                    
                    // Try to log crash ID
                    try
                    {
                        ServiceLocator.Analytics.TrackEvent("Crash", "Fatal Crash", eventID);
                    }
                    catch { }
                }
                catch { }

                // Try to save crash report
                try
                {
                    _crashReporter.LogError(ex);
                }
                catch { }

                // Try to report application shutdown
                try
                {
                    ServiceLocator.Analytics.TrackApplicationShutdown();
                }
                catch { }
            }
            finally
            {
                Environment.Exit(-1);
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
    }
}
