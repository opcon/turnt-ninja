using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetection.Audio
{
    class SonicAnnotatorWrapper
    {
        //protected string _sonicAnnotatorPath;
        //protected string _audioFilePath;
        //protected string _pluginsPath;
        //protected string _outputSuffix;
        //protected string _pluginDescriptorPath;
        //protected float _correction;

        protected SonicAnnotatorArguments _baseArguments;

        public SonicAnnotatorWrapper(SonicAnnotatorArguments arguments)
        {
            _baseArguments = arguments;
        }

        public bool Run(SonicAnnotatorArguments arguments, out string resultPath)
        {
            if (string.IsNullOrWhiteSpace(arguments.SonicAnnotatorPath))
                arguments.SonicAnnotatorPath = _baseArguments.SonicAnnotatorPath;
            if (string.IsNullOrWhiteSpace(arguments.PluginsPath))
                arguments.PluginsPath = _baseArguments.PluginsPath;
            if (string.IsNullOrWhiteSpace(arguments.CSVDirectory))
                arguments.CSVDirectory = _baseArguments.CSVDirectory;

            return ExecuteSonicAnnotator(arguments, out resultPath);
        }

        private bool ExecuteSonicAnnotator(SonicAnnotatorArguments arguments, out string resultPath)
        {
            var psi = new ProcessStartInfo(arguments.SonicAnnotatorPath) {WorkingDirectory = GameController.AssemblyDirectory};
            psi.EnvironmentVariables.Add("VAMP_PATH", arguments.PluginsPath);
            //var csvDir = "../../Processed Songs/";
            var args = String.Format("-t \"{0}\" \"{1}\" -w csv --csv-force --csv-basedir \"{2}\"", arguments.DescriptorPath, arguments.AudioFilePath, arguments.CSVDirectory.Replace(@"\", "/"));
            psi.Arguments = args;
            psi.CreateNoWindow = false;
            psi.UseShellExecute = false;
            //psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            var result = Path.Combine(arguments.CSVDirectory, String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(arguments.AudioFilePath), arguments.InitialOutputSuffix) + ".csv");
            var newName = Path.Combine(Path.GetDirectoryName(result), String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(arguments.AudioFilePath), arguments.DesiredOutputSuffix + ".csv"));

            if (File.Exists(newName))
            {
                resultPath = newName;
                return true;
            }

            string q = "";
            string e = "";
            var p = Process.Start(psi);
            while (!p.HasExited)
            {
                //q += p.StandardOutput.ReadToEnd();
                e += p.StandardError.ReadToEnd();
            }

            //Console.Write(q);

            if (File.Exists(result))
            {
                if (File.Exists(newName))
                    File.Delete(newName);
                File.Move(result, newName);
                resultPath = newName;
                return true;
            }

            resultPath = null;
            return false;
        }

        //public void Initialise(string audioPath, string sonicPath, string pluginPath, float correction, string descriptorPath = "../../Processed Songs/qmonset.n3", string outputSuffix = "vamp_qm-vamp-plugins_qm-onsetdetector_onsets")
        //{
        //    _audioFilePath = audioPath;
        //    _sonicAnnotatorPath = sonicPath;
        //    _correction = correction;
        //    _pluginDescriptorPath = descriptorPath;
        //    _outputSuffix = outputSuffix;
        //    _pluginsPath = pluginPath;
        //}
    }

    struct SonicAnnotatorArguments
    {
        public string SonicAnnotatorPath;
        public string PluginsPath;
        public string CSVDirectory;

        public string AudioFilePath;
        public string DescriptorPath;
        public string InitialOutputSuffix;
        public string DesiredOutputSuffix;
        public float Correction;

    }

    struct SegmentInformation
    {
        public double StartTime;
        public double EndTime;
        public int ID;

        public override string ToString()
        {
            return String.Format("{0} to {1}, ID = {2}", StartTime, EndTime, ID);
        }
    }
}
