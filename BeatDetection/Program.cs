
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
        float correction = 0.0f;
        float time = 0;

        Hexagon h;

        List<Hexagon> hexagons;
        List<Hexagon> toRemove;

        Random random;

        double[] angles;

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

            hexagons = new List<Hexagon>();
            toRemove = new List<Hexagon>();

            random = new Random();

            angles = new double[6];
            for (int i = 0; i < 6; i++)
			{
                angles[i] = (i+1) * (60) * (0.0174533);
			}

            foreach (var b in detector.Beats)
            {
                var start = random.Next(5);
                for (int i = 0; i < 5; i++)
                {
                    //hexagons.Add(new Hexagon(b, 300) { theta = angles[start] + angles[((i+start)%6)] });
                    hexagons.Add(new Hexagon(b, 300) { theta = angles[start] + i*angles[0] });
                }
                
            }
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
                foreach (var h in toRemove)
                {
                    hexagons.Remove(h);
                    GL.ClearColor(Color.Green);
                    tNext = time + 0.1f;
                }

                if (t > tNext)
                {
                    GL.ClearColor(Color.CornflowerBlue);
                }

                toRemove.Clear();
                foreach (var h in hexagons)
                {
                    h.Update(e.Time);
                    if (h.r <= h.impactDistance)
                        toRemove.Add(h);
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

            foreach (var h in hexagons)
            {
                h.Draw(e.Time);
            }

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