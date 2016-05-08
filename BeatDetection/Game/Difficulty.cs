using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace BeatDetection.Game
{
    public class DifficultyOptions
    {
        public float Speed { get; private set; }
        public float VeryCloseDistance{ get; private set; }
        public float CloseDistance { get; private set; }
        public float RotationSpeed { get; private set; }
        public float BeatSkipDistance { get; private set; }
        public float DifficultyMultiplier { get; private set; }

        private const float MinSpeed = 400f;
        private const float MaxSpeed = 2000f;
        private const float SpeedMultiplier = 0.0025f*4;

        private const float MinVeryCloseDistance = 0.05f;
        private const float MaxVeryCloseDistance = 0.4f;
        private const float VeryCloseDistanceMultiplier = 0.4f*0.5f;

        private const float MinCloseDistance = 0.2f;
        private const float MaxCloseDistance = 0.6f;
        private const float CloseDistanceMultiplier = 0.6f*0.5f;

        private const float MinRotationSpeed = 0.0f;
        private const float MaxRotationSpeed = 4.0f;
        private const float RotationSpeedMultiplier = 0.5f*3;

        private const float MinBeatSkipDistance = 0.0f;
        private const float MaxBeatSkipDistance = 1.0f;

        public static DifficultyOptions Easy
        {
            get
            {
                return new DifficultyOptions(500f, 0.32f, 0.5f, 1.0f, 0.25f, 1.0f);
            }
        }

        public static DifficultyOptions Medium
        {
            get
            {
                return new DifficultyOptions(550f, 0.2f, 0.4f, 1.2f, 0.15f, 2.0f); 
            }
        }

        public static DifficultyOptions Hard
        {
            get
            {
                return new DifficultyOptions(600f, 0.15f, 0.35f, 1.5f, 0.0f, 3.0f);
            }
        }

        public static DifficultyOptions Ultra
        {
            get
            {
                return new DifficultyOptions(700f, 0.15f, 0.30f, 1.9f, 0.0f, 4.0f);
            }
        }

        public static DifficultyOptions Ninja
        {
            get
            {
                return new DifficultyOptions(800f, 0.15f, 0.25f, 2.2f, 0.0f, 5.0f);
            }
        }

        private DifficultyOptions(float speed, float vCloseDistance, float closeDistance, float rotationSpeed, float beatSkipDistance, float multiplier)
        {
            Speed = MathHelper.Clamp(speed, MinSpeed, MaxSpeed);
            VeryCloseDistance = MathHelper.Clamp(vCloseDistance, MinVeryCloseDistance, MaxVeryCloseDistance);
            CloseDistance = MathHelper.Clamp(closeDistance, MinCloseDistance, MaxCloseDistance);
            RotationSpeed = MathHelper.Clamp(rotationSpeed, MinRotationSpeed, MaxRotationSpeed);
            BeatSkipDistance = MathHelper.Clamp(beatSkipDistance, MinBeatSkipDistance, MaxBeatSkipDistance);
            DifficultyMultiplier = multiplier;
        }

        public float GetScoreMultiplier()
        {
            return DifficultyMultiplier;
            //return (SpeedMultiplier*Speed)
            //    + (VeryCloseDistanceMultiplier*(1/VeryCloseDistance))
            //    + (CloseDistanceMultiplier*(1/CloseDistance))
            //    + (RotationSpeedMultiplier*RotationSpeed);
        }
    }

    enum DifficultyLevels
    {
        Easy,
        Medium,
        Hard,
        Ultra,
        Ninja
    }
}
