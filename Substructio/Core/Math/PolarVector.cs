using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Substructio.Core.Math
{
    class PolarVector
    {
        public float Radius { get; set; }
        public float Azimuth { get; set; }

        public PolarVector(float azimuth, float radius)
        {
            Azimuth = azimuth;
            Radius = radius;
        }

        public PolarVector()
        {
            Azimuth = 0;
            Radius = 0;
        }

        public Vector2 ToCartesianCoordinates()
        {
            return new Vector2((float)(Radius * System.Math.Cos(Azimuth)), (float)(Radius * System.Math.Sin(Azimuth)));
        }
    }
}
