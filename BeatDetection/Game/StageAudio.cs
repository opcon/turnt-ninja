using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenTK;
using Substructio.Core;
using Wav2Flac;

namespace BeatDetection.Game
{
    class StageAudio
    {
        public int AudioHashCode { get; private set; }
        private const int HashCount = 10000;
        private WaveOut _waveOut;
        private WaveStream _waveProvider;

        /// <summary>
        /// Max volume is 1.0f
        /// </summary>
        public float Volume
        {
            get { return _waveOut.Volume; }
            set { _waveOut.Volume = MathHelper.Clamp(value, 0, 1); }
        }

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
                _waveProvider = new RawSourceWaveStream(str, fmt);
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
            _waveProvider.Position = 0;
        }

        public void Resume()
        {
            _waveOut.Resume();
        }

        public void Seek(float percent)
        {
            int newPos = (int) (percent*_waveProvider.Length);
            newPos = newPos - newPos%_waveProvider.BlockAlign;
            _waveProvider.Position = newPos;
        }

        /// <summary>
        /// Fades out the audio.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="minVolume"></param>
        /// <param name="dVolume"></param>
        /// <param name="PauseOrStop">0 for nothing, 1 for pause, 2 for stop</param>
        public void FadeOut(float time, float minVolume, float dVolume, int PauseOrStop)
        {
            Task.Factory.StartNew(() =>
            {
                int dt = (int) (time/((Volume - minVolume)/dVolume));
                while (Volume > minVolume)
                {
                    Volume -= dVolume;
                    Thread.Sleep(dt);
                }
                if (PauseOrStop == 1) Pause();
                else if (PauseOrStop == 2) Stop();
            });
        }

        public void FadeIn(float time, float maxVolume, float dVolume)
        {
            
        }
    }
}
