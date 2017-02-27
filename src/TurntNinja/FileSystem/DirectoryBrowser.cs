using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio;
using Substructio.GUI;
using Substructio.Core;
using OpenTK.Input;
using TurntNinja.GUI;
using OpenTK;
using OpenTK.Graphics;
using System.Globalization;
using System.IO;
using QuickFont;

namespace TurntNinja.FileSystem
{
    class DirectoryBrowser
    {
        SceneManager _parentManager;
        ChooseSongScene _parentScene;
        List<IFileSystem> _fileSystemCollection;
        IFileSystem _currentFileSystem;
        List<FileBrowserEntry> _fileSystemEntries;

        GameFont _unselectedFont;
        GameFont _selectedFont;
        GameFont _searchFont;

        public FileBrowserEntry EntrySeparator { get; private set; }

        //int _entryIndex = 0;
        int _fileSystemEntryIndex = 0;
        int _directoryBrowserEntryIndex = 0;


        int _halfEntryDrawCount = 10;

        string _searchString = "";
        float _searchTimeout = 2.0f;
        double _searchLastTime = 0.0f;
        double _searchElapsedTime = 0.0f;

        QFontDrawing _qfontDrawing;

        public string SearchString
        {
            get { return _searchString; }
        }

        public DirectoryBrowser(SceneManager parentSceneManager, ChooseSongScene parentScene)
        {
            _parentManager = parentSceneManager;
            _parentScene = parentScene;
            _fileSystemCollection = new List<IFileSystem>();
            _fileSystemEntries = new List<FileBrowserEntry>();

            _unselectedFont = _parentManager.GameFontLibrary.GetFirstOrDefault(GameFontType.Body);
            _selectedFont = _parentManager.GameFontLibrary.GetFirstOrDefault("selected");
            _searchFont = _parentManager.GameFontLibrary.GetFirstOrDefault(GameFontType.Heading);

            _qfontDrawing = new QFontDrawing();

            Resize(_parentManager.Width, _parentManager.Height);

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
            _directoryBrowserEntryIndex = fileSystem.Initialise(EntrySeparator);
            SwitchFileSystem(fileSystem);
        }

        private void SwitchFileSystem(IFileSystem fileSystem)
        {
            fileSystem.Focused();
            _currentFileSystem = fileSystem;
        }
        
        private void ResetSearch()
        {
            _searchLastTime = _searchElapsedTime = 0.0f;
            _searchString = "";
        }

        public void SwitchCurrentFileSystemIfEmpty()
        {
            if (_currentFileSystem.FileSystemEntryCollection.Count == 0)
                SwitchFileSystem(_fileSystemCollection.Find(f => f is LocalFileSystem));
        }

        public void RefreshRecentSongFilesystem()
        {
            foreach (var f in _fileSystemCollection)
            {
                if (f is RecentFileSystem)
                    f.Focused();
            }
        }

        public void Resize(int wWidth, int wHeight)
        {
            // Account for file system chooser, selected song and search bar and spacing
            float height = wHeight - 3*_selectedFont.MaxLineHeight;
            _halfEntryDrawCount = (int) (height / _unselectedFont.MaxLineHeight) / 2;

            _qfontDrawing.ProjectionMatrix = _parentScene.SceneManager.ScreenCamera.ScreenProjectionMatrix;
        }

