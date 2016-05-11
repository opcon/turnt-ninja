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

        private object _lock = new object();

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
            Task.Run(() =>
            {
                lock (_lock )
                {
                    //_scCategories = _scclient.Explore.GetExploreCategories().ToList();
                    _scCategories = GetSoundcloudCategories();
                    ShowCategories();
                }
            });

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

        public static List<SCExploreCategory> GetSoundcloudCategories()
        {
            return new List<SCExploreCategory>
            {
                new SCExploreCategory { Name = "Popular+Music" },
                new SCExploreCategory { Name = "Alternative+Rock" },
                new SCExploreCategory { Name = "Ambient" },
                new SCExploreCategory { Name = "Classical" },
                new SCExploreCategory { Name = "Country" },
                new SCExploreCategory { Name = "Danceedm" },
                new SCExploreCategory { Name = "Dancehall" },
                new SCExploreCategory { Name = "Deephouse" },
                new SCExploreCategory { Name = "Disco" },
                new SCExploreCategory { Name = "Drumbass" },
                new SCExploreCategory { Name = "Dubstep" },
                new SCExploreCategory { Name = "Electronic" },
                new SCExploreCategory { Name = "Folksingersongwriter" },
                new SCExploreCategory { Name = "Hiphoprap" },
                new SCExploreCategory { Name = "House" },
                new SCExploreCategory { Name = "Indie" },
                new SCExploreCategory { Name = "Jazzblues" },
                new SCExploreCategory { Name = "Latin" },
                new SCExploreCategory { Name = "Metal" },
                new SCExploreCategory { Name = "Piano" },
                new SCExploreCategory { Name = "Pop" },
                new SCExploreCategory { Name = "Rbsoul" },
                new SCExploreCategory { Name = "Reggae" },
                new SCExploreCategory { Name = "Reggaeton" },
                new SCExploreCategory { Name = "Rock" },
                new SCExploreCategory { Name = "Soundtrack" },
                new SCExploreCategory { Name = "Techno" },
                new SCExploreCategory { Name = "Trance" },
                new SCExploreCategory { Name = "Trap" },
                new SCExploreCategory { Name = "Triphop" },
                new SCExploreCategory { Name = "World" },
            };
        }
    }
}
