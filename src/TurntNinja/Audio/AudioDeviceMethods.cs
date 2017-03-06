using Substructio.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurntNinja.Audio
{
    public static class AudioDeviceMethods
    {
        public static List<string> GetAudioDeviceNames()
        {
            List<string> ret = new List<string>();
            switch (PlatformDetection.RunningPlatform())
            {
                case Platform.Windows:
                    ret.AddRange(WasapiDeviceMethods.GetAvailableDevices().Select(mmd => mmd.FriendlyName));
                    break;
                case Platform.Linux:
                case Platform.MacOSX:
                    ret.AddRange(OpenALDeviceMethods.GetAvailableDevices().Select(ald => ald.Name));
                    break;
            }
            return ret;
        }

        public static string GetAudioDeviceNameOrDefault(string preferredDevice)
        {
            if (preferredDevice.Equals("Default", StringComparison.CurrentCultureIgnoreCase)) return "Default";
            switch (PlatformDetection.RunningPlatform())
            {
                case Platform.Windows:
                    return WasapiDeviceMethods.GetDeviceOrDefault(preferredDevice).FriendlyName;
                case Platform.Linux:
                case Platform.MacOSX:
                    return OpenALDeviceMethods.GetDeviceOrDefault(preferredDevice).Name;
            }
            return "";
        }
    }
}
