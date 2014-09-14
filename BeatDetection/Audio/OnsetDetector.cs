using System.Collections.Generic;
using Substructio.Audio;

namespace BeatDetection
{
    public abstract class OnsetDetector
    {
        public List<float> Beats = new List<float>();

        public OnsetDetector()
        {
        }

        public abstract void DetectBeats();
    }
}
