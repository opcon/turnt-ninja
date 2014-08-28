
using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using NAudio;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;

namespace BeatDetection
{
    /// <summary>
    /// Demonstrates the GameWindow class.
    /// </summary>
    public class Game : GameWindow
    {
        OnsetDetector detector;
        AudioWrapper audio;
        string audioFile = "..\\..\\test.wav";
        WaveOut waveOut;
        RawSourceWaveStream source;
        Stopwatch stopWatch;
        float tNext = 0;
        bool beatShown = false;
        float correction = 0.09f;
        float time = 0;

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

        #region OnLoad

        /// <summary>
        /// Setup OpenGL and load resources here.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color.CornflowerBlue);
            audio = new AudioWrapper(audioFile);
            detector = new QMVampWrapper(audio, @"../../test3.csv", correction);
            detector.DetectBeats();

            NAudio.Wave.WaveFormat fmt = new NAudio.Wave.WaveFormat(audio.AudioInfo.SampleRate, audio.AudioInfo.BitDepth, audio.AudioInfo.Channels);
            waveOut = new NAudio.Wave.WaveOut();
            source = new NAudio.Wave.RawSourceWaveStream(new MemoryStream(audio.AudioBuffer), fmt);
            waveOut.Init(source);

            stopWatch = new Stopwatch();
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
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
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
                time=0;
                stopWatch.Start();
            }
            if (waveOut.PlaybackState == PlaybackState.Playing && t > tNext)
            {
                foreach (var b in detector.Beats)
                {
                    float d = (float)(time - b);
                    if (d > 0f && d < 0.1f)
                    {
                        GL.ClearColor(Color.Green);
                        beatShown = true;
                        tNext = time + 0.1f;
                        break;
                    }
                    if (beatShown)
                    {
                        GL.ClearColor(Color.CornflowerBlue);
                        beatShown = false;
                    }
                }
            }
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

        #endregion
    }
}