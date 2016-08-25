using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TurntNinja.Core;

namespace TurntNinja.Game
{
    class OnsetCollection
    {
        /// <summary>
        /// Collection of onset times
        /// </summary>
        public float[] OnsetTimes { get; private set; }

        /// <summary>
        /// Collection of beat frequencies
        /// </summary>
        public float[] BeatFrequencies { get; private set; }

        public float MaxBeatFrequency;
        public float MinBeatFrequency;

        /// <summary>
        /// Number of onsets
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Collection of pulse data
        /// </summary>
        public PulseData[] PulseDataCollection { get; private set; }

        /// <summary>
        /// How many onsets have been reached this frame
        /// </summary>
        public int OnsetsReached { get; private set; }

        /// <summary>
        /// Whether we need to start pulsing because we are near an onset
        /// </summary>
        public bool BeginPulsing { get; private set; }

        /// <summary>
        /// The index of the current onset
        /// </summary>
        public int OnsetIndex { get; private set; } = 0;

        /// <summary>
        /// Whether we are pulsing at the moment
        /// </summary>
        public bool Pulsing { get; private set; }

        /// <summary>
        /// The total elapsed game time for this <see cref="OnsetCollection"/>
        /// </summary>
        public double ElapsedGameTime { get; private set; }

        private double _onsetTimeBuffer = 0.0f;

        public OnsetCollection(int onsetCount)
        {
            Count = onsetCount;
            OnsetTimes = new float[Count];
            PulseDataCollection = new PulseData[Count];
        }

        public void AddOnsets(float[] onsetTimes, float[] beatFrequencies)
        {
            OnsetTimes = onsetTimes;
            BeatFrequencies = beatFrequencies;
            MaxBeatFrequency = BeatFrequencies.Max();
            MinBeatFrequency = BeatFrequencies.Min();
            for (int i = 0; i < Count; i++)
            {
                PulseDataCollection[i] = new PulseData {
                    PulseDirection = 1,
                    PulseMultiplier = Math.Pow(BeatFrequencies[i] * 60, 1) + 70,
                    PulseWidth = 0,
                    PulseWidthMax = 25,
                    Pulsing = false };
            }
        }

        public void Update(double time)
        {
            ElapsedGameTime += time;

            OnsetIndex += OnsetsReached;
            if (BeginPulsing)
            {
                BeginPulsing = false;
                Pulsing = true;
            }
            if (OnsetsReached > 0)
            {
                BeginPulsing = false;
                Pulsing = false;
            }
            OnsetsReached = 0;
            BeginPulsing = false;

            for (int i = OnsetIndex; i < Count; i++)
            {
                //Check for any 'hit' beats
                if (OnsetTimes[i] <= ElapsedGameTime + _onsetTimeBuffer) OnsetsReached++;
                //Check if need to pulse center polygon
                else if (!Pulsing && CloseToNextOnset(i, PulseDataCollection[i].PulseWidthMax / PulseDataCollection[i].PulseMultiplier))
                    BeginPulsing = true;
            }
        }

        public bool CloseToNextOnset(int onsetIndex, double delta)
        {
            return (OnsetTimes[onsetIndex] - ElapsedGameTime) < delta;
        }

        public void Initialise()
        {
            OnsetIndex = 0;
        }

    }
}
