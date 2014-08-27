using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace BeatDetection
{
    class QMVampWrapper : OnsetDetector
    {
        string beats;
        float correctionAmount = 0;
        public QMVampWrapper(AudioWrapper a, string f, float correction) : base(a)
        {
            beats = f;
            correctionAmount = correction;
        }

        public override void DetectBeats()
        {
            using (StreamReader sr = new StreamReader(beats))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Beats.Add(float.Parse(line.Split(',')[0]) + correctionAmount);
                }
                sr.Close();
            }
        }
    }
}
