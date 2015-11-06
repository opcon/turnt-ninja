using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Audio;

namespace BeatDetection.FileSystem
{
    class SoundCloudFileSystem : IFileSystem
    {
        public ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string FriendlyName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool EntrySelected(ref int entryIndex)
        {
            throw new NotImplementedException();
        }

        public int Initialise(FileBrowserEntry separator)
        {
            throw new NotImplementedException();
        }

        public void LoadSongAudio(Song song)
        {
            throw new NotImplementedException();
        }

        public Song LoadSongInformation(int entryIndex)
        {
            throw new NotImplementedException();
        }
    }
}
