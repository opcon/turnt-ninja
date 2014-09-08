using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BeatDetection.Core
{
    class PolarPolygon
    {
        List<PolygonSide> Sides;
        static double[] angles;
        public double pulseWidth = 0;
        public double pulseWidthMax = 25;
        public double pulseMultiplier = 150;
        double width = 50;
        int pulseDirection = 1;
        public bool pulsing = false;

        public PolarPolygon(int numSides, double time, double sp, double startTheta, double distance = 100)
        {
            if (angles == null)
                GenerateAngles();
            Sides = new List<PolygonSide>();
            Direction = 1;

            GenerateHexagonSides(numSides, time, sp, startTheta, distance);
        }

        public int Direction { get; set; }

        public void Update(double time, bool updatePosition = true)
        {
            foreach (var s in Sides)
            {
                s.Direction = Direction;
                s.Update(time, updatePosition);
            }
            if (pulsing == true)
                Pulse(time);
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
                Sides.Add(new PolygonSide(time, sp, startTheta + i * angles[0], distance));
            }
        }

        public void Pulse(double time)
        {
            pulsing = true;
            if (pulseWidth >= pulseWidthMax)
                pulseDirection = -1;
            pulseWidth += (pulseDirection) * (pulseMultiplier * time);

            if (pulseWidth <= 0)
            {
                pulseWidth = 0;
                pulsing = false;
                pulseDirection = 1;
            }

            foreach (var s in Sides)
            {
                s.width = width + pulseWidth;
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

    class PolygonSide
    {
        public double theta;
        public double width;
        double length;
        public double r;

        public double impactTime;
        public double impactDistance;
        public double speed;

        public Color4 colour = Color4.White;

        public PolygonSide(double time, double sp, double th, double distance = 100)
        {
            theta = th;
            length = (60) * (0.0174533);
            width =  50;
            speed = sp;

            r = (time * sp + distance + 20);

            impactTime = time;
            impactDistance = distance;
            Direction = 1;
        }

        public int Direction { get; set; }

        public void Update(double time, bool updatePosition = true)
        {
            theta += time * 0.5 * Direction;
            if (updatePosition)
                r -= (time * speed);
        }

        public void Draw(double time)
        {
            GL.Begin(PrimitiveType.LineLoop);
            GL.Color4(colour);

            GL.Vertex2(new Vector2d(r * Math.Cos(theta), r * Math.Sin(theta)));
            GL.Vertex2(new Vector2d(r * Math.Cos(theta + length), r * Math.Sin(theta + length)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta + length), (r + width) * Math.Sin(theta + length)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta), (r + width) * Math.Sin(theta)));


            GL.End();
        }
    }
}
