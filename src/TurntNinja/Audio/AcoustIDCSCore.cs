using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcoustID.Chromaprint;
using CSCore;

namespace TurntNinja.Audio
{
    class AcoustIDCSCore : AcoustID.Audio.IDecoder, IDisposable
    {
        private readonly CSCore.IWaveSource _waveSource;
        private const int BUFFER_SIZE = 2 * 192000;

        private byte[] _buffer;
        private short[] _data;

        public AcoustIDCSCore(CSCore.IWaveSource waveSource)
        {
            _waveSource = waveSource;

            if (_waveSource.WaveFormat.BitsPerSample != 16)
                throw new ArgumentOutOfRangeException(nameof(waveSource), "Expected 16 bit audio");
        }

        public int Channels { get { return _waveSource.WaveFormat.Channels; } }

        public int SampleRate { get { return _waveSource.WaveFormat.SampleRate; } }

        public bool Decode(IAudioConsumer consumer, int maxLength)
        {
            if (_waveSource == null) return false;
            if (_buffer == null) _buffer = new byte[BUFFER_SIZE * 2];
            if (_data == null) _data = new short[BUFFER_SIZE];

            // maxLength is in seconds of audio
            // Calculate maximum bytes to read
            var maxBytes = maxLength * Channels * SampleRate;

            // Calculate actual bytes we can fit in buffer
            var bytesToRead = Math.Min(maxBytes, _buffer.Length);

            int read = 0;

            while ((read = _waveSource.Read(_buffer, 0, bytesToRead)) > 0)
            {
                Buffer.BlockCopy(_buffer, 0, _data, 0, read);

                consumer.Consume(_data, read / 2);

                maxBytes -= read / 2;
                if (maxBytes <= 0)
                    break;
                bytesToRead = Math.Min(maxBytes, _buffer.Length);
            }

            return true;
        }

        public void Dispose()
        {
            _waveSource.Dispose();
            _buffer = null;
            _data = null;
        }
    }
}
