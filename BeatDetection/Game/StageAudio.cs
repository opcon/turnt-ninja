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
    class StageAudio : IDisposable
    {
        public int AudioHashCode { get; private set; }
        private const int HashCount = 10000;
        private WaveOut _waveOut;
        private WaveStream _waveProvider;
        private float _maxVolume;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Volume is clamped between 0 and MaxVolume
        /// </summary>
        public float Volume
        {
            get { return _waveOut.Volume; }
            set { _waveOut.Volume = MathHelper.Clamp(value, 0, 1); }
        }

        /// <summary>
        /// Max volume is between 0 and 1
        /// </summary>
        public float MaxVolume
        {
            get { return _maxVolume; }
            set { _maxVolume = MathHelper.Clamp(value, 0, 1); }
        }

        public bool IsStopped
        {
            get { return _waveOut.PlaybackState == PlaybackState.Stopped; }
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
        /// <param name="pauseOrStop">0 for nothing, 1 for pause, 2 for stop</param>
        public void FadeOut(float time, float minVolume, float dVolume, int pauseOrStop)
        {
            var ct = _tokenSource.Token;
            Task.Run(async () =>
            {
                int dt = (int)(time / ((Volume - minVolume) / dVolume));
                while (Volume > minVolume && !ct.IsCancellationRequested)
                {
                    Volume -= dVolume;
                    await Task.Delay(dt, ct);
                }
                switch (pauseOrStop)
                {
                    case 1:
                        Pause();
                        break;
                    case 2:
                        Stop();
                        break;
                }
            }, ct);
        }

        /// <summary>
        /// Fades in the audio
        /// </summary>
        /// <param name="time">Time over which to fade the audio in</param>
        /// <param name="maxVolume">The maximum volume to reach</param>
        /// <param name="dVolume">The volume step size to use</param>
        /// <param name="pauseOrStop">0 for nothing, 1 to pause after the fade in, 2 to stop after the fade in</param>
        public void FadeIn(float time, float maxVolume, float dVolume, int pauseOrStop)
        {
            if (maxVolume > MaxVolume) throw new ArgumentOutOfRangeException("The maximum fade in volume " + maxVolume + " was greater than the maximum volume for this audio " + MaxVolume);
            var ct = _tokenSource.Token;
            Task.Run(async () =>
            {
                int dt = (int)(time / ((maxVolume - Volume) / dVolume));
                while (Volume < maxVolume && !ct.IsCancellationRequested)
                {
                    Volume += dVolume;
                    await Task.Delay(dt, ct);
                }
                switch (pauseOrStop)
                {
                    case 1:
                        Pause();
                        break;
                    case 2:
                        Stop();
                        break;
                }
            }, ct);
        }

        public void CancelAudioFades()
        {
            _tokenSource.Cancel();
            _tokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _waveOut.Dispose();
            //may error here if _waveOut disposes _waveProvider?
            _waveProvider.Dispose();
        }
    }
}
