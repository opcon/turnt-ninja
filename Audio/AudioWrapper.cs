namespace Substructio.Audio
{
    public class AudioWrapper
    {
        public string AudioFile { get; set; }
        public byte[] AudioBuffer { get; set; }
        public int[] AudioData { get; set; }
        public float[] MonoData { get; set; }
        public WaveInfo AudioInfo;

        public AudioWrapper(string audioFile)
        {
            AudioFile = audioFile;
            AudioInfo = new WaveInfo();

            LoadAudioFile();
        }

        void LoadAudioFile()
        {
            AudioBuffer = WaveLoader.GetWaveData(AudioFile, ref AudioInfo);
            AudioData = WaveLoader.WaveDataToInt16(AudioBuffer, ref AudioInfo);
            MonoData = WaveLoader.StereoToMono(AudioData);
        }

    }
}

