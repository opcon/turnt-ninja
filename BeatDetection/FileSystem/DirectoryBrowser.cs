using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio;
using Substructio.GUI;
using Substructio.Core;
using OpenTK.Input;
using BeatDetection.GUI;
using OpenTK;
using OpenTK.Graphics;

namespace BeatDetection.FileSystem
{
    class DirectoryBrowser
    {
        SceneManager _parentManager;
        ChooseSongScene _parentScene;
        List<IFileSystem> _fileSystemCollection;
        IFileSystem _currentFileSystem;
        List<FileBrowserEntry> _fileSystemEntries;

        public FileBrowserEntry EntrySeparator { get; private set; }

        //int _entryIndex = 0;
        int _fileSystemEntryIndex = 0;
        int _directoryBrowserEntryIndex = 0;

        int _fileSystemEntryIndexOffset { get { return _fileSystemCollection.Count + 1; } }

        int _halfEntryDrawCount = 10;
        float _verticalEntrySpacing = 30;


        public DirectoryBrowser(SceneManager parentSceneManager, ChooseSongScene parentScene)
        {
            _parentManager = parentSceneManager;
            _parentScene = parentScene;
            _fileSystemCollection = new List<IFileSystem>();
            _fileSystemEntries = new List<FileBrowserEntry>();

            EntrySeparator = new FileBrowserEntry
            {
                Path = "./",
                Name = "--------------------",
                EntryType = FileBrowserEntryType.Separator
            };
        }

        public void AddFileSystem(IFileSystem fileSystem)
        {
            _fileSystemCollection.Add(fileSystem);
            fileSystem.FileSystemCollection = _fileSystemCollection;
            LoadFileSystem(fileSystem);
        }

        private void LoadFileSystem(IFileSystem fileSystem)
        {
            _directoryBrowserEntryIndex =  fileSystem.Initialise(EntrySeparator) + _fileSystemEntryIndexOffset;
            _currentFileSystem = fileSystem;
        }

        public void Update(double time)
        {
            if (InputSystem.NewKeys.Contains(Key.Enter) && !_fileSystemEntries[_directoryBrowserEntryIndex].EntryType.HasFlag(FileBrowserEntryType.Separator))
            {
                _fileSystemEntryIndex = _directoryBrowserEntryIndex - _fileSystemEntryIndexOffset;
                if (_fileSystemEntryIndex >= 0)
                {
                    var isSong = _currentFileSystem.EntrySelected(ref _fileSystemEntryIndex);
                    if (isSong) _parentScene.SongChosen(_currentFileSystem.LoadSongInformation(_fileSystemEntryIndex));

                    _directoryBrowserEntryIndex = _fileSystemEntryIndex + _fileSystemEntryIndexOffset;
                }
                else
                {
                    var entry = _fileSystemEntries[_directoryBrowserEntryIndex];
                    if (entry.EntryType.HasFlag(FileBrowserEntryType.Plugin)) LoadFileSystem(_fileSystemCollection[_directoryBrowserEntryIndex]);

                }
            }

            // Update the entry list
            _fileSystemEntries.Clear();

            // Add the filesystem plugins
            _fileSystemEntries.AddRange(_fileSystemCollection.Select(fs => new FileBrowserEntry
            {
                Name = fs.FriendlyName,
                Path = "",
                EntryType = FileBrowserEntryType.Plugin
            }));

            _fileSystemEntries.Add(EntrySeparator);

            // Add the current filesystem's files
            _fileSystemEntries.AddRange(_currentFileSystem.FileSystemEntryCollection);

            // Update index
            if (InputSystem.NewKeys.Contains(Key.Up))
                _directoryBrowserEntryIndex--;
            if (InputSystem.NewKeys.Contains(Key.Down))
                _directoryBrowserEntryIndex++;
            if (InputSystem.NewKeys.Contains(Key.Left))
                _directoryBrowserEntryIndex -= 10;
            if (InputSystem.NewKeys.Contains(Key.Right))
                _directoryBrowserEntryIndex += 10;

            // Update text search
            foreach (var c in InputSystem.PressedChars)
            {
                int match = _fileSystemEntries.FindIndex(fbe => fbe.Name.StartsWith(c.ToString(), StringComparison.CurrentCultureIgnoreCase) && !fbe.EntryType.HasFlag(FileBrowserEntryType.Special));
                if (match >= 0) _directoryBrowserEntryIndex = match;
            }

            // Clamp the index
            if (_directoryBrowserEntryIndex < 0) _directoryBrowserEntryIndex = 0;
            if (_directoryBrowserEntryIndex >= _fileSystemEntries.Count) _directoryBrowserEntryIndex = _fileSystemEntries.Count - 1;
        }

        public void Draw(double time)
        {
            float startY = _verticalEntrySpacing * _halfEntryDrawCount;

            for (int i = _directoryBrowserEntryIndex - _halfEntryDrawCount; i < _directoryBrowserEntryIndex + _halfEntryDrawCount; i++)
            {
                if (i >= 0 && i < _fileSystemEntries.Count && i != _directoryBrowserEntryIndex) _parentManager.DrawTextLine(_fileSystemEntries[i].Name, new Vector3(0, startY, 0), Color4.Black, QuickFont.QFontAlignment.Centre);
                startY -= 30;
            }
            _parentManager.DrawTextLine(_fileSystemEntries[_directoryBrowserEntryIndex].Name, new Vector3(0, 0, 0), Color4.White, QuickFont.QFontAlignment.Centre);
        }
    }
}
