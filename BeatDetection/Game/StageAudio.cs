using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;
using Substructio.Core;
using Wav2Flac;

namespace BeatDetection.Game
{
    class StageAudio
    {
        public int AudioHashCode { get; private set; }
        private const int HashCount = 10000;
        private WaveOut _waveOut;
        private IWaveProvider _waveProvider;

        public void Load(string audioPath)
        {
            //assert that the audio path given is valid.
            Debug.Assert(!string.IsNullOrWhiteSpace(audioPath));
            var hashBytes = new byte[HashCount];
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

            AudioHashCode = CRC16.Instance().ComputeChecksum(hashBytes);
        }

        public void Play()
        {
            _waveOut.Play();
        }

        public void Pause()
        {
            _waveOut.Pause();
        }

        public void Stop()
        {
            _waveOut.Stop();
        }

        public void Resume()
        {
            _waveOut.Resume();
        }
    }
}
