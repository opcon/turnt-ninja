
using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using NAudio;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

namespace BeatDetection
{
    /// <summary>
    /// Demonstrates the GameWindow class.
    /// </summary>
    public class Game : GameWindow
    {
        OnsetDetector detector;
        AudioWrapper audio;
        string audioFile = "./sun.wav";
        string sonicAnnotator = @"D:\Patrick\Dropbox\Dev\Beat Detection Research\sonic-annotator-1.0-win32\sonic-annotator.exe";
        WaveOut waveOut;
        RawSourceWaveStream source;
        Stopwatch stopWatch;
        float tNext = 0;
        bool beatShown = false;
        float correction = 0.0f;
        float time = 0;

        Hexagon hexagon;

        List<HexagonSide> hexagonSides;
        List<HexagonSide> toRemove;

        Random random;

        double[] angles;

        internal static bool focused = true;

        Player p;


        public Game()
            : base(1024, 768)
        {
            KeyDown += Keyboard_KeyDown;
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
                this.Exit();

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
            string file = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Audio Files (*.mp3, *.flac, *.wav)|*.mp3;*.flac;*.wav|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                file = ofd.FileName;
                file = file.Replace(@"\", "/");
                //file.Replace("\\", "/");
            }
            else
            {
                this.Exit();
                return;
            }

            Keyboard.KeyDown += (o, args) => InputSystem.KeyDown(args);
            Keyboard.KeyUp += (o, args) => InputSystem.KeyUp(args);
            Mouse.ButtonDown += (o, args) => InputSystem.MouseDown(args);
            Mouse.ButtonUp += (o, args) => InputSystem.MouseUp(args);
            Mouse.WheelChanged += (o, args) => InputSystem.MouseWheelChanged(args);
            Mouse.Move += (o, args) => InputSystem.MouseMoved(args);

            GL.ClearColor(Color.CornflowerBlue);
            audio = new AudioWrapper(audioFile);
            //detector = new QMVampWrapper(audio, "./sun2.csv", correction);


            detector = new QMVampWrapper(null, file, sonicAnnotator, correction);

            detector.DetectBeats();

            AudioFileReader audioReader = new AudioFileReader(file);
            waveOut = new WaveOut();
            waveOut.Init(audioReader);

            //NAudio.Wave.WaveFormat fmt = new NAudio.Wave.WaveFormat(audio.AudioInfo.SampleRate, audio.AudioInfo.BitDepth, audio.AudioInfo.Channels);
            //waveOut = new NAudio.Wave.WaveOut();
            //source = new NAudio.Wave.RawSourceWaveStream(new MemoryStream(audio.AudioBuffer), fmt);
            //waveOut.Init(source);

            stopWatch = new Stopwatch();

            hexagonSides = new List<HexagonSide>();
            toRemove = new List<HexagonSide>();

            random = new Random();

            angles = new double[6];
            for (int i = 0; i < 6; i++)
			{
                angles[i] = (i+1) * (60) * (0.0174533);
			}

            int prevStart = random.Next(5);
            double prevTime = 0;
            foreach (var b in detector.Beats.Where((value, index) => index % 1 == 0))
            {
                int start = 0;
                if (b - prevTime < 0.4)
                {
                    start = prevStart;
                    prevTime = b;
                }
                else
                {
                    start = random.Next(5);
                }
                for (int i = 0; i < 5; i++)
                {
                    //hexagons.Add(new Hexagon(b, 300) { theta = angles[start] + angles[((i+start)%6)] });
                    hexagonSides.Add(new HexagonSide(b, 300, angles[start] + i * angles[0] , 125));
                }
                prevTime = b;
                //hexagons.Add(new Hexagon(b, 300) { theta = angles[0] });
                //hexagons.Add(new Hexagon(b, 300) { theta = angles[1] });
                //hexagons.Add(new Hexagon(b, 300) { theta = angles[2] });
                //hexagons.Add(new Hexagon(b, 300) { theta = angles[3] });
                
            }

            hexagon = new Hexagon(6, 0, 1, 0, 80);
            p = new Player();
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

            GL.MatrixMode(MatrixMode.Projection);
            var mat = Matrix4.CreateOrthographic(1024, 768, 0.0f, 4.0f);
            GL.LoadMatrix(ref mat);
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

            var t = (float)stopWatch.Elapsed.TotalSeconds;
            time += (float)e.Time;
            if (waveOut.PlaybackState != PlaybackState.Playing)
            {
                waveOut.Play();
                time = 0;
                stopWatch.Start();
            }
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                p.Update(e.Time);
                foreach (var h in toRemove)
                {
                    hexagonSides.Remove(h);
                    //GL.ClearColor(Color.Green);
                    tNext = time + 0.1f;
                }

                if (t > tNext)
                {
                    GL.ClearColor(Color.CornflowerBlue);
                }

                toRemove.Clear();
                foreach (var h in hexagonSides)
                {
                    h.Update(e.Time);
                    if (h.r <= h.impactDistance)
                        toRemove.Add(h);
                    else if (((h.r - h.impactDistance) / h.speed) < (hexagon.pulseWidthMax / hexagon.pulseMultiplier))
                        hexagon.pulsing = true;
                }

                if (toRemove.Count > 0)
                {
                    hexagon.Pulse(e.Time);
                }

                hexagon.Update(e.Time, false);
                //if (InputSystem.CurrentKeys.Contains(Key.Left))
                //    hexagon.Rotate(e.Time);
                //else if (InputSystem.CurrentKeys.Contains(Key.Right))
                //    hexagon.Rotate(-e.Time*1.5);

            }

            InputSystem.Update();
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

            foreach (var h in hexagonSides)
            {
                h.Draw(e.Time);
            }

            hexagon.Draw(e.Time);
            p.Draw(e.Time);

            this.SwapBuffers();
        }

        #endregion

        #region public static void Main()

        /// <summary>
        /// Entry point of this example.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            using (Game game = new Game())
            {
                // Get the title and category  of this example using reflection.
                game.Title = "BeatTest";
                game.Run(30.0, 0.0);
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