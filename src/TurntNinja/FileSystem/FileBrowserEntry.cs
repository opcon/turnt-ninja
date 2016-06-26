using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.FileSystem
{
    public struct FileBrowserEntry
    {
        public string Path;
        public string Name;
        public FileBrowserEntryType EntryType;
    }

    [Flags]
    public enum FileBrowserEntryType
    {
        Song = 1 << 0,
        Directory = 1 << 1,
        Drive = 1 << 2,
        Special = 1 << 3,
        Separator = 1 << 4,
        Plugin = 1 << 5
    }
}
