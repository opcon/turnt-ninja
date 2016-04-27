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
        FileBrowserEntry _entrySeparator;
        string clientID = "74e6e3acb28021e21eb32ef4bc10e995";
        string clientSecret = "";
        ISoundCloudConnector _sconnector;
        IUnauthorizedSoundCloudClient _scclient;

        public List<IFileSystem> FileSystemCollection { get; set; }
        public ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get { return _soundcloudSongs.AsReadOnly(); } }

        public string FriendlyName { get { return "SoundCloud"; } }

        public SoundCloudFileSystem()
        {
            _soundcloudSongs = new List<FileBrowserEntry>();
            _scTracks = new List<SCTrack>();
        }

        public int Initialise(FileBrowserEntry separator)
        {
            _entrySeparator = separator;

            //setup soundcloud connection
            _sconnector = new SoundCloudConnector();
            _scclient = _sconnector.UnauthorizedConnect(clientID, clientSecret);

            //Get tracks
            var categories = _scclient.Explore.GetExploreCategories();
            _scTracks = _scclient.Chart.GetTracks(categories[0]).ToList();

            _soundcloudSongs = _scTracks.ConvertAll(s => new FileBrowserEntry { EntryType = FileBrowserEntryType.Song, Name = s.Title, Path = s.Uri });

            return 0;
        }

        public bool EntrySelected(ref int entryIndex)
        {
            //return true if we've found a song
            if (_soundcloudSongs[entryIndex].EntryType.HasFlag(FileBrowserEntryType.Song)) return true;

            //something has gone wrong
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
            var sc = _scTracks[entryIndex];
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
            return _scclient.Resolve.GetTrack(song.InternalName) != null;
        }
    }
}
