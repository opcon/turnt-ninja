using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using BeatDetection.Core;
using NAudio.Wave;
using OpenTK;
using OpenTK.Input;
using Substructio.Core;
using Wav2Flac;

namespace BeatDetection.Game
{
    class Stage
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

        public Stage()
        {

        }

        public void Load(string audioPath, string sonicPath, string pluginPath, float correction)
        {
            LoadAudioStream(audioPath);
            LoadAudioFeatures(audioPath, sonicPath, pluginPath, correction);

            _player = new Player();
            _centerPolygon = new PolarPolygon(6, 6, 0, 1, 0, 80);
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
            _qmVampWrapper = new QMVampWrapper(audioPath, sonicPath, pluginPath, correction);
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

                _polygons[index] = new PolarPolygon(maxSides, 5, b, 400, angles[start % 6], 125);

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

            if (_waveOut.PlaybackState != PlaybackState.Playing)
            {
                _waveOut.Play();
                time = 0;
            }
            if (_waveOut.PlaybackState == PlaybackState.Playing)
            {
                _totalTime += time;
                _player.Direction = _direction;
                _centerPolygon.Direction = _direction;
                _player.Update(time);

                _polygonIndex += _polygonsToRemoveCount;
                _polygonsToRemoveCount = 0;

                for (int i = _polygonIndex; i < _polygons.Length; i++)
                {
                    var poly = _polygons[i];
                    poly.Direction = _direction;
                    poly.Update(time);
                    if (poly.Destroy)
                        _polygonsToRemoveCount++;
                    else if ((poly.Radius - poly.ImpactDistance)/(poly.Speed) < (poly.PulseWidthMax/poly.PulseMultiplier))
                        _centerPolygon.Pulsing = true;
                }

                if (_polygonsToRemoveCount > 0)
                {
                    var d = _random.NextDouble();
                    _direction = d > 0.95 ? -_direction : _direction;
                   // _centerPolygon.Pulse(time);
                }

                _centerPolygon.Update(time, false);
                //if (InputSystem.CurrentKeys.Contains(Key.Left))
                //    _centerPolygon.Rotate(time);
                //else if (InputSystem.CurrentKeys.Contains(Key.Right))
                //    _centerPolygon.Rotate(-time * 1.5);

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
