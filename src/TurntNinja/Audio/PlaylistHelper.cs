using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurntNinja.Audio
{
    public static class PlaylistHelper
    {
        public static List<string> LoadPlaylist(string playlistFile)
        {
            StreamReader file = File.OpenText(playlistFile);
            var lines = new List<string>();
            string line;
            while ((line = file.ReadLine()) != null)
            {
                //check that this line is not a comment
                if (line.StartsWith("#")) continue;
                lines.Add(line);
            }

            return lines;
        }
    }
}
