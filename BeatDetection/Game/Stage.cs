﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BeatDetection.Audio;
using BeatDetection.Core;
using ClipperLib;
using ColorMine.ColorSpaces;
using NAudio.Wave;
using OpenTK;
using OpenTK.Input;
using Substructio.Core;
using Substructio.Core.Math;
using Wav2Flac;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BeatDetection.Game
{
    internal class Stage
    {
        private double _totalTime;
        private PolarPolygon[] _polygons;
        private AudioFeatures _audioFeatures;
        private int _polygonIndex;
        private int _polygonsToRemoveCount;
        private Color4[] _segmentColours;
        private SegmentInformation[] _segments;
        private int _colourIndex;
        private int _segmentIndex;

        private WaveOut _waveOut;
        private IWaveProvider _waveProvider;

        private Random _random;
        private int _hashCode;
        private const int HashCount = 10000;

        private Player _player;
        private PolarPolygon _centerPolygon;
        private int _direction;

        public double Overlap;
        private int _collidedPolygonIndex = -1;

        private double _warmupTime = 2.0f;
        private double _elapsedWarmupTime;
        private double _easeInTime = 1.0f;
        private bool _finishedEaseIn;
        private bool _running;

        private bool _AI = false;
        private Color4 _opposingColour;
        private Color4 _collisionColour;
        private Color4 _outlineColour;

        Color4 _collisionOutlineColour;

        private int _multiplier;

        public int Hits
        {
            get { return _player.Hits; }
        }

        public int Multiplier
        {
            get { return _multiplier; }
        }

        public int CurrentPolygon
        {
            get {return _polygonIndex;}
        }

        public int PolygonCount
        {
            get {return _polygons.Length;}
        }

        public bool FinishedEaseIn
        {
            get
            {
                return _finishedEaseIn;
            }
        }

        private bool Collided
        {
            get { return _collidedPolygonIndex == _polygonIndex; }
        }

        //public Stage()
        //{
        //    _player = new Player();
        //    _centerPolygon = new PolarPolygon(new List<bool>{true, true, true, true, true, true}, new PolarVector(0.5, 0), 50, 80, 0);
        //    //_centerPolygon = new PolarPolygon(6, 6, 0, 1, 0, 80);
        //}

        public Stage(Player player, PolarPolygon centerPolygon)
        {
            _player = player;
            _centerPolygon = centerPolygon;
        }

        public void LoadAsync(string audioPath, string sonicPath, string pluginPath, float correction, IProgress<string> progress)
        {
            progress.Report("Loading audio");
            LoadAudioStream(audioPath);
            progress.Report("Extracting audio features");
            LoadAudioFeatures(audioPath, sonicPath, pluginPath, correction, progress);
            progress.Report("Load complete");


            _direction = 1;
        }

        private void LoadAudioStream(string audioPath)
        {
            byte[] hashBytes = new byte[HashCount];
            _waveOut = new WaveOut();

            if (Path.GetExtension(audioPath).Equals(".flac", StringComparison.CurrentCultureIgnoreCase))
            {
                var str = new MemoryStream();
                var output = new WavWriter(str);
                var fr = new FlacReader(audioPath, output);
                fr.Process();
                str.Position = 0;
                str.Read(hashBytes, 0, HashCount);
                str.Position = 0;

                var fmt = new WaveFormat(fr.inputSampleRate, fr.inputBitDepth, fr.inputChannels);
                var s = new RawSourceWaveStream(str, fmt);
                _waveProvider = s;
                _waveOut.Init(_waveProvider);
            }
            else
            {

                var audioReader = new AudioFileReader(audioPath);
                audioReader.Read(hashBytes, 0, HashCount);
                audioReader.Position = 0;
                _waveProvider = audioReader;
                _waveOut.Init(_waveProvider);
            }

            _hashCode = CRC16.Instance().ComputeChecksum(hashBytes);
            _random = new Random(_hashCode);
        }

        private void LoadAudioFeatures(string audioPath, string sonicPath, string pluginPath, float correction, IProgress<string> progress)
        {
            _audioFeatures = new AudioFeatures(sonicPath, pluginPath, "../../Processed Songs/", correction + (float)_easeInTime, progress);
            _audioFeatures.Extract(audioPath);

            _polygons = new PolarPolygon[_audioFeatures.Onsets.Count];

            int maxSides = 6;

            var angles = new double[maxSides];
            for (int i = 0; i < maxSides; i++)
            {
                angles[i] = MathHelper.DegreesToRadians((i + 1) * (360/maxSides));
            }

            int prevStart = _random.Next(maxSides - 1);
            int prevSkip = 1;
            double prevTime = 0;
            int c = 0;

            int index = 0;

            var sorted = _audioFeatures.Onsets.OrderBy(f => f);
            foreach (var b in sorted) 
            {
                int start = 0;
                int skip = (int)(3*Math.Pow(_random.NextDouble(), 4)) + 1;
                if (b - prevTime < 0.2)
                {
                    c++;
                    start = prevStart;
                    skip = prevSkip;
                }
                else if (b - prevTime < 0.4)
                {
                    start = (prevStart + maxSides) + _random.Next(0, 2) - 1;
                    skip = prevSkip;
                }
                else
                {
                    start = _random.Next(maxSides - 1);
                    c = 0;
                }

                bool[] sides = new bool[6];
                for (int i = 0; i < 6; i++)
                {
                    if (skip == 1 && i == start%6) sides[i] = false;
                    else if ((i+start)%skip == 0) sides[i] = true;
                    else sides[i] = false;
                }
                _polygons[index] = new PolarPolygon(sides.ToList(), new PolarVector(0, 600), 50, 125, b);
                //_polygons[index] = new PolarPolygon(maxSides, 5, b, 600 + b*0, angles[start % 6] + _centerPolygon.Azimuth, 125);

                prevTime = b;
                prevStart = start;
                prevSkip = skip;

                index++;
            }

            _segments = _audioFeatures.Segments.OrderBy(x => x.StartTime).ToArray();
            var maxID = _audioFeatures.Segments.Max(x => x.ID);
            _segmentColours = new Color4[maxID];


            var s = 30;
            var l = 40;
            double maxStep = (double)360/(maxID+1);
            double minStep = 0.5*maxStep;
            double startAngle = _random.NextDouble()*360;
            double prevAngle = startAngle - maxStep;
            for (int i = 0; i < maxID; i++)
            {
                var step = _random.NextDouble()*(maxStep - minStep) + minStep;
                double angle = prevAngle;
                do
                {
                    angle = MathUtilities.Normalise(step + angle, 0, 360);
                } while (angle > 275 && angle < 310);
                var col = new Hsl{H = angle, L=l, S=s};
                var rgb = col.ToRgb();

                prevAngle = angle;

                _segmentColours[i] = new Color4((byte)rgb.R, (byte)rgb.G, (byte)rgb.B, 255);
            }
            _colourIndex = _segments[_segmentIndex].ID - 1;
            //UpdateColours();
        }

        public void Update(double time)
        {
            var rotate = time*0.5*_direction;
            if (!_running)
            {
                _elapsedWarmupTime += time;
                if (_elapsedWarmupTime > _warmupTime)
                {
                    _running = true;
                    time = _elapsedWarmupTime - _warmupTime;
                }

            }
            if (_running)
            {
                _totalTime += time;
            }
            if (!_finishedEaseIn && _totalTime > _easeInTime)
            {
                _waveOut.Play();
                _finishedEaseIn = true;
            }

            _polygonIndex += _polygonsToRemoveCount;
            _polygonsToRemoveCount = 0;

            for (int i = _polygonIndex; i < _polygons.Length; i++)
            {
                var poly = _polygons[i];
                poly.Direction = _direction;
                poly.Position.Azimuth = _centerPolygon.Position.Azimuth + rotate;
                poly.Update(time, _running);
                if (_running)
                {
                    if (poly.Destroy)
                        _polygonsToRemoveCount++;
                    else if ((poly.Position.Radius - poly.ImpactDistance)/(poly.Velocity.Radius) < (poly.PulseWidthMax/poly.PulseMultiplier))
                        _centerPolygon.Pulsing = true;
                }
                if (poly.Colour != _collisionColour && poly.Colour != _opposingColour)
                    poly.SetColour(_opposingColour, _outlineColour);
                //poly.Colour = poly.Colour == _collisionColour ? _collisionColour : _opposingColour;
            }

            if (_polygonsToRemoveCount > 0)
            {
                var d = _random.NextDouble();
                _direction = d > 0.95 ? -_direction : _direction;
                _multiplier += _polygonsToRemoveCount;
            }
            GetPlayerOverlap();

            if (InputSystem.NewKeys.Contains(Key.F2)) _AI = !_AI;


            if (_AI && _polygonIndex < _polygons.Length)
            {
                var t = _polygons[_polygonIndex].OpeningAngle + rotate*_direction + _centerPolygon.Position.Azimuth;
                t += MathHelper.DegreesToRadians(30);
                _player.DoAI(t);
            }

            _player.Direction = _direction;
            _centerPolygon.Direction = _direction;
            if (_centerPolygon.Colour == _collisionColour && !Collided)
                _centerPolygon.SetColour(_opposingColour, _outlineColour);
            //_centerPolygon.Colour = Collided ? _collisionColour : _opposingColour;
            _player.Position.Azimuth += rotate;
            _player.Update(time, _AI);
            _centerPolygon.Update(time, false);
            _centerPolygon.Position.Azimuth += rotate;

            var seg = _segments[_segmentIndex];
            if (seg.EndTime > 0 && seg.EndTime < _totalTime)
            {
                _segmentIndex++;
                _colourIndex = _segments[_segmentIndex].ID - 1;

                UpdateColours();
            }

        }

        public void UpdateColours()
        {
            var c = Utilities.Color4ToColorSpace(_segmentColours[_colourIndex]).ToRgb();

            var opp = c.To<Hsl>();
            opp.S = 60;
            opp.L = 60;
            _collisionColour = Utilities.ColorSpaceToColor4(opp);
            _collisionOutlineColour = Utilities.ColorSpaceToColor4(GetOutlineColour(opp));

            var hsl = c.To<Hsl>();
            hsl.H = MathUtilities.Normalise(hsl.H + 180, 0, 360);
            hsl.S = 50;
            c = hsl.ToRgb();
            _opposingColour = Utilities.ColorSpaceToColor4(c);

            _outlineColour = Utilities.ColorSpaceToColor4(GetOutlineColour(hsl));

            //_centerPolygon.Colour = _opposingColour;
            _centerPolygon.SetColour(_opposingColour, _outlineColour);
            GL.ClearColor(_segmentColours[_colourIndex]);
        }

        private Hsl GetOutlineColour(Hsl col)
        {
            col.L += 10;
            col.S += 20;
            return col;
        }

        private void GetPlayerOverlap()
        {
            if (_polygonIndex == _collidedPolygonIndex || _polygonIndex >= _polygons.Length)
            {
                Overlap = 0;
                return;
            }
            var c = new Clipper();
            c.AddPaths(_polygons[_polygonIndex].GetPolygonBounds(), PolyType.ptSubject, true);
            c.AddPath(_player.GetBounds(), PolyType.ptClip, true);

            var soln = new List<List<IntPoint>>();
            c.Execute(ClipType.ctIntersection, soln);

            Overlap = soln.Count > 0 ? (int)((Clipper.Area(soln[0]) / Clipper.Area(_player.GetBounds()))*100) : 0;
            if (Overlap > 80)
            {
                _multiplier = -1;
                _player.Hits++;
                _collidedPolygonIndex = _polygonIndex;
                _polygons[_collidedPolygonIndex].SetColour(_collisionColour, _collisionOutlineColour);
                _centerPolygon.SetColour(_collisionColour, _collisionOutlineColour);
                //_polygons[_collidedPolygonIndex].Colour = _collisionColour;
            }
        }

        public void Draw(double time)
        {
            //GL.Color4(_opposingColour);
            for (int i = _polygonIndex; i < _polygons.Length; i++)
            {
                _polygons[i].Draw(time);
            }
            _centerPolygon.Draw(time);
            _player.Draw(time);

        }
    }
}
