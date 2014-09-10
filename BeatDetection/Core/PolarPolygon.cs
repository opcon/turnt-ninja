using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Substructio.Core.Math;

namespace BeatDetection.Core
{
    class PolarPolygon
    {
        List<PolarPolygonSide> Sides;
        private double[] _angles;
        private double _interiorAngle;
        public double PulseWidth = 0;
        public double PulseWidthMax = 25;
        public double PulseMultiplier = 150;
        private double _width = 50;
        int pulseDirection = 1;
        public bool Pulsing;
        public int MaxNumberOfSides;
        public int NumberOfSides;
        public int Direction { get; set; }
        public double Radius;
        public double ImpactDistance;
        public double Speed;
        public bool Destroy = false;

        public PolarPolygon(int maxNumSides, int numSides, double impactTime, double speed, double startAngle, double minimumDistance = 100)
        {
            MaxNumberOfSides = maxNumSides;
            NumberOfSides = numSides;
            GenerateAngles();
            Sides = new List<PolarPolygonSide>();
            Direction = 1;

            ImpactDistance = minimumDistance;
            Speed = speed;

            GenerateHexagonSides(impactTime, speed, startAngle, minimumDistance);
        }

        public void Update(double time, bool updatePosition = true)
        {
            foreach (var s in Sides)
            {
                s.Direction = Direction;
                s.Update(time, updatePosition);
                Radius = s.Position.Radius;
            }
            if (Radius <= ImpactDistance)
                Destroy = true;
            //else if ((Radius - ImpactDistance) / (Speed) < (PulseWidthMax / PulseMultiplier))
            //    Pulsing = true;
            if (Pulsing)
                Pulse(time);
        }

        public void Rotate(double amount)
        {
            foreach (var s in Sides)
            {
                s.Position.Azimuth += amount;
            }
        }

        public void Draw(double time)
        {
            foreach (var s in Sides)
            {
                s.Draw(time);
            }
        }

        private void GenerateHexagonSides(double impactTime, double speed, double startAngle, double minimumDistance)
        {
            for (int i = 0; i < NumberOfSides; i++)
            {
                Sides.Add(new PolarPolygonSide(impactTime, speed, startAngle + (i * MathHelper.DegreesToRadians(360/MaxNumberOfSides)), minimumDistance) {Length = MathHelper.DegreesToRadians(360/MaxNumberOfSides)});
            }
        }

        public void Pulse(double time)
        {
            Pulsing = true;
            if (PulseWidth >= PulseWidthMax)
                pulseDirection = -1;
            PulseWidth += (pulseDirection) * (PulseMultiplier * time);

            if (PulseWidth <= 0)
            {
                PulseWidth = 0;
                Pulsing = false;
                pulseDirection = 1;
            }

            foreach (var s in Sides)
            {
                s.Width = _width + PulseWidth;
            }
        }

        void GenerateAngles()
        {
            _angles = new double[MaxNumberOfSides];
            _interiorAngle = MathHelper.DegreesToRadians(((double) MaxNumberOfSides - 2)*180)/(MaxNumberOfSides);

            for (int i = 0; i < MaxNumberOfSides; i++)
            {
                _angles[i] = MathHelper.DegreesToRadians((i + 1)*(_interiorAngle));
            }
        }

    }

    class PolarPolygonSide
    {
        public PolarVector Position;
        //public double theta;
        public double Width;
        public double Length;
        //public double r;

        public double ImpactTime;
        public double ImpactDistance;
        public double Speed;
        public PolarVector Velocity;

        public Color4 Colour = Color4.White;

        public PolarPolygonSide(double impactTime, double speed, double startAngle, double minimumDistance = 100)
        {
            //theta = startAngle;
            //Length = MathHelper.DegreesToRadians(60);
            Width =  50;

            var r = (impactTime * speed + minimumDistance + 20);

            Position = new PolarVector(startAngle, r);

            Velocity = new PolarVector(0.5, speed);

            ImpactTime = impactTime;
            ImpactDistance = minimumDistance;
            Direction = 1;
        }

        public int Direction { get; set; }

        public void Update(double time, bool updatePosition = true)
        {
            //var newRadius = updatePosition ? Position.Radius - time*Velocity.Radius : Position.Radius;
            //Position = new PolarVector(Position.Azimuth += time * Velocity.Azimuth * Direction, newRadius);
            Position.Azimuth += time * Velocity.Azimuth * Direction;
            if (updatePosition)
                Position.Radius -= (time * Velocity.Radius);
        }

        public void Draw(double time)
        {
            GL.Begin(PrimitiveType.LineLoop);
            GL.Color4(Colour);

            //GL.Vertex2(new Vector2d(Position.Radius * Math.Cos(Position.Azimuth), Position.Radius * Math.Sin(Position.Azimuth)));
            //GL.Vertex2(new Vector2d(Position.Radius * Math.Cos(Position.Azimuth + Length), Position.Radius * Math.Sin(Position.Azimuth + Length)));
            //GL.Vertex2(new Vector2d((Position.Radius + Width) * Math.Cos(Position.Azimuth + Length), (Position.Radius + Width) * Math.Sin(Position.Azimuth + Length)));
            //GL.Vertex2(new Vector2d((Position.Radius + Width) * Math.Cos(Position.Azimuth), (Position.Radius + Width) * Math.Sin(Position.Azimuth)));

            GL.Vertex2(PolarVector.ToCartesianCoordinates(Position));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth + Length, Position.Radius)));
            GL.Vertex2(
                PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth + Length, Position.Radius + Width)));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth, Position.Radius + Width)));


            GL.End();
        }
    }
}
