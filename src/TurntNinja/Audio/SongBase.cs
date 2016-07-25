using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurntNinja.Audio
{
    public class SongBase : IEquatable<SongBase>
    {
        public string FileSystemFriendlyName { get; set; }
        public string InternalName { get; set; }

        public string Identifier { get; set; }
        public string Artist { get; set; }
        public string TrackName { get; set; }

        public bool Equals(SongBase other)
        {
            return (FileSystemFriendlyName.Equals(other.FileSystemFriendlyName)) 
                && (InternalName.Equals(other.InternalName)) 
                && (Identifier.Equals(other.Identifier)) 
                && (Artist.Equals(other.Artist)) 
                && (TrackName.Equals(other.TrackName));
        }

        public override bool Equals(object obj)
        {
            SongBase s = obj as SongBase;
            if (s != null)
                return Equals(s);
            return false;
        }
    }
}
