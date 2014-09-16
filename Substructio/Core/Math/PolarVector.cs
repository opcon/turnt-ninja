using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace Substructio.Core.Math
{
    public class PolarVector
    {
        public double Radius { get; set; }
        public double Azimuth { get; set; }

        public PolarVector(double azimuth, double radius)
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
            //return new Vector2((float)(Radius * System.Math.Cos(Azimuth)), (float)(Radius * System.Math.Sin(Azimuth)));
            return PolarVector.ToCartesianCoordinates(this);
        }

        public static Vector2 ToCartesianCoordinates(PolarVector polarVector)
        {
            return new Vector2((float)(polarVector.Radius * System.Math.Cos(polarVector.Azimuth)), (float)(polarVector.Radius * System.Math.Sin(polarVector.Azimuth)));
        }
    }
}
