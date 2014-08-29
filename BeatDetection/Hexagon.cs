using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BeatDetection
{
    class Hexagon
    {
        List<HexagonSide> Sides;
        static double[] angles;

        public Hexagon(int numSides, double time, double sp, double startTheta, double distance = 100)
        {
            if (angles == null)
                GenerateAngles();
            Sides = new List<HexagonSide>();

            GenerateHexagonSides(numSides, time, sp, startTheta, distance);
        }

        public void Update(double time, bool updatePosition = true)
        {
            foreach (var s in Sides)
            {
                s.Update(time, updatePosition);
            }
        }

        public void Rotate(double amount)
        {
            foreach (var s in Sides)
            {
                s.theta += amount;
            }
        }

        public void Draw(double time)
        {
            foreach (var s in Sides)
            {
                s.Draw(time);
            }
        }

        private void GenerateHexagonSides(int numSides, double time, double sp, double startTheta, double distance)
        {
            for (int i = 0; i < numSides; i++)
            {
                Sides.Add(new HexagonSide(time, sp, startTheta + i * angles[0], distance));
            }
        }

        static void GenerateAngles()
        {
            angles = new double[6];

            for (int i = 0; i < 6; i++)
            {
                angles[i] = (i + 1) * (60) * (0.0174533);
            }
        }
    }

    class HexagonSide
    {
        public double theta;
        double width;
        double length;
        public double r;

        public double impactTime;
        public double impactDistance;
        public double speed;

        public HexagonSide(double time, double sp, double th, double distance = 100)
        {
            theta = th;
            length = (60) * (0.0174533);
            width =  50;
            speed = sp;

            r = (time * sp + distance);

            impactTime = time;
            impactDistance = distance;
        }

        public void Update(double time, bool updatePosition = true)
        {
            theta += time * 0.5;
            if (updatePosition)
                r -= (time * speed);
        }

        public void Draw(double time)
        {
            GL.Begin(PrimitiveType.LineLoop);

            GL.Vertex2(new Vector2d(r * Math.Cos(theta), r * Math.Sin(theta)));
            GL.Vertex2(new Vector2d(r * Math.Cos(theta + length), r * Math.Sin(theta + length)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta + length), (r + width) * Math.Sin(theta + length)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta), (r + width) * Math.Sin(theta)));


            GL.End();
        }
    }
}
