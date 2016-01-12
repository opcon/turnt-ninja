﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Audio;
using System.Diagnostics;

namespace BeatDetection.FileSystem
{
    class LocalFileSystem : IFileSystem
    {
        string[] _fileFilter;
        FileBrowserEntry _entrySeparator;
        List<FileBrowserEntry> _drives;
        List<FileBrowserEntry> _localFileSystemEntries;
        List<FileBrowserEntry> _userFolders;

        public ReadOnlyCollection<FileBrowserEntry> FileSystemEntryCollection { get { return _localFileSystemEntries.AsReadOnly(); } }
        public string FriendlyName { get { return "Local File System"; } }

        public LocalFileSystem()
        {
            _fileFilter = CSCore.Codecs.CodecFactory.Instance.GetSupportedFileExtensions();

            _drives = new List<FileBrowserEntry>();
            _localFileSystemEntries = new List<FileBrowserEntry>();
            _userFolders = new List<FileBrowserEntry>();
        }

        public int Initialise(FileBrowserEntry separator)
        {
            _entrySeparator = separator;

            // populate the drive list
            _drives.Clear();
            _localFileSystemEntries.Clear();
            _userFolders.Clear();
            foreach (var driveInfo in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                _drives.Add(new FileBrowserEntry
                {
                    Path = driveInfo.RootDirectory.FullName,
                    EntryType = FileBrowserEntryType.Directory | FileBrowserEntryType.Drive | FileBrowserEntryType.Special,
                    Name = string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? driveInfo.Name : string.Format("{1} ({0})", driveInfo.Name, driveInfo.VolumeLabel)
                });
            }

            return EnterPath(_drives.First().Path);
        }

        public bool EntrySelected(ref int entryIndex)
        {
            var entry = _localFileSystemEntries[entryIndex];

            // If a song has been selected, return immediately and notify the caller that the song is ready to be loaded
            if (entry.EntryType.HasFlag(FileBrowserEntryType.Song) && File.Exists(entry.Path)) return true;
            entryIndex = EnterPath(entry.Path);

            return false;
        }

        private int EnterPath(string directoryPath)
        {
            // Sanity check that directory exists
            if (!Directory.Exists(directoryPath)) throw new Exception("Directory doesn't exist: " + directoryPath);

            // Get a list of children in new directory
            var childrenDirectories = Directory.EnumerateDirectories(directoryPath).Where(d => !new DirectoryInfo(d).Attributes.HasFlag(FileAttributes.Hidden));
            var childrenFiles = Directory.EnumerateFiles(directoryPath).Where(p => _fileFilter.Any(f => p.EndsWith(f, StringComparison.OrdinalIgnoreCase)));

            // Clear the file system entry list
            _localFileSystemEntries.Clear();

            // Add the drive list
            _localFileSystemEntries.AddRange(_drives);

            // Add Drive/Directory separator
            _localFileSystemEntries.Add(_entrySeparator);

            int desiredIndex = _localFileSystemEntries.Count;

            // Add shortcut to parent directory
            _localFileSystemEntries.Add(new FileBrowserEntry
            {
                Path = Path.Combine(directoryPath, "../"),
                EntryType = FileBrowserEntryType.Directory | FileBrowserEntryType.Special,
                Name = "Parent Directory"
            });

            // Add the new directories
            foreach (var dir in childrenDirectories.OrderBy(Path.GetFileName))
            {
                _localFileSystemEntries.Add(new FileBrowserEntry
                {
                    Path = dir,
                    Name = Path.GetFileName(dir),
                    EntryType = FileBrowserEntryType.Directory
                });
            }

            // Add Directory/File separator
            _localFileSystemEntries.Add(_entrySeparator);

            // Add the new files
            foreach (var file in childrenFiles.OrderBy(Path.GetFileName))
            {
                _localFileSystemEntries.Add(new FileBrowserEntry
                {
                    Path = file,
                    Name = Path.GetFileNameWithoutExtension(file),
                    EntryType = FileBrowserEntryType.Song
                });
            }

            // Update the index position
            if (_localFileSystemEntries.Count <= desiredIndex) desiredIndex = _localFileSystemEntries.Count - 1;

            return desiredIndex;
        }

        public Song LoadSongInformation(int entryIndex)
        {
            var entry = _localFileSystemEntries[entryIndex];
            TagLib.Tag tag;

            // Load the tag information
            string artist = "";
            string title = "";

            if (TagLib.SupportedMimeType.AllExtensions.Any(s => entry.Path.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                using (var fs = File.Open(entry.Path, FileMode.Open))
                {
                    try
                    {
                        var file = TagLib.File.Create(new TagLib.StreamFileAbstraction(entry.Name, fs, fs));
                        tag = file.Tag;
                        artist = tag.AlbumArtists.Count() > 0 ? tag.AlbumArtists[0] : "";
                        title = tag.Title;
                        file.Dispose();
                    }
                    catch (TagLib.UnsupportedFormatException ex)
                    {
                    }
                }
            }

            title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(entry.Path) : title;
            artist = string.IsNullOrWhiteSpace(title) ? "Unkown Artist" : artist; 

            // Initialise the song variable
            var ret = new Song
            {
                InternalName = entry.Path,
                Artist = artist,
                Identifier = entry.Name,
                TrackName = title,
                FileSystem = this
            };

            // Song information is loaded so we return it
            return ret;
        }

        public void LoadSongAudio(Song song)
        {
            // Sanity checks
            if (!File.Exists(song.InternalName)) throw new Exception("File not found: " + song.InternalName);

            song.SongAudio = CSCore.Codecs.CodecFactory.Instance.GetCodec(song.InternalName);
            song.SongAudioLoaded = true;
        }
    }
}