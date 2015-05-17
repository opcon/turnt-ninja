using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Substructio.Core;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;

namespace BeatDetection.Core
{
    struct PulseData
    {
        public double PulseWidth;
        public double PulseWidthMax;
        public double PulseMultiplier;
        public bool Pulsing;
        public int PulseDirection;
    }

    internal class PolarPolygon
    {
        public double PulseWidth = 0;
        public double PulseWidthMax = 25;
        public double PulseMultiplier = 150;
        private double _currentPulseMultiplier = 150;
        private double _width = 50;
        private int pulseDirection = 1;
        public bool Pulsing;
        public int NumberOfSides;
        public double ImpactDistance;
        public bool Destroy = false;

        public double OutlineWidth = 4;

        public PolarVector Position;
        public PolarVector Velocity;
        private List<bool> _sides;

        public double AngleBetweenSides { get; private set; }

        public Color4 EvenColour { get; private set; }
        public Color4 EvenOutlineColour { get; private set; }
        public Color4 OddColour { get; private set; }
        public Color4 OddOutlineColour { get; private set; }

        private VertexBuffer _vertexBuffer;
        private VertexArray _vertexArray;
        private BufferDataSpecification _dataSpecification;
        private ShaderProgram _shaderProgram;
        private int _evenCount;
        private int _oddCount;

        public ShaderProgram ShaderProgram
        {
            get { return _shaderProgram; }
            set
            {
                _shaderProgram = value;
                //InitialiseRendering();
            }
        }

        public double OpeningAngle
        {
            get { return _sides.FindIndex(x => x == false)*AngleBetweenSides; }
        }

        public PolarPolygon(List<bool> sides, PolarVector velocity, double width, double minimumRadius, double impactTime)
        {
            InitialisePolygon(sides, velocity, width, minimumRadius, impactTime);
        }

        public PolarPolygon(int numberOfSides, PolarVector velocity, double width, double minimumRadius, double impactTime)
        {
            var sides = Enumerable.Repeat(true, numberOfSides).ToList();
            InitialisePolygon(sides, velocity, width, minimumRadius, impactTime);
        }

        private void InitialisePolygon(List<bool> sides, PolarVector velocity, double width, double minimumRadius, double impactTime)
        {
            Velocity = velocity;
            var initialRadius = (impactTime*Velocity.Radius + minimumRadius + 20);
            ImpactDistance = minimumRadius;
            Position = new PolarVector(0, initialRadius);

            _sides = sides;
            NumberOfSides = _sides.Count;

            EvenColour = OddColour = Color4.White;
            EvenOutlineColour = OddOutlineColour = Color4.Black;

            AngleBetweenSides = GetAngleBetweenSides(NumberOfSides);
            _width = width;

            _evenCount = _oddCount;

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

            if (_vertexArray == null) InitialiseRendering();

            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();
            _vertexBuffer.SetData(BuildVertexList(), _dataSpecification);
            _vertexBuffer.UnBind();
        }

