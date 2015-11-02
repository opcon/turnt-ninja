using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using BeatDetection.Audio;


namespace BeatDetection.FileSystem
{
    interface IFileSystem
    {
        IWaveSource LoadSongAudio(Song song); 
    }
}
