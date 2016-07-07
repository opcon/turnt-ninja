using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using Substructio.Core;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;

namespace BeatDetection
{
    class Player
    {
        private double _length;
        private double _width;

        private PolarVector _position;
        private PolarVector _velocity;

        public int Hits;
        public Color4 Colour = Color4.White;

        private VertexBuffer _vertexBuffer;
        private VertexArray _vertexArray;
        private BufferDataSpecification _dataSpecification;

        public bool UseGamePad { get; set; }

        public ShaderProgram ShaderProgram
        {
            get { return _shaderProgram; }
            set
            {
                _shaderProgram = value;
                CreateBuffers();
            }
        }

        public double Length
        {
            get { return _length; }
        }

        public double Width
        {
            get { return _width; }
        }

        public PolarVector Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public int Direction { get; set; }

        public PolarVector Velocity
        {
            get { return _velocity; }
        }

        public float Score;

        private Input _currentFramesInput;
        private ShaderProgram _shaderProgram;

        public Player()
        {
            _position = new PolarVector(0, 180);
            _velocity = new PolarVector(-9, 0);
            _length = (10) * (0.0174533);
            _width = 20;
            Direction = 1;
            UseGamePad = false;
        }

        public void Update(double time, bool AI = false)
        {
            if (!AI) _currentFramesInput = GetUserInput();
           // _position.Azimuth += time*0.5*Direction;
            if (_currentFramesInput.HasFlag(Input.Left))
            {
                _position.Azimuth -= _velocity.Azimuth*time;
            }
            else if (_currentFramesInput.HasFlag(Input.Right))
            {
                _position.Azimuth += _velocity.Azimuth*time;
            }
            _position = _position.Normalised();

            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();
            _vertexBuffer.SetData(BuildVertexList(), _dataSpecification);
            _vertexBuffer.UnBind();
        }

        private void CreateBuffers()
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

            _vertexArray = new VertexArray{DrawPrimitiveType = PrimitiveType.Triangles};
            _vertexArray.Bind();

            var size = 3*2*sizeof (float);
            _vertexBuffer = new VertexBuffer
            {
                BufferUsage = BufferUsageHint.StreamDraw,
                DrawableIndices = 3,
                MaxSize = size
            };
            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();
            _vertexBuffer.DataSpecifications.Add(_dataSpecification);

            _vertexArray.Load(_shaderProgram, _vertexBuffer);
            _vertexArray.UnBind();
        }

        public void Reset()
        {
            Score = 0;
            Hits = 0;
        }

        public List<IntPoint> GetBounds()
        {
            var p = new List<IntPoint>();
            var p1 = PolarVector.ToCartesianCoordinates(_position);
            var p2 = PolarVector.ToCartesianCoordinates(_position, _length/2, _width);
            var p3 = PolarVector.ToCartesianCoordinates(_position, _length, 0);

            p.Add(new IntPoint(p1.X, p1.Y));
            p.Add(new IntPoint(p2.X, p2.Y));
            p.Add(new IntPoint(p3.X, p3.Y));

            return p;
        }

        public List<float> BuildVertexList()
        {
            var verts = new List<Vector2>();
            verts.Add(PolarVector.ToCartesianCoordinates(_position));
            verts.Add(PolarVector.ToCartesianCoordinates(_position, _length/2, _width));
            verts.Add(PolarVector.ToCartesianCoordinates(_position, _length, 0));
            return verts.SelectMany(v => new[] {v.X, v.Y}).ToList();
        }

        public void Draw(double time)
        {
            _vertexArray.Draw(time);
        }

        private Input GetUserInput()
        {
            Input i = Input.Default;
            //if (GamePad.GetCapabilities(0).IsConnected)
            //{
            //    if (OpenTK.Input.GamePad.GetState(0).Buttons.LeftShoulder == ButtonState.Pressed || GamePad.GetState(0).Triggers.Left > 0.3)
            //        i |= Input.Left;
            //    if (GamePad.GetState(0).Buttons.RightShoulder == ButtonState.Pressed || GamePad.GetState(0).Triggers.Right > 0.3)
            //        i |= Input.Right;
            //}
            if (InputSystem.CurrentKeys.Contains(Key.Left))
                i |= Input.Left;
            if (InputSystem.CurrentKeys.Contains(Key.Right))
                i |= Input.Right;
            if (InputSystem.CurrentKeys.Contains(Key.Up))
                i |= Input.Up;
            if (InputSystem.CurrentKeys.Contains(Key.Down))
                i |= Input.Down;
            return i;
        }

        public void DoAI(double targetAzimuth)
        {
            _currentFramesInput = DirectionToTurn(_position.Azimuth, targetAzimuth);
        }

        private Input DirectionToTurn(double current, double target)
        {
            current = MathUtilities.Normalise(current, 0, MathUtilities.TwoPI);
            target = MathUtilities.Normalise(target, 0, MathUtilities.TwoPI);

            var diff = Math.Abs(current - target);
            if (diff < 0.1)
                return Input.Default;
            int flip = 1;
            if (diff > Math.PI)
                flip *= -1;
            int d = current > target ? -flip : flip;
            return d > 0 ? Input.Left : Input.Right;
        }
    }

    [Flags]
    public enum Input
    {
        Default = 0x0,
        Left = 0x1,
        Right = 0x2,
        Up = 0x4,
        Down = 0x8
    }

}
