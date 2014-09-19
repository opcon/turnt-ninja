using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using BeatDetection.Core;
using ClipperLib;
using NAudio.Wave;
using OpenTK;
using OpenTK.Input;
using Substructio.Core;
using Wav2Flac;
using OpenTK.Graphics;

namespace BeatDetection.Game
{
    internal class Stage
    {
        private double _totalTime;
        private PolarPolygon[] _polygons;
        private QMVampWrapper _qmVampWrapper;
        private int _polygonIndex;
        private int _polygonsToRemoveCount;

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
        private bool _finishedEaseIn = false;
        private bool _running = false;

        public int Hits
        {
            get { return _player.Hits; }
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

        private bool _collided
        {
            get { return _collidedPolygonIndex == _polygonIndex; }
        }

        public Stage()
        {
            _player = new Player();
            _centerPolygon = new PolarPolygon(6, 6, 0, 1, 0, 80);
        }

        public Stage(Player player, PolarPolygon centerPolygon)
        {
            _player = player;
            _centerPolygon = centerPolygon;
        }

        public void Load(string audioPath, string sonicPath, string pluginPath, float correction)
        {
            Thread.Sleep(1000);
            LoadAudioStream(audioPath);
            LoadAudioFeatures(audioPath, sonicPath, pluginPath, correction);

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

        private void LoadAudioFeatures(string audioPath, string sonicPath, string pluginPath, float correction)
        {
            _qmVampWrapper = new QMVampWrapper(audioPath, sonicPath, pluginPath, correction + (float)_easeInTime);
            _qmVampWrapper.DetectBeats();

            _polygons = new PolarPolygon[_qmVampWrapper.Beats.Count];

            int maxSides = 6;

            var angles = new double[maxSides];
            for (int i = 0; i < maxSides; i++)
            {
                angles[i] = MathHelper.DegreesToRadians((i + 1) * (360/maxSides));
            }

            int prevStart = _random.Next(maxSides - 1);
            double prevTime = 0;
            int c = 0;

            int index = 0;

            var sorted = _qmVampWrapper.Beats.OrderBy(f => f);
            foreach (var b in sorted) 
            {
                var col = Color.White;
                int start = 0;
                if (b - prevTime < 0.2)
                {
                    c++;
                    start = prevStart;
                    col = Color.Red;
                }
                else if (b - prevTime < 0.4)
                {
                    start = (prevStart + maxSides) + _random.Next(0, 2) - 1;
                }
                else
                {
                    start = _random.Next(maxSides - 1);
                    c = 0;
                }

                _polygons[index] = new PolarPolygon(maxSides, 5, b, 600, angles[start % 6] + _centerPolygon.Azimuth, 125);

                //for (int i = 0; i < 5; i++)
                //{
                //    //hexagons.Add(new PolarPolygon(b, 300) { theta = angles[start] + angles[((i+start)%6)] });
                //    hexagonSides.Add(new PolarPolygonSide(b, 400, angles[start % 6] + i * angles[0], 125) { Colour = col });
                //}
                prevTime = b;
                prevStart = start;
                //hexagons.Add(new PolarPolygon(b, 300) { theta = angles[0] });
                //hexagons.Add(new PolarPolygon(b, 300) { theta = angles[1] });
                //hexagons.Add(new PolarPolygon(b, 300) { theta = angles[2] });
                //hexagons.Add(new PolarPolygon(b, 300) { theta = angles[3] });

                index++;
            }

        }

        public void Update(double time)
        {
            double targetAzimuth;
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

            if (true)
            {
                //_totalTime += time;


                _polygonIndex += _polygonsToRemoveCount;
                _polygonsToRemoveCount = 0;

                for (int i = _polygonIndex; i < _polygons.Length; i++)
                {
                    var poly = _polygons[i];
                    poly.Direction = _direction;
                    poly.Update(time, _running);
                    if (_running)
                    {
                        if (poly.Destroy)
                            _polygonsToRemoveCount++;
                        else if ((poly.Radius - poly.ImpactDistance) / (poly.Speed) < (poly.PulseWidthMax / poly.PulseMultiplier))
                            _centerPolygon.Pulsing = true;
                    }
                }

                if (_polygonsToRemoveCount > 0)
                {
                    var d = _random.NextDouble();
                    _direction = d > 0.95 ? -_direction : _direction;
                    // _centerPolygon.Pulse(time);
                }

                //_centerPolygon.Update(time, false);
                //if (InputSystem.CurrentKeys.Contains(Key.Left))
                //    _centerPolygon.Rotate(time);
                //else if (InputSystem.CurrentKeys.Contains(Key.Right))
                //    _centerPolygon.Rotate(-time * 1.5);

                GetPlayerOverlap();

            }

            var t = _polygons[_polygonIndex].Azimuth;
            t -= MathHelper.DegreesToRadians(30);
            //_player.DoAI(t);
            _player.Direction = _direction;
            _centerPolygon.Direction = _direction;
            _centerPolygon.Colour = _collided ? Color4.Red : Color4.White;
            _player.Update(time);
            _centerPolygon.Update(time, false);
        }

        private void GetPlayerOverlap()
        {
            if (_polygonIndex == _collidedPolygonIndex || _polygonIndex >= _polygons.Length)
            {
                Overlap = 0;
                return;
            }
            Clipper c = new Clipper();
            c.AddPaths(_polygons[_polygonIndex].GetPolygonBounds(), PolyType.ptSubject, true);
            c.AddPath(_player.GetBounds(), PolyType.ptClip, true);

            List<List<IntPoint>> soln = new List<List<IntPoint>>();
            c.Execute(ClipType.ctIntersection, soln);

            Overlap = soln.Count > 0 ? (int)((Clipper.Area(soln[0]) / Clipper.Area(_player.GetBounds()))*100) : 0;
            if (Overlap > 80)
            {
                _player.Hits++;
                _collidedPolygonIndex = _polygonIndex;
                _polygons[_collidedPolygonIndex].Colour = Color4.Red;
            }
        }

        public void Draw(double time)
        {
            for (int i = _polygonIndex; i < _polygons.Length; i++)
            {
                _polygons[i].Draw(time);
            }

            _centerPolygon.Draw(time);
            _player.Draw(time);

        }
    }
}
