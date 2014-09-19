using System;
using System.Collections.Generic;
using ClipperLib;
using Microsoft.Win32;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using Substructio.Core;
using Substructio.Core.Math;

namespace BeatDetection
{
    class Player
    {
        double length;
        double width;

        private PolarVector _position;
        private PolarVector _velocity;

        private const double _2PI = Math.PI*2;

        public int Hits;

        public double Length
        {
            get { return length; }
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
            length = (10) * (0.0174533);
            width = 20;
            Direction = 1;
        }

        public void Update(double time)
        {
            _currentFramesInput = GetUserInput();
            _position.Azimuth += time*0.5*Direction;
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
            var p2 = PolarVector.ToCartesianCoordinates(_position, length/2, width);
            var p3 = PolarVector.ToCartesianCoordinates(_position, length, 0);

            p.Add(new IntPoint(p1.X, p1.Y));
            p.Add(new IntPoint(p2.X, p2.Y));
            p.Add(new IntPoint(p3.X, p3.Y));

            return p;
        }

        public void Draw(double time)
        {
            GL.Begin(BeginMode.Triangles);
            GL.Vertex2(PolarVector.ToCartesianCoordinates(_position));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(_position, length/2, width));
            GL.Vertex2(PolarVector.ToCartesianCoordinates(_position, length, 0));
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
