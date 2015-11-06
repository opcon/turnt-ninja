using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using BeatDetection.Audio;


namespace BeatDetection.FileSystem
{
    public interface IFileSystem
    {
        ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get; }
        string FriendlyName { get; }

        int Initialise(FileBrowserEntry separator);

        bool EntrySelected(ref int entryIndex);

        Song LoadSongInformation(int entryIndex);
        void LoadSongAudio(Song song);
    }
}
