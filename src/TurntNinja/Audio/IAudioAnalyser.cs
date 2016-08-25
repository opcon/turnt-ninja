using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.Audio
{
    interface IAudioAnalyser
    {
        List<float> ExtractOnsets(string audioFilePath);
        bool SongAnalysed(string audioFilePath);
    }

    struct AnalysisArguments
    {
        public string CSVDirectory;
        public string AudioFilePath;
        public string InitialOutputSuffix;
        public string DesiredOutputSuffix;
        public float Correction;
    }
}
