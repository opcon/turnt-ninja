using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Streams;
using OpenTK;
using Substructio.Core;


namespace BeatDetection.Game
{
    class StageAudio : IDisposable
    {
        public int AudioHashCode { get; private set; }
        private const int HashCount = 10000;
        //private WaveOut _waveOut;
        //private WaveStream _waveProvider;
        private float _maxVolume;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private IAudio _audio;

        /// <summary>
        /// Volume is clamped between 0 and MaxVolume
        /// </summary>
        public float Volume
        {
            get { return _audio.Volume; }
            set { _audio.Volume = MathHelper.Clamp(value, 0, 1); }
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
            get { return _audio.PlaybackState == PlaybackState.Stopped; }
        }

        public IWaveSource Source
        {
            get { return _audio.GetSource(); }
        }

        public void Load(IWaveSource source)
        {
            _audio = new CSCoreAudio();
            _audio.Init(source);
            AudioHashCode = CRC16.Instance().ComputeChecksum(_audio.GetHashBytes(HashCount));
        }

        public void Play()
        {
            _audio.Play();
        }

        public void Pause()
        {
            _audio.Pause();
        }

        public void Stop()
        {
            _audio.Stop();
            _audio.Seek(0);
        }

        public void Resume()
        {
            _audio.Resume();
        }

        public void Seek(float percent)
        {
            _audio.Seek(percent);
        }

        public string CreateTempWavFile(string audioFilePath, string tempFolderName = "")
        {
            var newFile = Path.Combine(Path.GetTempPath() + tempFolderName, Path.GetFileNameWithoutExtension(audioFilePath)) + ".wav";
            
            //create directory if it doesn't exist
            var dir = Path.GetDirectoryName(newFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            _audio.ConvertToWav(newFile);
            return newFile;
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
            if (_audio != null) _audio.Dispose();
        }
    }


    internal interface IAudio : IDisposable
    {
        PlaybackState PlaybackState { get; }
        float Volume { get; set; }
        //void Init(string audioFilePath);
        void Init(IWaveSource source);
        byte[] GetHashBytes(int hashByteCount);
        void Play();
        void Pause();
        void Resume();
        void Stop();
        void Seek(float percent);
        void ConvertToWav(string wavFilePath);
        IWaveSource GetSource();
    }

    enum PlaybackState
    {
        Paused,
        Playing,
        Stopped
    }

    class CSCoreAudio : IAudio
    {
        private ISoundOut _soundOut;
        private IWaveSource _soundSource;

        public void Dispose()
        {
            if (_soundOut != null)
            {
                _soundOut.Stop();
                _soundOut.Dispose();
                _soundOut = null;
            }
            if (_soundSource != null)
            {
                _soundSource.Dispose();
                _soundSource = null;
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                switch (_soundOut.PlaybackState)
                {
                    case CSCore.SoundOut.PlaybackState.Paused:
                        return PlaybackState.Paused;
                    case CSCore.SoundOut.PlaybackState.Playing:
                        return PlaybackState.Playing;
                    case CSCore.SoundOut.PlaybackState.Stopped:
                        return PlaybackState.Stopped;
                }
                return PlaybackState.Stopped;
            }
        }

        public float Volume { get { return _soundOut.Volume; } set { _soundOut.Volume = value; } }

        public IWaveSource GetSource()
        {
            return _soundSource;
        }

        //public void Init(string audioFilePath)
        //{
        //    Init(CodecFactory.Instance.GetCodec(audioFilePath));
        //}

        public void Init(IWaveSource source)
        {
            _soundSource = source;
            _soundOut = new WaveOut();
            _soundOut.Initialize(_soundSource);
            _soundOut.Stopped += (sender, args) => { if (args.HasError) throw new Exception("exception thrown on stoping audio", args.Exception); };
        }

        public byte[] GetHashBytes(int hashByteCount)
        {
            var ret = new byte[hashByteCount];
            _soundSource.Read(ret, 0, hashByteCount);
            _soundSource.Position = 0;
            return ret;
        }


        public void Play()
        {
            _soundOut.Play();
        }

        public void Resume()
        {
            _soundOut.Resume();
        }

        public void Stop()
        {
            _soundOut.Stop();
            _soundSource.SetPosition(TimeSpan.Zero);
        }

        public void Seek(float percent)
        {
            _soundSource.SetPosition(TimeSpan.FromMilliseconds(percent * _soundSource.GetLength().TotalMilliseconds));
        }

        public void ConvertToWav(string wavFilePath)
        {
            _soundSource.WriteToFile(wavFilePath);
        }

        public void Pause()
        {
            _soundOut.Pause();
        }
    }
}
