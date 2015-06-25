using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using BeatDetection.Audio;
using BeatDetection.Core;
using BeatDetection.Generation;
using OpenTK;
using OpenTK.Input;
using QuickFont;
using Substructio.Core;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;

namespace BeatDetection.Game
{
    internal class Stage : IDisposable
    {
        public double TotalTime { get; private set; }
        public double EndTime { get; private set; }
        private AudioFeatures _audioFeatures;

        private DifficultyOptions _difficultyOptions;

        private Random _random;

        public double Overlap { get; set; }

        private double _warmupTime = 2.0f;
        private double _elapsedWarmupTime;
        private double _easeInTime = 2.0f;
        public bool Running;
        public bool Ended;

        public bool AI { get; private set; }

        public ShaderProgram ShaderProgram { get; set; }
        public SceneManager SceneManager { get; set; }

        public QFont MultiplierFont;
        public QFontDrawing MultiplierFontDrawing;
        private string _centerText = "";

        public bool Loaded { get; private set; }

        public int Hits
        {
            get { return StageGeometry.Player.Hits; }
        }

        public int Multiplier { get; set; }

        public int CurrentPolygon
        {
            get {return StageGeometry.CurrentBeat;}
        }

        public int PolygonCount
        {
            get { return StageGeometry.BeatCount; }
        }

        public float ScoreMultiplier
        {
            get { return _difficultyOptions.GetScoreMultiplier(); }
        }

        public bool FinishedEaseIn { get; private set; }

        public StageGeometry StageGeometry;
        public StageAudio _stageAudio;

        private const float WADSWORTH = 0.30f;

        public Stage(SceneManager sceneManager)
        {
            SceneManager = sceneManager;

            MultiplierFont = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Italic);
            MultiplierFontDrawing = new QFontDrawing();
            MultiplierFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;

            _stageAudio = new StageAudio();
        }

        public void LoadAsync(string audioPath, string sonicPath, string pluginPath, float audioCorrection, float maxAudioVolume, IProgress<string> progress, PolarPolygon centerPolygon, Player player, DifficultyOptions difficultyOptions)
        {
            progress.Report("Loading audio");
            _stageAudio.Load(audioPath);
            _stageAudio.MaxVolume = maxAudioVolume;
            _random = new Random(_stageAudio.AudioHashCode);

            _stageAudio.Volume = 0.0f;
            _stageAudio.Seek(WADSWORTH);
            _stageAudio.Play();
            _stageAudio.FadeIn(1000, _stageAudio.MaxVolume*0.5f, 0.01f, 0);

            progress.Report("Extracting audio features");
            LoadAudioFeatures(audioPath, sonicPath, pluginPath, audioCorrection, progress);

            progress.Report("Building stage geometry");

            //Apply difficulty options to builder options
            var bOptions = new GeometryBuilderOptions(ShaderProgram);
            bOptions.ApplyDifficulty(difficultyOptions);
            _difficultyOptions = difficultyOptions;

            //Build stage geometry
            StageGeometry = new StageGeometryBuilder().Build(_audioFeatures, _random, bOptions);
            StageGeometry.ParentStage = this;

            StageGeometry.CenterPolygon = centerPolygon;
            StageGeometry.Player = player;
            StageGeometry.RotationSpeed = _difficultyOptions.RotationSpeed;

            progress.Report("Load complete");

            Thread.Sleep(1000);

            //cancellationTokenSource.Cancel();
            _stageAudio.CancelAudioFades();
            _stageAudio.FadeOut(500, 0.0f, 0.01f, 2);

            Loaded = true;
        }

        private void LoadAudioFeatures(string audioPath, string sonicPath, string pluginPath, float correction, IProgress<string> progress)
        {
            _audioFeatures = new AudioFeatures(sonicPath, pluginPath, "../../Processed Songs/", correction + (float)_easeInTime, progress);
            _audioFeatures.Extract(audioPath);
        }

        public void Update(double time)
        {
            if (!Running && !Ended)
            {
                SceneManager.ScreenCamera.TargetScale = new Vector2(1.3f);
                _elapsedWarmupTime += time;
                _centerText = (Math.Ceiling(_easeInTime + _warmupTime - _elapsedWarmupTime)).ToString();
                if (_elapsedWarmupTime > _warmupTime)
                {
                    Running = true;
                    time = _elapsedWarmupTime - _warmupTime;
                }
            }

            if (Running || Ended)
            {
                TotalTime += time;

                if (Running)
                {
                    if (StageGeometry.CurrentBeat == StageGeometry.BeatCount && _stageAudio.IsStopped)
                    {
                        EndTime = TotalTime;
                        Ended = true;
                        Running = false;
                    }
                    _centerText = string.Format("{0}x", Multiplier == -1 ? 0 : Multiplier);

                    if (!FinishedEaseIn)
                    {
                        _centerText = (Math.Ceiling(_easeInTime - TotalTime)).ToString();
                        if (TotalTime > _easeInTime)
                        {
                            _stageAudio.Volume = _stageAudio.MaxVolume;
                            _stageAudio.Play();
                            FinishedEaseIn = true;
                        }
                    }
                }
            }

            if (StageGeometry.CurrentBeat < StageGeometry.BeatCount)
            {
                SceneManager.ScreenCamera.TargetScale =
                    new Vector2(0.9f*
                                (0.80f +
                                 Math.Min(1,
                                     ((StageGeometry.BeatFrequencies[StageGeometry.CurrentBeat] - StageGeometry.MinBeatFrequency)/(StageGeometry.MaxBeatFrequency - StageGeometry.MinBeatFrequency))*
                                     0.5f)));
                SceneManager.ScreenCamera.ScaleChangeMultiplier = Math.Min(StageGeometry.BeatFrequencies[StageGeometry.CurrentBeat], 2)*2;
            }

            if (!InputSystem.CurrentKeys.Contains(Key.F3))
                StageGeometry.Update(time);

            if (InputSystem.NewKeys.Contains(Key.F2)) AI = !AI;

            //Scale multiplier font with beat
            MultiplierFontDrawing.ProjectionMatrix = Matrix4.Mult(Matrix4.CreateScale((float)(0.75 + 0.24f * StageGeometry.CenterPolygon.PulseWidth / StageGeometry.CenterPolygon.PulseWidthMax)), SceneManager.ScreenCamera.ScreenProjectionMatrix);

        }

        public void Draw(double time)
        {
            StageGeometry.Draw(time);

            MultiplierFontDrawing.DrawingPrimitives.Clear();
            MultiplierFontDrawing.Print(MultiplierFont, _centerText, new Vector3(0, MultiplierFont.Measure("0", QFontAlignment.Centre).Height * 0.5f, 0),
                QFontAlignment.Centre);
            MultiplierFontDrawing.RefreshBuffers();
            MultiplierFontDrawing.Draw();
        }

        public void Dispose()
        {
            MultiplierFont.Dispose();
            MultiplierFontDrawing.Dispose();
            StageGeometry.Dispose();
        }

        public void Reset()
        {
            _stageAudio.FadeOut(1000, 0, 0.01f, 2);
            StageGeometry.CenterPolygon.Position.Azimuth = 0;
            //reset hit hexagons
            StageGeometry.Player.Hits = 0;
        }
    }
}
