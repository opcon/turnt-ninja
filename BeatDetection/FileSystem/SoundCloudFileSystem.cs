using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Audio;
using SoundCloud.API.Client;
using SoundCloud.API.Client.Objects;
using System.Net;

namespace BeatDetection.FileSystem
{
    class SoundCloudFileSystem : IFileSystem
    {
        List<FileBrowserEntry> _soundcloudSongs;
        List<SCTrack> _scTracks;
        List<SCExploreCategory> _scCategories;
        FileBrowserEntry _entrySeparator;
        string clientID = "74e6e3acb28021e21eb32ef4bc10e995";
        string clientSecret = "";
        ISoundCloudConnector _sconnector;
        IUnauthorizedSoundCloudClient _scclient;

        const int FILE_SYSTEM_ENTRY_OFFSET = 2;

        public List<IFileSystem> FileSystemCollection { get; set; }
        public ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get { return _soundcloudSongs.AsReadOnly(); } }

        public string FriendlyName { get { return "SoundCloud"; } }

        public SoundCloudFileSystem()
        {
            _soundcloudSongs = new List<FileBrowserEntry>();
            _scTracks = new List<SCTrack>();
            _scCategories = new List<SCExploreCategory>();
        }

        public int Initialise(FileBrowserEntry separator)
        {
            _entrySeparator = separator;

            //setup soundcloud connection
            _sconnector = new SoundCloudConnector();

            _scclient = _sconnector.UnauthorizedConnect(clientID, clientSecret);

            //Get tracks
            _scCategories = _scclient.Explore.GetExploreCategories().ToList();

            ShowCategories();

            return 0;
        }

        public void ShowCategories()
        {
            _soundcloudSongs = _scCategories.ConvertAll(c => new FileBrowserEntry { EntryType = FileBrowserEntryType.Directory, Name = System.Uri.UnescapeDataString(c.Name).Replace('+',' '), Path = c.Name});
        }

        public void ShowSongs()
        {
            _soundcloudSongs = _scTracks.ConvertAll(s => new FileBrowserEntry { EntryType = FileBrowserEntryType.Song, Name = s.Title, Path = s.Uri });
        }

        public bool EntrySelected(ref int entryIndex)
        {
            //return true if we've found a song
            if (_soundcloudSongs[entryIndex].EntryType.HasFlag(FileBrowserEntryType.Song))
                return true;

            //If category list selected
            if (_soundcloudSongs[entryIndex].EntryType.HasFlag(FileBrowserEntryType.Special))
            {
                ShowCategories();
                return false;
            }

            //If category selected
            _scTracks = _scclient.Chart.GetTracks(_scCategories[entryIndex]).ToList();
            ShowSongs();

            //Add the category list entry
            _soundcloudSongs.Insert(0, new FileBrowserEntry
            {
                EntryType = FileBrowserEntryType.Special,
                Name = "Category List",
                Path = ""
            });

            _soundcloudSongs.Insert(1, _entrySeparator);

            entryIndex = 0;
            return false;
        }

        public void LoadSongAudio(Song song)
        {
            var sctrack = _scclient.Resolve.GetTrack(song.SongBase.InternalName);
            var url = sctrack.StreamUrl + "?client_id=" + clientID;
            var wr = WebRequest.Create(url);
            var response = wr.GetResponse();
            song.SongAudio = CSCore.Codecs.CodecFactory.Instance.GetCodec(response.ResponseUri);
        }

        public Song LoadSongInformation(int entryIndex)
        {
            var sc = _scTracks[entryIndex - FILE_SYSTEM_ENTRY_OFFSET];
            return new Song
            {
                FileSystem = this,
                SongBase = new SongBase
                {
                    InternalName = sc.Uri,
                    Artist = sc.User.UserName,
                    Identifier = sc.Title,
                    TrackName = sc.Title,
                    FileSystemFriendlyName = FriendlyName,
                }
            };
        }

        public bool SongExists(SongBase song)
        {
            //return _scclient.Resolve.GetTrack(song.InternalName) != null;
            return true;
        }

        public void Focused()
        {
        }
    }
}
