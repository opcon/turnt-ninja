using System;
using System.Collections.Generic;
using ClipperLib;
using Microsoft.Win32;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL4;
using Substructio.Core;
using Substructio.Core.Math;

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

        public double Length
        {
            get { return _length; }
        }

        public PolarVector Position
        {
            get { return _position; }
        }

        public int Direction { get; set; }

        public PolarVector Velocity
        {
            get { return _velocity; }
        }

        private Input _currentFramesInput;

        public Player()
        {
            _position = new PolarVector(0, 180);
            _velocity = new PolarVector(-9, 0);
            _length = (10) * (0.0174533);
            _width = 20;
            Direction = 1;
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

        public void Draw(double time)
        {
            GL.Begin(BeginMode.Triangles);
            GL.Color4(Colour);
            GL.Vertex2(PolarVector.ToCartesianCoordinates(_position));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(_position, _length/2, _width));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(_position, _length, 0));
            GL.End();
        }

        private Input GetUserInput()
        {
            Input i = Input.Default;
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
