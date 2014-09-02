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
        string audioFile;
        string sonicPath;
        private string pluginsPath;
        string rdfDescriptor;
        string pluginFileSuffix;
        float correctionAmount = 0;

        string beatsFile;
        public QMVampWrapper(AudioWrapper a, string aFile, string sPath, string pPath, float correction, string rdf = "../../Processed Songs/qmonset.n3", string suffix = "vamp_qm-vamp-plugins_qm-onsetdetector_onsets") : base(a)
        {
            audioFile = aFile;
            sonicPath = sPath;
            correctionAmount = correction;
            rdfDescriptor = rdf;
            pluginFileSuffix = suffix;
            this.pluginsPath = pPath;
        }

        public override void DetectBeats()
        {
            CallSonicAnnotator();
            using (StreamReader sr = new StreamReader(beatsFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Beats.Add(float.Parse(line.Split(',')[0]) + correctionAmount);
                }
                sr.Close();
            }
        }

        void CallSonicAnnotator()
        {

            ProcessStartInfo psi = new ProcessStartInfo(sonicPath);
            psi.WorkingDirectory = Game.AssemblyDirectory;
            psi.EnvironmentVariables.Add("VAMP_PATH", pluginsPath);
            var csvDir = "../../Processed Songs/";
            var arguments = String.Format("-t \"{0}\" \"{1}\" -w csv --csv-force --csv-basedir \"{2}\"", rdfDescriptor, audioFile, csvDir.Replace(@"\", "/"));
            psi.Arguments = arguments;
            //psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            //psi.RedirectStandardOutput = true;
            //psi.RedirectStandardError = true;

            var result = Path.Combine(csvDir, String.Format("{0}_{1}", Path.GetFileNameWithoutExtension(audioFile), pluginFileSuffix) + ".csv");
            var newName = Path.Combine(Path.GetDirectoryName(result), Path.GetFileNameWithoutExtension(audioFile) + ".csv");

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
