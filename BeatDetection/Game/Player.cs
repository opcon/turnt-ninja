using System;
using System.Collections.Generic;
using ClipperLib;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics.OpenGL;
using Substructio.Core;

namespace BeatDetection
{
    class Player
    {
        double theta;
        double dtheta;
        double length;
        double width;
        double r;

        public int Hits;

        public Player()
        {
            theta = 0;
            dtheta = 9;
            length = (10) * (0.0174533);
            width = 20;
            r = 180;
            Direction = 1;
        }

        public int Direction { get; set; }

        public void Update(double time)
        {
            theta += time * 0.5 * Direction;
            if (InputSystem.CurrentKeys.Contains(Key.Left))
            {
                theta += dtheta * time;
            }
            else if (InputSystem.CurrentKeys.Contains(Key.Right))
            {
                theta -= dtheta * time*1;
            }
        }

        public List<IntPoint> GetBounds()
        {
            var p = new List<IntPoint>();
            var p1 = new Vector2d((r)*Math.Cos(theta), (r)*Math.Sin(theta));
            var p2 = new Vector2d((r + width)*Math.Cos(theta + length/2), (r + width)*Math.Sin(theta + length/2));
            var p3 = new Vector2d((r)*Math.Cos(theta + length), (r)*Math.Sin(theta + length));

            p.Add(new IntPoint(p1.X, p1.Y));
            p.Add(new IntPoint(p2.X, p2.Y));
            p.Add(new IntPoint(p3.X, p3.Y));

            return p;
        }

        public void Draw(double time)
        {
            GL.Begin(BeginMode.Triangles);
            GL.Vertex2(new Vector2d((r) * Math.Cos(theta), (r) * Math.Sin(theta)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta + length / 2), (r + width) * Math.Sin(theta + length / 2)));
            GL.Vertex2(new Vector2d((r) * Math.Cos(theta + length), (r) * Math.Sin(theta + length)));
            GL.End();
        }
    }
}
