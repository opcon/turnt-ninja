using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.Audio
{
    class AudioFeatures
    {
        private SonicAnnotatorWrapper _annotatorWrapper;
        public List<float> Onsets = new List<float>();
        public List<SegmentInformation> Segments = new List<SegmentInformation>();
        public float _correction;
        private IProgress<string> _outerProgressReporter;
        private IProgress<string> _innerProgressReporter;
        private string _currentTask = "";

        public AudioFeatures(string sonicAnnotatorPath, string pluginPath, string csvDirectory, float correction, IProgress<string> progress = null)
        {
            _outerProgressReporter = progress ?? new Progress<string>();
            _annotatorWrapper = new SonicAnnotatorWrapper(new SonicAnnotatorArguments{SonicAnnotatorPath = sonicAnnotatorPath, PluginsPath = pluginPath, CSVDirectory = csvDirectory});
            _correction = correction;
            _innerProgressReporter = new Progress<string>(status =>
            {
                _outerProgressReporter.Report(_currentTask + ":" + status);
            });
        }

        public void Extract(string audioFilePath)
        {
            _currentTask = "Extracting Offsets"; 
            ExtractOnsets(audioFilePath);
            _currentTask = "Extracting Segments";
            ExtractSegments(audioFilePath);
        }

        private void ExtractOnsets(string audioFilePath)
        {
            var args = new SonicAnnotatorArguments
            {
                AudioFilePath = audioFilePath,
                Correction = _correction,
                InitialOutputSuffix = "vamp_qm-vamp-plugins_qm-onsetdetector_onsets",
                DesiredOutputSuffix = "onsets",
                DescriptorPath = "../../Processed Songs/qmonset.xml"
            };
            string resultPath;
            bool success = _annotatorWrapper.Run(args, out resultPath, _innerProgressReporter);

            if (!success) throw new Exception("Error during sonic annotator onset processing");

            using (StreamReader sr = new StreamReader(resultPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Onsets.Add(float.Parse(line.Split(',')[0]) + _correction);
                }
                sr.Close();
            }
        }

        private void ExtractSegments(string audioFilePath)
        {
            var args = new SonicAnnotatorArguments
            {
                AudioFilePath = audioFilePath,
                Correction = _correction,
                InitialOutputSuffix = "vamp_qm-vamp-plugins_qm-segmenter_segmentation",
                DesiredOutputSuffix = "segments",
                DescriptorPath = "../../Processed Songs/qmsegments.xml"
            };
            string resultPath;
            bool success = _annotatorWrapper.Run(args, out resultPath, _innerProgressReporter);

            if (!success) throw new Exception("Error during sonic annotator segment processing");

            using (StreamReader sr = new StreamReader(resultPath))
            {
                string line = null, nextLine;
                bool done = sr.EndOfStream;
                if (!done) line = sr.ReadLine();
                while (!done)
                {
                    var segment = new SegmentInformation();
                    var pieces1 = line.Split(',');
                    segment.StartTime = double.Parse(pieces1[0]);
                    segment.ID = int.Parse(pieces1[1]);
                    if (!sr.EndOfStream)
                    {
                        nextLine = sr.ReadLine();
                        var pieces2 = nextLine.Split(',');
                        segment.EndTime = double.Parse(pieces2[0]);
                        line = nextLine;
                    }
                    else
                        done = true;
                    Segments.Add(segment);
                }
            }
        }

    }
}
