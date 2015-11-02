using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnsetDetection;
using CSCore;

namespace BeatDetection.Audio
{
    class AudioFeatures
    {
        private OnsetDetector _onsetDetector;
        string _csvDirectory;
        string _outputSuffix = "onsets";

        public List<float> Onsets = new List<float>();
        public float _correction;
        private IProgress<string> _outerProgressReporter;
        private IProgress<string> _innerProgressReporter;
        private string _currentTask = "";

        public AudioFeatures(DetectorOptions options, string csvDirectory, float correction, IProgress<string> progress = null)
        {
            //force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, true);

            _csvDirectory = csvDirectory;
            _outerProgressReporter = progress ?? new Progress<string>();
            _correction = correction;
            _innerProgressReporter = new Progress<string>(status =>
            {
                _outerProgressReporter.Report(_currentTask + ":" + status);
            });

            _onsetDetector = new OnsetDetector(options, _innerProgressReporter);
        }

        public bool SongAnalysed(string audioPath)
        {
            return File.Exists(GetOnsetFilePath(audioPath));
        }

        public void Extract(string audioFilePath)
        {
            _currentTask = "Extracting Onsets"; 
            ExtractOnsets(audioFilePath);

            //force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        public void Extract(CSCore.IWaveSource audioSource)
        {
            _currentTask = "Extracting Onsets";
            ExtractOnsets(audioSource);

            //force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        private void ExtractOnsets(string audioFilePath)
        {
            List<float> onsets;
            if (SongAnalysed(audioFilePath))
                onsets = LoadOnsets(GetOnsetFilePath(audioFilePath));
            else
            {
                onsets = _onsetDetector.Detect(audioFilePath);
                SaveOnsets(GetOnsetFilePath(audioFilePath), onsets);
            }

            ApplyCorrection(onsets, _correction);
            Onsets = onsets;
        }

        private void ExtractOnsets(CSCore.IWaveSource audioSource)
        {
            var onsets = _onsetDetector.Detect(audioSource.ToSampleSource());

            ApplyCorrection(onsets, _correction);
            Onsets = onsets;
        }

        private void SaveOnsets(string onsetFile, List<float> onsets)
        {
            using (StreamWriter sw = new StreamWriter(onsetFile))
            {
                foreach (var onset in onsets)
                {
                    sw.WriteLine(onset);
                }
                sw.Close();
            }
        }

        private List<float> LoadOnsets(string onsetFile)
        {
            List<float> onsets = new List<float>();
            using (StreamReader sr = new StreamReader(onsetFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    onsets.Add(float.Parse(line.Split(',')[0]));
                }
                sr.Close();
            }
            return onsets;
        }

        private void ApplyCorrection(List<float> onsets, float correction)
        {
            for (int i = 0; i < onsets.Count; i++)
            {
                onsets[i] += correction;
            }
        }

        private string GetOnsetFilePath(string audioPath)
        {
            return Path.Combine(_csvDirectory, String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(audioPath), _outputSuffix) + ".csv");
        }
    }
}
