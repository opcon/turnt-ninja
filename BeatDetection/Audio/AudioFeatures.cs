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

        public List<float> OnsetTimes = new List<float>();
        public List<Onset> Onsets = new List<Onset>();
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

        //public void Extract(string audioFilePath)
        //{
        //    _currentTask = "Extracting Onsets"; 
        //    ExtractOnsets(audioFilePath);

        //    //force garbage collection
        //    GC.Collect(2, GCCollectionMode.Forced, true);
        //}

        public void Extract(CSCore.IWaveSource audioSource, Song s)
        {
            _currentTask = "Extracting Onsets";

            List<Onset> onsets;
            if (SongAnalysed(s.SongBase.InternalName))
                onsets = LoadOnsets(GetOnsetFilePath(s.SongBase.InternalName));
            else
            {
                onsets = _onsetDetector.Detect(audioSource.ToSampleSource());
                SaveOnsets(GetOnsetFilePath(s.SongBase.InternalName), onsets);
            }
            OnsetTimes = onsets.Select(o => o.OnsetTime).ToList();
            Onsets = onsets;
            ApplyCorrection(OnsetTimes, _correction);

            //force garbage collection
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        //private void ExtractOnsets(string audioFilePath)
        //{
        //    List<Onset> onsets;
        //    if (SongAnalysed(audioFilePath))
        //        onsets = LoadOnsets(GetOnsetFilePath(audioFilePath));
        //    else
        //    {
        //        onsets = _onsetDetector.Detect(audioFilePath);
        //        SaveOnsets(GetOnsetFilePath(audioFilePath), onsets);
        //    }
        //    OnsetTimes = onsets.Select(o => o.OnsetTime).ToList();
        //    Onsets = onsets;
        //    ApplyCorrection(OnsetTimes, _correction);
        //}

        //private void ExtractOnsets(CSCore.IWaveSource audioSource)
        //{
        //    var onsets = _onsetDetector.Detect(audioSource.ToSampleSource());

        //    OnsetTimes = onsets.Select(o => o.OnsetTime).ToList();
        //    Onsets = onsets;
        //    ApplyCorrection(OnsetTimes, _correction);
        //}

        private void SaveOnsets(string onsetFile, List<Onset> onsets)
        {
            using (StreamWriter sw = new StreamWriter(onsetFile))
            {
                foreach (var onset in onsets)
                {
                    sw.WriteLine(onset.ToString());
                }
                sw.Close();
            }
        }

        private List<Onset> LoadOnsets(string onsetFile)
        {
            List<Onset> onsets = new List<Onset>();
            using (StreamReader sr = new StreamReader(onsetFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    onsets.Add(new Onset { OnsetTime = float.Parse(line.Split(',')[0]), OnsetAmplitude = float.Parse(line.Split(',')[1]) });
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
