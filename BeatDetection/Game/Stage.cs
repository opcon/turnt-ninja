using System;
using System.Collections.Generic;
using System.IO;
using BeatDetection.Core;
using NAudio.Wave;
using Substructio.Core;
using Wav2Flac;

namespace BeatDetection.Game
{
    class Stage
    {
        private double _totalTime;
        private PolarPolygon[] _polygons;
        private QMVampWrapper _qmVampWrapper;

        private WaveOut _waveOut;
        private IWaveProvider _waveProvider;

        private Random _random;
        private int _hashCode;
        private const int HashCount = 10000;

        public Stage()
        {

        }

        public void Load(string audioPath, string sonicPath, string pluginPath, float correction)
        {
            LoadAudioStream(audioPath);
            LoadAudioFeatures(audioPath, sonicPath, pluginPath, correction);
        }

        private void LoadAudioStream(string audioPath)
        {
            byte[] hashBytes = new byte[HashCount];

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
        }

        public void Update(double time)
        {
            _totalTime += time;
        }

        public void Draw(double time)
        {
            
        }
    }
}
