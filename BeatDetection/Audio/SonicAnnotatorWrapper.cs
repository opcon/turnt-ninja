using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BeatDetection.Audio
{
    class SonicAnnotatorWrapper
    {
        private SonicAnnotatorArguments _baseArguments;
        private IProgress<string> _progressReporter;

        public SonicAnnotatorWrapper(SonicAnnotatorArguments arguments)
        {
            _baseArguments = arguments;
        }

        public bool Run(SonicAnnotatorArguments arguments, out string resultPath, IProgress<string> progress = null)
        {
            _progressReporter = progress ?? new Progress<string>();
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
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;

            var result = Path.Combine(arguments.CSVDirectory, String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(arguments.AudioFilePath), arguments.InitialOutputSuffix) + ".csv");
            var newName = Path.Combine(Path.GetDirectoryName(result), String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(arguments.AudioFilePath), arguments.DesiredOutputSuffix + ".csv"));

            if (File.Exists(newName))
            {
                resultPath = newName;
                return true;
            }

            string pattern = @"\s(\d{1,3})%";
            var p = Process.Start(psi);
            while (!p.HasExited)
            {
                string e = p.StandardError.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(e))
                {
                    var match = Regex.Match(e, pattern).ToString();
                    _progressReporter.Report(match);
                }
            }

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
