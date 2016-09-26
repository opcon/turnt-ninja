using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TurntNinja.Core.Settings;
using TurntNinja.Logging;
using OpenTK;
using OpenTK.Graphics;
using Substructio.Core;
using Substructio.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TurntNinja
{
    public static class TurntNinjaGame
    {

        private static CrashReporter _crashReporter;

        [STAThread]
        public static void Main(string[] args)
        {
            // Load services
            var initialSettingsProvider = new PropertySettings();
            initialSettingsProvider.Load();

            // Default settings provider is the Windows application settings implementation
            ServiceLocator.Settings = initialSettingsProvider;

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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), (string)ServiceLocator.Settings["AppDataFolderName"]));

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

            // If we are not running on a windows platform, use the JsonSettings backend
            if (PlatformDetection.RunningPlatform() != Platform.Windows)
            {
                ServiceLocator.Settings = new JsonSettings(initialSettingsProvider, directoryHandler.Locate("AppData", "settings.json"));
                ServiceLocator.Settings.Load();
            }

            //init logging
            Splat.DependencyResolverMixins.RegisterConstant(Splat.Locator.CurrentMutable,
                new SimpleLogger(directoryHandler["Application"].FullName, Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".log")
                {
                    Level = Splat.LogLevel.Debug
                },
                typeof(Splat.ILogger));

            int rX = (int)ServiceLocator.Settings["ResolutionX"];
            int rY = (int)ServiceLocator.Settings["ResolutionY"];
            int FSAASamples = (int)ServiceLocator.Settings["AntiAliasingSamples"];
            GraphicsMode graphicsMode = new GraphicsMode(32, 32, 8, FSAASamples, GraphicsMode.Default.AccumulatorFormat, 3);

            // Choose right OpenGL version for mac
            int major = 3;
            int minor = 0;
            if (PlatformDetection.RunningPlatform() == Platform.MacOSX)
                major = 4;

            if ((bool)ServiceLocator.Settings["Analytics"])
                ServiceLocator.Analytics.TrackApplicationStartup();

            using (GameController game = new GameController(ServiceLocator.Settings, rX, rY, graphicsMode, "Turnt Ninja",
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
