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
    internal class PolarPolygon
    {
        public double PulseWidth = 0;
        public double PulseWidthMax = 25;
        public double PulseMultiplier = 150;
        private double _width = 50;
        private int pulseDirection = 1;
        public bool Pulsing;
        public int NumberOfSides;
        public int Direction { get; set; }
        public double ImpactDistance;
        public bool Destroy = false;

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
            Direction = 1;

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
                    verts[index + 3] = PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth);
                    _evenCount += 4;
                    index+=4;
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
                    verts[index + 3] = PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth);
                    _oddCount +=4;
                    index += 4;
                }
            }
            return verts.SelectMany(v => new[] {v.X, v.Y});
        }

        

        public void Draw(double time)
        {
            _shaderProgram.SetUniform("in_color", EvenColour);
            _vertexArray.Draw(time, 0, _evenCount);
            _shaderProgram.SetUniform("in_color", OddColour);
            _vertexArray.Draw(time, _evenCount, _oddCount);
            //GL.Begin(PrimitiveType.Quads);
            //GL.Color4(EvenColour);
            //for (int i = 0; i < NumberOfSides; i+= 2)
            //{
            //    if (_sides[i]) DrawPolygonSide(i);
            //}
            //GL.Color4(OddColour);
            //for (int i = 1; i < NumberOfSides; i += 2)
            //{
            //    if (_sides[i]) DrawPolygonSide(i);
            //}
            //GL.End();

            //GL.LineWidth(3);
            //GL.Begin(PrimitiveType.Lines);
            //GL.Color4(EvenOutlineColour);
            //for (int i = 0; i < NumberOfSides; i+= 2)
            //{
            //    if (_sides[i]) DrawPolygonSide(i);
            //}
            //GL.Color4(OddOutlineColour);
            //for (int i = 1; i < NumberOfSides; i += 2)
            //{
            //    if (_sides[i]) DrawPolygonSide(i);
            //}
            //GL.End();
        }

        //private void DrawPolygonSide(int index)
        //{
        //    var sp = new PolarVector(Position.Azimuth + index*AngleBetweenSides, Position.Radius);
        //    GL.Vertex2(PolarVector.ToCartesianCoordinates(sp));
        //    GL.Vertex2(PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, 0));
        //    GL.Vertex2(PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth));
        //    GL.Vertex2(PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth));
        //}

        private void InitialiseRendering()
        {
            _dataSpecification = new BufferDataSpecification
            {
                Count = 2,
                Name = "in_position",
                Offset = 0,
                ShouldBeNormalised = false,
                Stride = 0,
                Type = VertexAttribPointerType.Float
            };

            _vertexArray = new VertexArray {DrawPrimitiveType = PrimitiveType.Quads};
            _vertexArray.Bind();

            _vertexBuffer = new VertexBuffer
            {
                BufferUsage = BufferUsageHint.StreamDraw,
                DrawableIndices = _sides.Count(b => b)*4
            };
            _vertexBuffer.AddSpec(_dataSpecification);
            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();

            _vertexArray.Load(_shaderProgram, _vertexBuffer);
            _vertexArray.UnBind();
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

        public IEnumerable<float> GetVertices()
        {
            var temp = new List<Vector2>();
            for (int i = 0; i < NumberOfSides; i += 1)
            {
                if (!_sides[i]) continue;
                var sp = new PolarVector(Position.Azimuth + i * AngleBetweenSides, Position.Radius);
                temp.Add(PolarVector.ToCartesianCoordinates(sp));
                temp.Add(PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, 0));
                temp.Add(PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides, _width + PulseWidth));
                temp.Add(PolarVector.ToCartesianCoordinates(sp, 0, _width + PulseWidth));
            }
            var ret = temp.SelectMany(v => new[] { v.X, v.Y});
            //var ret = temp.SelectMany(v => new[] {v.X, v.Y, 0.0f});
            return ret;
        }
    }
}
