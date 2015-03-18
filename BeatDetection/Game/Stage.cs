using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BeatDetection.Audio;
using BeatDetection.Core;
using BeatDetection.Generation;
using ClipperLib;
using ColorMine.ColorSpaces;
using NAudio.Wave;
using OpenTK;
using OpenTK.Input;
using QuickFont;
using Substructio.Core;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using Wav2Flac;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace BeatDetection.Game
{
    internal class Stage
    {
        public double TotalTime { get; private set; }
        private AudioFeatures _audioFeatures;

        private DifficultyOptions _difficultyOptions;

        private Random _random;

        public double Overlap { get; set; }

        private double _warmupTime = 2.0f;
        private double _elapsedWarmupTime;
        private double _easeInTime = 1.0f;
        public bool Running;
        public bool Ended;

        public bool AI { get; private set; }

        public ShaderProgram ShaderProgram { get; set; }
        public SceneManager SceneManager { get; set; }

        public QFont MultiplierFont;

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
        private StageAudio _stageAudio;

        public Stage(SceneManager sceneManager)
        {
            SceneManager = sceneManager;

            MultiplierFont = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Italic);
            MultiplierFont.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;

            _stageAudio = new StageAudio();
        }

        public void LoadAsync(string audioPath, string sonicPath, string pluginPath, float correction, IProgress<string> progress, PolarPolygon centerPolygon, Player player, DifficultyOptions difficultyOptions)
        {
            progress.Report("Loading audio");
            _stageAudio.Load(audioPath);
            _random = new Random(_stageAudio.AudioHashCode);

            progress.Report("Extracting audio features");
            LoadAudioFeatures(audioPath, sonicPath, pluginPath, correction, progress);

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
            StageGeometry.RotationMultiplier = _difficultyOptions.RotationSpeed;

            progress.Report("Load complete");

            Thread.Sleep(1000);
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
                if (_elapsedWarmupTime > _warmupTime)
                {
                    Running = true;
                    time = _elapsedWarmupTime - _warmupTime;
                }
            }
            if (Running)
            {
                TotalTime += time;
                if (StageGeometry.CurrentBeat == StageGeometry.BeatCount)
                {
                    Ended = true;
                    Running = false;
                }
            }
            if (!FinishedEaseIn && TotalTime > _easeInTime)
            {
                _stageAudio.Play();
                FinishedEaseIn = true;
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
            StageGeometry.Update(time);

            if (InputSystem.NewKeys.Contains(Key.F2)) AI = !AI;

            //Scale multiplier font with beat
            MultiplierFont.ProjectionMatrix = Matrix4.Mult(Matrix4.CreateScale((float)(0.75 + 0.24f * StageGeometry.CenterPolygon.PulseWidth / StageGeometry.CenterPolygon.PulseWidthMax)), SceneManager.ScreenCamera.ScreenProjectionMatrix);
        }

        public void Draw(double time)
        {
            StageGeometry.Draw(time);

            MultiplierFont.ResetVBOs();
            MultiplierFont.Print(string.Format("{0}x", Multiplier == -1 ? 0 : Multiplier), new Vector3(0, MultiplierFont.Measure("0", QFontAlignment.Centre).Height * 0.5f, 0),
                QFontAlignment.Centre);
            MultiplierFont.Draw();
        }
    }
}
