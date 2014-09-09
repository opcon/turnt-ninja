using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using Substructio.Audio;

namespace BeatDetection
{
    class QMVampWrapper : OnsetDetector
    {
        string _audioPath;
        string _sonicPath;
        private string _pluginsPath;
        string _descriptorPath;
        string _outputSuffix;
        float _correctionAmount;

        string beatsFile;

        public QMVampWrapper(string audioPath, string sonicPath, string pluginPath, float correction, string descriptorPath = "../../Processed Songs/qmonset.n3", string outputSuffix = "vamp_qm-vamp-plugins_qm-onsetdetector_onsets")
        {
            _audioPath = audioPath;
            _sonicPath = sonicPath;
            _correctionAmount = correction;
            _descriptorPath = descriptorPath;
            _outputSuffix = outputSuffix;
            _pluginsPath = pluginPath;
        }

        public override void DetectBeats()
        {
            CallSonicAnnotator();
            using (StreamReader sr = new StreamReader(beatsFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Beats.Add(float.Parse(line.Split(',')[0]) + _correctionAmount);
                }
                sr.Close();
            }
        }

        void CallSonicAnnotator()
        {

            ProcessStartInfo psi = new ProcessStartInfo(_sonicPath);
            psi.WorkingDirectory = GameController.AssemblyDirectory;
            psi.EnvironmentVariables.Add("VAMP_PATH", _pluginsPath);
            var csvDir = "../../Processed Songs/";
            var arguments = String.Format("-t \"{0}\" \"{1}\" -w csv --csv-force --csv-basedir \"{2}\"", _descriptorPath, _audioPath, csvDir.Replace(@"\", "/"));
            psi.Arguments = arguments;
            //psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            //psi.RedirectStandardOutput = true;
            //psi.RedirectStandardError = true;

            var result = Path.Combine(csvDir, String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(_audioPath), _outputSuffix) + ".csv");
            var newName = Path.Combine(Path.GetDirectoryName(result), Path.GetFileNameWithoutExtension(_audioPath) + ".csv");

            if (File.Exists(newName))
            {
                beatsFile = newName;
                return;
            }

            string q = "";
            string e = "";
            var p = Process.Start(psi);
            while (!p.HasExited)
            {
                //q += p.StandardOutput.ReadToEnd();
                //e += p.StandardError.ReadToEnd();
            }

            //Console.Write(q);



            if (File.Exists(result))
            {
                if (File.Exists(newName))
                    File.Delete(newName);
                File.Move(result, newName);
                beatsFile = newName;
            }
        }
    }
}