        public void Update(double time)
        {
            // Check if we are switching file systems
            if (InputSystem.NewKeys.Contains(Key.Left))
            {
                var index = _fileSystemCollection.IndexOf(_currentFileSystem);
                SwitchFileSystem(_fileSystemCollection[(index - 1 + _fileSystemCollection.Count) % _fileSystemCollection.Count]);
            }
            if (InputSystem.NewKeys.Contains(Key.Right))
            {
                var index = _fileSystemCollection.IndexOf(_currentFileSystem);
                SwitchFileSystem(_fileSystemCollection[(index + 1 + _fileSystemCollection.Count) % _fileSystemCollection.Count]);
            }

            if (InputSystem.NewKeys.Contains(Key.Enter) && _fileSystemEntries.Count > 0 && !_fileSystemEntries[_directoryBrowserEntryIndex].EntryType.HasFlag(FileBrowserEntryType.Separator))
            {
                SoundCloudFileSystem sfc;
                if (!string.IsNullOrWhiteSpace(_searchString) && (sfc = _currentFileSystem as SoundCloudFileSystem) != null)
                {
                    sfc.Search(_searchString);
                    _fileSystemEntryIndex = 0;
                }
                else
                {
                    _fileSystemEntryIndex = _directoryBrowserEntryIndex;
                    if (_fileSystemEntryIndex >= 0)
                    {
                        var isSong = _currentFileSystem.EntrySelected(ref _fileSystemEntryIndex);
                        if (isSong) _parentScene.SongChosen(_currentFileSystem.LoadSongInformation(_fileSystemEntryIndex));

                        _directoryBrowserEntryIndex = _fileSystemEntryIndex;
                    }
                    else
                    {
                        var entry = _fileSystemEntries[_directoryBrowserEntryIndex];
                        if (entry.EntryType.HasFlag(FileBrowserEntryType.Plugin)) SwitchFileSystem(_fileSystemCollection[_directoryBrowserEntryIndex]);

                    }
                }
                ResetSearch();
            }

            // Update the entry list
            _fileSystemEntries.Clear();

//             // Add the filesystem plugins
//             _fileSystemEntries.AddRange(_fileSystemCollection.Select(fs => new FileBrowserEntry
//             {
//                 Name = fs.FriendlyName,
//                 Path = "",
//                 EntryType = FileBrowserEntryType.Plugin
//             }));
// 
//             _fileSystemEntries.Add(EntrySeparator);

            // Add the current filesystem's files
            _fileSystemEntries.AddRange(_currentFileSystem.FileSystemEntryCollection);

            // Update index
            if (InputSystem.NewKeys.Contains(Key.Up))
                _directoryBrowserEntryIndex--;
            if (InputSystem.NewKeys.Contains(Key.Down))
                _directoryBrowserEntryIndex++;
//             if (InputSystem.NewKeys.Contains(Key.Left))
//                 _directoryBrowserEntryIndex -= 10;
//             if (InputSystem.NewKeys.Contains(Key.Right))
//                 _directoryBrowserEntryIndex += 10;

            _searchElapsedTime += time;
            if (_searchElapsedTime - _searchLastTime > _searchTimeout)
                ResetSearch();

            // Update text search
            foreach (var c in InputSystem.PressedChars)
            {
                _searchString = (_searchString + c).ToLowerInvariant();
                _searchElapsedTime = _searchLastTime = 0.0f;
                int match = _fileSystemEntries.FindIndex(fbe => fbe.Name.StartsWith(_searchString, StringComparison.CurrentCultureIgnoreCase));
                if (match < 0)
                    match = _fileSystemEntries.FindIndex(fbe => CultureInfo.CurrentCulture.CompareInfo.IndexOf(fbe.Name, _searchString, CompareOptions.IgnoreCase) >= 0 &&
                        !(fbe.EntryType.HasFlag(FileBrowserEntryType.Special) || fbe.EntryType.HasFlag(FileBrowserEntryType.Plugin)));
                if (match >= 0) _directoryBrowserEntryIndex = match;
            }
            if (InputSystem.NewKeys.Contains(Key.BackSpace) && _searchString.Length > 0)
            {
                _searchString = _searchString.Substring(0, _searchString.Length - 1);
                _searchElapsedTime = _searchLastTime = 0.0f;
            }

            // Clamp the index
            if (_directoryBrowserEntryIndex < 0) _directoryBrowserEntryIndex = 0;
            if (_directoryBrowserEntryIndex >= _fileSystemEntries.Count) _directoryBrowserEntryIndex = _fileSystemEntries.Count - 1;
        }

