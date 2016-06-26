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
        List<IFileSystem> FileSystemCollection { get; set; }
        ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get; }
        string FriendlyName { get; }

        int Initialise(FileBrowserEntry separator);
        void Focused();

        bool EntrySelected(ref int entryIndex);
        bool SongExists(SongBase song);

        Song LoadSongInformation(int entryIndex);
        void LoadSongAudio(Song song);
    }
}
