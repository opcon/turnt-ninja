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
        public float Speed;
        public float VeryCloseDistance;
        public float CloseDistance;
        public float RotationSpeed;

        private const float MinSpeed = 400f;
        private const float MaxSpeed = 2000f;
        private const float SpeedMultiplier = 0.0025f*4;

        private const float MinVeryCloseDistance = 0.05f;
        private const float MaxVeryCloseDistance = 0.4f;
        private const float VeryCloseDistanceMultiplier = 0.4f*0.5f;

        private const float MinCloseDistance = 0.2f;
        private const float MaxCloseDistance = 0.6f;
        private const float CloseDistanceMultiplier = 0.6f*0.5f;

        private const float MinRotationSpeed = 0.5f;
        private const float MaxRotationSpeed = 4.0f;
        private const float RotationSpeedMultiplier = 0.5f*3;

        public DifficultyOptions()
        {
            Speed = 600f;
            VeryCloseDistance = 0.2f;
            CloseDistance = 0.4f;
            RotationSpeed = 1.0f;
        }

        public DifficultyOptions(float speed, float vCloseDistance, float closeDistance, float rotationSpeed)
        {
            Speed = MathHelper.Clamp(speed, MinSpeed, MaxSpeed);
            VeryCloseDistance = MathHelper.Clamp(vCloseDistance, MinVeryCloseDistance, MaxVeryCloseDistance);
            CloseDistance = MathHelper.Clamp(closeDistance, MinCloseDistance, MaxCloseDistance);
            RotationSpeed = MathHelper.Clamp(rotationSpeed, MinRotationSpeed, MaxRotationSpeed);
        }

        public float GetScoreMultiplier()
        {
            return (SpeedMultiplier*Speed)
                + (VeryCloseDistanceMultiplier*(1/VeryCloseDistance))
                + (CloseDistanceMultiplier*(1/CloseDistance))
                + (RotationSpeedMultiplier*RotationSpeed);
        }
    }
}
