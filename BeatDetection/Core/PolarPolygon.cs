using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using ColorMine.ColorSpaces;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Substructio.Core;
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
        public double Azimuth;
        public double ImpactDistance;
        public double Speed;
        public bool Destroy = false;

        private Color4 _colour;
        public Color4 Colour
        {
            get {return _colour;}
            set
            {
                _colour = value;
                if (Sides != null && Sides.Count > 0)
                {
                    foreach (var s in Sides)
                    {
                        s.Colour = _colour;
                    }
                }
            }
        }

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
            Colour = Color4.White;
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
            Azimuth = Sides.First().Position.Azimuth;
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

        public List<List<IntPoint>> GetPolygonBounds()
        {
            var polys = new List<List<IntPoint>>();
            foreach (var s in Sides)
            {
                polys.Add(s.GetPoints());
            }
            return polys;
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

        private Color4 _colour = Color4.White;
        private Color4 _outlineColour = Color4.White;

        public Color4 Colour
        {
            get { return _colour; }
            set
            {
                _colour = value;
                var hsl = Utilities.Color4ToColorSpace(_colour).To<Hsl>();
                hsl.L += 10;
                hsl.S += 20;
                _outlineColour = Utilities.ColorSpaceToColor4(hsl);
            }
        }

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
            GL.Begin(PrimitiveType.Quads);
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

            GL.LineWidth(3);
            GL.Begin(PrimitiveType.LineLoop);
            GL.Color4(_outlineColour);
            GL.Vertex2(PolarVector.ToCartesianCoordinates(Position));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth + Length, Position.Radius)));
            GL.Vertex2(
                PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth + Length, Position.Radius + Width)));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth, Position.Radius + Width)));

            GL.End();
        }

        public List<IntPoint> GetPoints()
        {
            var p = new List<IntPoint>();
            var p1 = PolarVector.ToCartesianCoordinates(Position);
            var p2 = PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth + Length, Position.Radius));
            var p3 =
                PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth + Length, Position.Radius + Width));
            var p4 = PolarVector.ToCartesianCoordinates(new PolarVector(Position.Azimuth, Position.Radius + Width));

            p.Add(new IntPoint(p1.X, p1.Y));
            p.Add(new IntPoint(p2.X, p2.Y));
            p.Add(new IntPoint(p3.X, p3.Y));
            p.Add(new IntPoint(p4.X, p4.Y));

            return p;
        }
    }
}
