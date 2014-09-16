using System;
using System.IO;

namespace Substructio.Audio
{
    public static class WaveLoader
    {
        public static byte[] GetWaveData(string path, ref WaveInfo waveInfo)
        {
            byte[] returnData;
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                returnData = GetWaveData(new BinaryReader(fs), ref waveInfo);
            }
            return returnData;
        }

        public static int[] WaveDataToInt16(string path, ref WaveInfo waveInfo)
        {
            byte[] data = GetWaveData(path, ref waveInfo);

            return WaveDataToInt16(data, ref waveInfo);
        }

        public static int[] WaveDataToInt16(byte[] data, ref WaveInfo waveInfo)
        {
            int[] returnData = new int[waveInfo.DataSize / 2];

            for (int i = 0; i < waveInfo.DataSize/2; i++)
            {
                returnData[i] = BitConverter.ToInt16(data, i * 2);
            }

            return returnData;
        }

        public static float[] StereoToMono(int[] stereo)
        {
            float[] mono = new float[stereo.Length / 2];
            for (int i = 0; i < mono.Length; i++)
            {
                mono[i] = (float)Math.Sqrt(stereo[(i * 2)] * stereo[(i * 2)] + stereo[(i * 2) + 1] * stereo[(i * 2) + 1]);
            }
            return mono;
        }

        public static byte[] GetWaveData(BinaryReader file, ref WaveInfo waveInfo)
        {
            byte[] returnData;

            //Read the wave file header from the buffer. 

            waveInfo.ChunkID = file.ReadInt32();
            waveInfo.FileSize = file.ReadInt32();
            waveInfo.RiffType = file.ReadInt32();
            waveInfo.FormatID = file.ReadInt32();
            waveInfo.FormatSize = file.ReadInt32();
            waveInfo.FormatCode = file.ReadInt16();
            waveInfo.Channels = file.ReadInt16();
            waveInfo.SampleRate = file.ReadInt32();
            waveInfo.FormatAverageBps = file.ReadInt32();
            waveInfo.FormatBlockAlign = file.ReadInt16();
            waveInfo.BitDepth = file.ReadInt16();

            if (waveInfo.FormatSize == 18)
            {
                // Read any extra values
                waveInfo.FormatExtraSize = file.ReadInt16();
                file.ReadBytes(waveInfo.FormatExtraSize);
            }

            waveInfo.DataID = file.ReadInt32();
            waveInfo.DataSize = file.ReadInt32();


            // Store the audio data of the wave file to a byte array. 

            returnData = file.ReadBytes(waveInfo.DataSize);

            return returnData;
        }
    }

    public struct WaveInfo
    {
        public int ChunkID;
        public int FileSize;
        public int RiffType;
        public int FormatID;
        public int FormatSize;
        public int FormatExtraSize;
        public int FormatCode;
        public int Channels;
        public int SampleRate;
        public int FormatAverageBps;
        public int FormatBlockAlign;
        public int BitDepth;
        public int DataID;
        public int DataSize;
    }
}

