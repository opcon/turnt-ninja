using System.Collections.Generic;
using Substructio.Audio;

namespace BeatDetection
{
    public abstract class OnsetDetector
    {
        public List<float> Beats = new List<float>();
        public AudioWrapper Audio { get; set; }

        public OnsetDetector(AudioWrapper a)
        {
            Audio = a;
        }

        public abstract void DetectBeats();
    }
}