        public void Draw(double time)
        {
            _qfontDrawing.DrawingPrimitives.Clear();
            var col = Color4.White;

            if (_fileSystemEntries.Count > 0)
            {
                var s = _selectedFont.Font.Measure(_fileSystemEntries[_directoryBrowserEntryIndex].Name);
                _parentManager.DrawTextLine(_fileSystemEntries[_directoryBrowserEntryIndex].Name, new Vector3(0, s.Height / 2.0f, 0), Color4.White, QFontAlignment.Centre, _selectedFont.Font);

                float startY = _unselectedFont.MaxLineHeight * (_halfEntryDrawCount) + s.Height * 0.5f + _unselectedFont.MaxLineHeight * 0.5f;
                for (int i = _directoryBrowserEntryIndex - (_halfEntryDrawCount); i < _directoryBrowserEntryIndex + _halfEntryDrawCount; i++)
                {
                    if (i >= 0 && i < _fileSystemEntries.Count && i != _directoryBrowserEntryIndex)
                        _parentManager.DrawTextLine(_fileSystemEntries[i].Name, new Vector3(0, startY, 0), col, QFontAlignment.Centre, _unselectedFont.Font);
                    if (i == _directoryBrowserEntryIndex)
                        startY -= s.Height + _unselectedFont.MaxLineHeight * 1.5f;
                    if (i != _directoryBrowserEntryIndex)
                        startY -= _unselectedFont.MaxLineHeight;
                }
            }

            // Draw file systems
            var size = _parentManager.DrawTextLine(_currentFileSystem.FriendlyName, new Vector3(0, (_parentManager.Height / 2), 0), Color4.White, QFontAlignment.Centre, _selectedFont.Font);

            int currentFSIndex = _fileSystemCollection.IndexOf(_currentFileSystem);

            // Draw two file systems on either side
            float unselectedFsScale = 0.9f;

            col = Color4.White;
            col.A = 1.0f;

            var dp = new QFontDrawingPrimitive(_selectedFont.Font, new QFontRenderOptions { Colour = (System.Drawing.Color)col });
            // Draw next fs on right
            dp.Print(
                    _fileSystemCollection[(currentFSIndex + 1 + _fileSystemCollection.Count) % _fileSystemCollection.Count].FriendlyName,
                    Vector3.Zero,
                    QFontAlignment.Left);
            dp.ModelViewMatrix = Matrix4.CreateTranslation(new Vector3(0, size.Height * 2.0f, 0))
                                    * Matrix4.CreateScale(unselectedFsScale)
                                    * Matrix4.CreateTranslation(new Vector3(size.Width * 0.75f, _parentManager.Height / 2 - size.Height * 2.0f, 0));

            _qfontDrawing.DrawingPrimitives.Add(dp);

            dp = new QFontDrawingPrimitive(_selectedFont.Font, new QFontRenderOptions { Colour = (System.Drawing.Color)col });
            // Draw previous fs on left
            dp.Print(
                    _fileSystemCollection[(currentFSIndex - 1 + _fileSystemCollection.Count) % _fileSystemCollection.Count].FriendlyName,
                    Vector3.Zero,
                    QFontAlignment.Right);
            dp.ModelViewMatrix = Matrix4.CreateTranslation(new Vector3(0, size.Height * 2.0f, 0))
                                    * Matrix4.CreateScale(unselectedFsScale)
                                    * Matrix4.CreateTranslation(new Vector3(-size.Width * 0.75f, _parentManager.Height / 2 - size.Height * 2.0f, 0));
            _qfontDrawing.DrawingPrimitives.Add(dp);

            _qfontDrawing.RefreshBuffers();
            _qfontDrawing.Draw();

            var searchString = string.Format("Search: {0}", _searchString);
            _parentManager.DrawTextLine(searchString, new Vector3(0, -(_parentManager.Height / 2) + _searchFont.MaxLineHeight, 0), Color4.White, QuickFont.QFontAlignment.Centre, _searchFont.Font);
        }
    }
}
