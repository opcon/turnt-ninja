
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
using QuickFont;
using Substructio.Core;
using Substructio.GUI;

namespace BeatDetection
{
    /// <summary>
    /// Demonstrates the GameWindow class.
    /// </summary>
    public class GameController : GameWindow
    {

        private SceneManager _gameSceneManager;
        private const float prefWidth = 1920;
        private const float prefHeight = 1080;

        OnsetDetector detector;
        private string sonicAnnotator = "../../External Programs/sonic-annotator-1.0-win32/sonic-annotator.exe";
        private string pluginPath = "../../External Programs/Vamp Plugins";
        private string fontPath = Directories.FontsDirectory + "./Chamgagne Limousines/Champagne & Limousines Italic.ttf";
        WaveOut waveOut;
        RawSourceWaveStream source;
        Stopwatch stopWatch;
        float tNext = 0;
        bool beatShown = false;
        float correction = 0.25f;
        float time = 0;

        PolarPolygon _polarPolygon;

        //List<PolarPolygonSide> hexagonSides;
        //List<PolarPolygonSide> toRemove;

        Random random;

        double[] angles;

        Player p;

        IWaveProvider prov;

        private int dir = 1;

        private Stopwatch _watch;

        private double _lag = 0.0;
        private double _dt = 0.01;

        private Stage _stage;

        private double MaxFrameTime;
        public GameController()
            : base(1280, 720, new GraphicsMode(32, 24, 8, 4))
        {
            KeyDown += Keyboard_KeyDown;
            this.VSync = VSyncMode.Off;
        }

        #region Keyboard_KeyDown

        /// <summary>
        /// Occurs when a key is pressed.
        /// </summary>
        /// <param name="sender">The KeyboardDevice which generated this event.</param>
        /// <param name="e">The key that was pressed.</param>
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Exit();
            }

            if (e.Key == Key.F11)
                if (this.WindowState == WindowState.Fullscreen)
                    this.WindowState = WindowState.Normal;
                else
                    this.WindowState = WindowState.Fullscreen;
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

            var gameCamera = new Camera(prefWidth, prefHeight, this.Width, this.Height, this.Mouse);
            gameCamera.CameraBounds = gameCamera.OriginalBounds = new Polygon(new Vector2(-prefWidth * 10, -prefHeight * 10), (int)prefWidth * 20, (int) (prefHeight * 20));
            var gameFont = new QFont(fontPath, 18, new QFontBuilderConfiguration(), FontStyle.Italic){ProjectionMatrix = gameCamera.ScreenProjectionMatrix};
            _gameSceneManager = new SceneManager(this, gameCamera, gameFont, fontPath);
            _gameSceneManager.AddScene(new LoadingScene(sonicAnnotator, pluginPath, correction));



            //string file = "";
            //OpenFileDialog ofd = new OpenFileDialog();
            //ofd.Multiselect = false;
            //ofd.Filter = "Audio Files (*.mp3, *.flac, *.wav)|*.mp3;*.flac;*.wav|All Files (*.*)|*.*";
            //if (ofd.ShowDialog() == DialogResult.OK)
            //{
            //    file = ofd.FileName;
            //    file = file.Replace(@"\", "/");
            //    //file.Replace("\\", "/");
            //}
            //else
            //{
            //    this.Exit();
            //    return;
            //}

            Keyboard.KeyDown += (o, args) => InputSystem.KeyDown(args);
            Keyboard.KeyUp += (o, args) => InputSystem.KeyUp(args);
            Mouse.ButtonDown += (o, args) => InputSystem.MouseDown(args);
            Mouse.ButtonUp += (o, args) => InputSystem.MouseUp(args);
            Mouse.WheelChanged += (o, args) => InputSystem.MouseWheelChanged(args);
            Mouse.Move += (o, args) => InputSystem.MouseMoved(args);

            GL.ClearColor(Color.CornflowerBlue);

            _watch = new Stopwatch();

            //_stage = new Stage();
            //_stage.LoadAsync(file, sonicAnnotator, pluginPath, correction);

            MaxFrameTime = 1/DisplayDevice.Default.RefreshRate;
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


            //GL.MatrixMode(MatrixMode.Projection);
            //var mat = Matrix4.CreateOrthographic(Width, Height, 0.0f, 4.0f);
            //GL.LoadMatrix(ref mat);

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
            //Debug.Write((e.Time * 1000).ToString("0.00") + ", ");

            GL.Clear(ClearBufferMask.ColorBufferBit);

            //_stage.Draw(e.Time);
            //GL.MatrixMode(MatrixMode.Modelview);
            //GL.LoadIdentity();
            //GL.Translate(0.375, 0.375, 0.0);
            _gameSceneManager.Draw(e.Time);

            this.SwapBuffers();

            //TODO FIX THIS HACKY SCREEN TEARING REDUCTION HACK!
            if (e.Time < (MaxFrameTime) - 0.001)
                Thread.Sleep((int)(MaxFrameTime*1000) - (int)(e.Time*1000) - 1);
        }

        #endregion

        #region public static void Main()

        /// <summary>
        /// Entry point of this example.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            using (GameController game = new GameController())
            {
                // Get the title and category  of this example using reflection.
                game.Title = "turnt-ninja";
                game.Run(60.0, 0.0);
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