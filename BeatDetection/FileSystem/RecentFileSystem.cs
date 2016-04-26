using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Audio;
using System.IO;

namespace BeatDetection.FileSystem
{
    class RecentFileSystem : IFileSystem
    {
        List<FileBrowserEntry> _recentSongs;
        List<SongBase> _recentSongList;

        public ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get { return _recentSongs.AsReadOnly(); } }

        public string FriendlyName { get { return "Recent Songs"; } }

        public RecentFileSystem(List<SongBase> RecentSongList)
        {
            _recentSongList = RecentSongList;
            _recentSongs = new List<FileBrowserEntry>();
        }

        public bool EntrySelected(ref int entryIndex)
        {
            //return true if we've found a song
            if (_recentSongs[entryIndex].EntryType.HasFlag(FileBrowserEntryType.Song)) return true;

            //something has gone wrong
            return false;
        }

        public int Initialise(FileBrowserEntry separator)
        {
            //Build song list
            _recentSongs.Clear();

            _recentSongs = _recentSongList.ConvertAll(s => new FileBrowserEntry { EntryType = FileBrowserEntryType.Song, Name = s.Identifier, Path = s.InternalName });

            return 0;
        }

        public void LoadSongAudio(Song song)
        {
            // Sanity checks
            if (!File.Exists(song.SongBase.InternalName)) throw new Exception("File not found: " + song.SongBase.InternalName);

            song.SongAudio = CSCore.Codecs.CodecFactory.Instance.GetCodec(song.SongBase.InternalName);
            song.SongAudioLoaded = true;
        }

        public Song LoadSongInformation(int entryIndex)
        {
            return new Song { SongBase = _recentSongList[entryIndex] };
        }
    }
}
