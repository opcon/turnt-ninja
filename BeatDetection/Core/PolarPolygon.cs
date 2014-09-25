using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Substructio.Core;
using Substructio.Core.Math;

namespace BeatDetection.Core
{
    class PolarPolygon
    {
        public double PulseWidth = 0;
        public double PulseWidthMax = 25;
        public double PulseMultiplier = 150;
        private double _width = 50;
        int pulseDirection = 1;
        public bool Pulsing;
        public int NumberOfSides;
        public int Direction { get; set; }
        public double ImpactDistance;
        public bool Destroy = false;

        public PolarVector Position;
        public PolarVector Velocity;
        private List<bool> _sides;

        private Color4 _colour;

        private Color4 _outlineColour;
        public double AngleBetweenSides { get; private set; }

        public Color4 Colour
        {
            get {return _colour;}
            private set
            {
                _colour = value;
            }
        }

        public Color4 OutlineColour
        {
            get{ return _outlineColour; }
            private set 
            {
                _outlineColour = value;
            }
        }

        public double OpeningAngle
        {
            get { return _sides.FindIndex(x => x == false)*AngleBetweenSides; }
        }

        public PolarPolygon(List<bool> sides, PolarVector velocity, double width, double minimumRadius, double impactTime)
        {
            Velocity = velocity;
            var initialRadius = (impactTime * Velocity.Radius + minimumRadius + 20);
            ImpactDistance = minimumRadius;
            Position = new PolarVector(0, initialRadius);
            Direction = 1;

            _sides = sides;
            NumberOfSides = _sides.Count;

            Colour = Color4.White;
            OutlineColour = Color4.Black;

            AngleBetweenSides = GetAngleBetweenSides(NumberOfSides);
            _width = width;
        }

        public static double GetAngleBetweenSides(int numberOfSides)
        {
            return MathHelper.DegreesToRadians(360.0/numberOfSides);
        }

        public void Update(double time, bool updateRadius)
        {
            if (updateRadius) Position.Radius -= (time * Velocity.Radius);
            if (Position.Radius <= ImpactDistance)
                Destroy = true;
            if (Pulsing) Pulse(time);
        }


        public void Draw(double time)
        {
            GL.Begin(PrimitiveType.Quads);
            GL.Color4(Colour);
            for (int i = 0; i < NumberOfSides; i++)
            {
                if (_sides[i]) DrawPolygonSide(i);
            }
            GL.End();

            GL.LineWidth(3);
            GL.Begin(PrimitiveType.Lines);
            GL.Color4(OutlineColour);
            for (int i = 0; i < NumberOfSides; i++)
            {
                if (_sides[i]) DrawPolygonSide(i);
            }
            GL.End();
        }

        private void DrawPolygonSide(int index)
        {
            var sp = new PolarVector(Position.Azimuth + index*AngleBetweenSides, Position.Radius);
            GL.Vertex2(PolarVector.ToCartesianCoordinates(sp));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, 0));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth));
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
        }

        public List<List<IntPoint>> GetPolygonBounds()
        {
            var polys = new List<List<IntPoint>>();
            for (int i = 0; i < NumberOfSides; i++)
            {
                if (_sides[i]) polys.Add(GetSideBounds(i));
            }
            return polys;
        }

        private List<IntPoint> GetSideBounds(int index)
        {
            var p = new List<IntPoint>();
            var sp = new PolarVector(Position.Azimuth + index*AngleBetweenSides, Position.Radius);
            var p1 = PolarVector.ToCartesianCoordinates(sp);
            var p2 = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, 0);
            var p3 = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth);
            var p4 = PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth);

            p.Add(new IntPoint(p1.X, p1.Y));
            p.Add(new IntPoint(p2.X, p2.Y));
            p.Add(new IntPoint(p3.X, p3.Y));
            p.Add(new IntPoint(p4.X, p4.Y));

            return p;
        }

        public void SetColour(Color4 colour, Color4 outlineColour)
        {
            Colour = colour;
            OutlineColour = outlineColour;
        }
    }
}
