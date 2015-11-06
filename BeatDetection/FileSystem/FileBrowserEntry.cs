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
        Song = 0,
        Directory = 1 << 0,
        Drive = 1 << 1,
        Special = 1 << 2,
        Separator = 1 << 3,
        Plugin = 1 << 4
    }
}
