using BeatDetection.FileSystem;
using CSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.Audio
{
    public class Song : IDisposable
    {
        public IWaveSource SongAudio { get; set; }
        public bool SongAudioLoaded { get; set; } = false;
        public IFileSystem FileSystem { get; set; }

        public string InternalName { get; set; }

        public string Identifier { get; set; }
        public string Artist { get; set; }
        public string TrackName { get; set; }

        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SongAudio.Dispose();
                }
                SongAudio = null;

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
    }
}
