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
        List<Song> _recentSongList;
        List<SongBase> _recentSongBaseList;

        public ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get { return _recentSongs.AsReadOnly(); } }

        public string FriendlyName { get { return "Recent Songs"; } }

        public List<IFileSystem> FileSystemCollection { get; set; }

        public RecentFileSystem(List<SongBase> RecentSongList)
        {
            _recentSongBaseList = RecentSongList;
            _recentSongList = new List<Song>();
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
            BuildRecentSongList();
            return 0;
        }

        private void BuildRecentSongList()
        {
            //Build song list
            var _toRemove = new List<SongBase>();
            _recentSongList.Clear();
            foreach (var s in _recentSongBaseList)
            {
                var fs = FileSystemCollection.FirstOrDefault(f => f.FriendlyName.Equals(s.FileSystemFriendlyName, StringComparison.OrdinalIgnoreCase));
                if (fs != null && fs.SongExists(s))
                    _recentSongList.Add(new Song { SongBase = s, FileSystem = fs });
                else
                    _toRemove.Add(s);

            }
            _toRemove.ForEach(s => _recentSongBaseList.Remove(s));
            _recentSongs.Clear();

            _recentSongs = _recentSongList.ConvertAll(s => new FileBrowserEntry { EntryType = FileBrowserEntryType.Song, Name = s.SongBase.Identifier, Path = s.SongBase.InternalName });
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
            return _recentSongList[entryIndex];
        }

        public bool SongExists(SongBase song)
        {
            throw new NotImplementedException();
        }

        public void Focused()
        {
            BuildRecentSongList();
        }
    }
}
