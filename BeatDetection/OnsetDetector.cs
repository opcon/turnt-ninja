using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
