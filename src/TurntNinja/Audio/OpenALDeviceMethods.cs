using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore.SoundOut;
using CSCore.SoundOut.AL;

namespace TurntNinja.Audio
{
    public static class OpenALDeviceMethods
    {
        public static ALDevice DefaultDevice
        {
            get
            {
                return ALDevice.DefaultDevice;
            }
        }

        public static ALDevice[] GetAvailableDevices()
        {
            return ALDevice.EnumerateALDevices();
        }

        public static ALDevice GetDeviceOrDefault(string preferredDevice)
        {
            if (string.IsNullOrEmpty(preferredDevice) || preferredDevice.Equals("Default", StringComparison.CurrentCultureIgnoreCase)) return DefaultDevice;

            var alDevice = GetAvailableDevices().FirstOrDefault(ald => ald.Name.Equals(preferredDevice, StringComparison.CurrentCultureIgnoreCase));
            return alDevice ?? DefaultDevice;
        }

        public static ALSoundOut CreateWithPreferredDevice(string preferredDevice)
        {
            return new ALSoundOut { Device = GetDeviceOrDefault(preferredDevice) };
        }
    }
}
