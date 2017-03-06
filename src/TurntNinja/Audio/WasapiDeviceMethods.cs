using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore.SoundOut;
using CSCore.CoreAudioAPI;

namespace TurntNinja.Audio
{
    public static class WasapiDeviceMethods
    {
        public static MMDevice DefaultDevice
        {
            get
            {
                return MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
        }

        public static MMDeviceCollection GetAvailableDevices()
        {
            return MMDeviceEnumerator.EnumerateDevices(DataFlow.Render, DeviceState.Active);
        }

        public static MMDevice GetDeviceOrDefault(string preferredDevice)
        {
            if (string.IsNullOrEmpty(preferredDevice) || preferredDevice.Equals("Default", StringComparison.CurrentCultureIgnoreCase)) return DefaultDevice;
            
            var mmDevice = GetAvailableDevices().FirstOrDefault(mmd => mmd.FriendlyName.Equals(preferredDevice, StringComparison.CurrentCultureIgnoreCase));
            return mmDevice ?? DefaultDevice;
        }

        public static WasapiOut CreateWithPreferredDevice(string preferredDevice)
        {
            return new WasapiOut { Device = GetDeviceOrDefault(preferredDevice) };
        }
    }
}