        private IEnumerable<float> BuildVertexList()
        {
            var verts = new Vector2[_vertexBuffer.DrawableIndices];
            int index = 0;
                _evenCount = 0;
                _oddCount = 0;
            for (int i = 0; i < NumberOfSides; i += 2)
            {
                if (_sides[i])
                {
                    var sp = new PolarVector(Position.Azimuth + i*AngleBetweenSides, Position.Radius);
                    verts[index] = PolarVector.ToCartesianCoordinates(sp);
                    verts[index + 1] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, 0);
                    verts[index + 2] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth);
                    verts[index + 3] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth);
                    verts[index + 4] = PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth);
                    verts[index + 5] = PolarVector.ToCartesianCoordinates(sp);
                    _evenCount += 6;
                    index+=6;
                }
            }
            for (int i = 1; i < NumberOfSides; i += 2)
            {
                if (_sides[i])
                {
                    var sp = new PolarVector(Position.Azimuth + i * AngleBetweenSides, Position.Radius);
                    verts[index] = PolarVector.ToCartesianCoordinates(sp);
                    verts[index + 1] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, 0);
                    verts[index + 2] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth);
                    verts[index + 3] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth);
                    verts[index + 4] = PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth);
                    verts[index + 5] = PolarVector.ToCartesianCoordinates(sp);
                    _oddCount +=6;
                    index += 6;
                }
            }

            //generate vertices for outline of even side
            for (int i = 0; i < NumberOfSides; i += 2)
            {
                if (_sides[i])
                {
                    var pInner = new PolarVector(Position.Azimuth + i * AngleBetweenSides, Position.Radius);
                    var pOuter = new PolarVector(Position.Azimuth + i*AngleBetweenSides, Position.Radius + _width + PulseWidth);
                    verts[index] = PolarVector.ToCartesianCoordinates(pOuter);
                    verts[index + 1] = PolarVector.ToCartesianCoordinates(pOuter, AngleBetweenSides, 0);
                    verts[index + 2] = PolarVector.ToCartesianCoordinates(pOuter, AngleBetweenSides, OutlineWidth);
                    verts[index + 3] = PolarVector.ToCartesianCoordinates(pOuter, AngleBetweenSides, OutlineWidth);
                    verts[index + 4] = PolarVector.ToCartesianCoordinates(pOuter, 0, OutlineWidth);
                    verts[index + 5] = PolarVector.ToCartesianCoordinates(pOuter);
                    verts[index + 6] = PolarVector.ToCartesianCoordinates(pInner);
                    verts[index + 7] = PolarVector.ToCartesianCoordinates(pInner, AngleBetweenSides, 0);
                    verts[index + 8] = PolarVector.ToCartesianCoordinates(pInner, AngleBetweenSides, OutlineWidth);
                    verts[index + 9] = PolarVector.ToCartesianCoordinates(pInner, AngleBetweenSides, OutlineWidth);
                    verts[index + 10] = PolarVector.ToCartesianCoordinates(pInner, 0, OutlineWidth);
                    verts[index + 11] = PolarVector.ToCartesianCoordinates(pInner);
                    index+=12;
                }
            }

            //generate vertices for outline of odd side
            for (int i = 1; i < NumberOfSides; i += 2)
            {
                if (_sides[i])
                {
                    var pInner = new PolarVector(Position.Azimuth + i * AngleBetweenSides, Position.Radius);
                    var pOuter = new PolarVector(Position.Azimuth + i*AngleBetweenSides, Position.Radius + _width + PulseWidth);
                    verts[index] = PolarVector.ToCartesianCoordinates(pOuter);
                    verts[index + 1] = PolarVector.ToCartesianCoordinates(pOuter, AngleBetweenSides, 0);
                    verts[index + 2] = PolarVector.ToCartesianCoordinates(pOuter, AngleBetweenSides, OutlineWidth);
                    verts[index + 3] = PolarVector.ToCartesianCoordinates(pOuter, AngleBetweenSides, OutlineWidth);
                    verts[index + 4] = PolarVector.ToCartesianCoordinates(pOuter, 0, OutlineWidth);
                    verts[index + 5] = PolarVector.ToCartesianCoordinates(pOuter);
                    verts[index + 6] = PolarVector.ToCartesianCoordinates(pInner);
                    verts[index + 7] = PolarVector.ToCartesianCoordinates(pInner, AngleBetweenSides, 0);
                    verts[index + 8] = PolarVector.ToCartesianCoordinates(pInner, AngleBetweenSides, OutlineWidth);
                    verts[index + 9] = PolarVector.ToCartesianCoordinates(pInner, AngleBetweenSides, OutlineWidth);
                    verts[index + 10] = PolarVector.ToCartesianCoordinates(pInner, 0, OutlineWidth);
                    verts[index + 11] = PolarVector.ToCartesianCoordinates(pInner);
                    index += 12;
                }
            }

            return verts.SelectMany(v => new[] {v.X, v.Y});
        }

        public void Draw(double time, int even = 0)
        {
            if (_vertexArray == null) InitialiseRendering();
            if (even == 0 || even == 1)
            {
//                if (even == 0) _shaderProgram.SetUniform("in_color", EvenColour);
                _vertexArray.Draw(time, 0, _evenCount);
            }
            if (even == 0 || even == 2)
            {
//                if (even == 0) _shaderProgram.SetUniform("in_color", OddColour);
                _vertexArray.Draw(time, _evenCount, _oddCount);
            }
        }

        public void DrawOutline(double time, int even = 0)
        {
            if (_vertexArray == null)
                InitialiseRendering();
            if (even == 0 || even == 1)
            {
                _vertexArray.Draw(time, _evenCount + _oddCount, _evenCount * 2);
            }
            if (even == 0 || even == 2)
            {
                _vertexArray.Draw(time, (_evenCount + _oddCount) * 2, _oddCount * 2);
            }
        }

        private  void InitialiseRendering()
        {
            _dataSpecification = new BufferDataSpecification
            {
                Count = 2,
                Name = "in_position",
                Offset = 0,
                ShouldBeNormalised = false,
                Stride = 0,
                Type = VertexAttribPointerType.Float,
                SizeInBytes = sizeof(float)
            };

            _vertexArray = new VertexArray {DrawPrimitiveType = PrimitiveType.Triangles};
            _vertexArray.Bind();

            _vertexBuffer = new VertexBuffer
            {
                BufferUsage = BufferUsageHint.StreamDraw,
                DrawableIndices = _sides.Count(b => b)*6*3,
                MaxDrawableIndices = _sides.Count(b => b)*6*3
            };
            _vertexBuffer.AddSpec(_dataSpecification);
            _vertexBuffer.CalculateMaxSize();
            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();

            _vertexArray.Load(_shaderProgram, _vertexBuffer);
            _vertexArray.UnBind();
        }

        public void BeginPulse()
        {
            Pulsing = true;
            pulseDirection = 1;
            _currentPulseMultiplier = PulseMultiplier;
        }

        public void Pulse(double time)
        {
            Pulsing = true;
            if (PulseWidth >= PulseWidthMax)
                pulseDirection = -1;
            PulseWidth += (pulseDirection) * (_currentPulseMultiplier * time);

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

        public void SetColour(Color4 evenColour, Color4 evenOutlineColour, Color4 oddColour, Color4 oddOutlineColour)
        {
            EvenColour = evenColour;
            EvenOutlineColour = evenOutlineColour;
            OddColour = oddColour;
            OddOutlineColour = oddOutlineColour;
        }

        public void SetColour(Color4 evenColour, Color4 evenOutlineColour)
        {
            SetColour(evenColour, evenOutlineColour, evenColour, evenOutlineColour);
        }
    }
}
