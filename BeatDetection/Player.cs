using System;
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

        float score;

        public Player()
        {
            theta = 0;
            dtheta = 7;
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

        public void Draw(double time)
        {
            GL.Begin(BeginMode.LineLoop);
            GL.Vertex2(new Vector2d((r) * Math.Cos(theta), (r) * Math.Sin(theta)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta + length / 2), (r + width) * Math.Sin(theta + length / 2)));
            GL.Vertex2(new Vector2d((r) * Math.Cos(theta + length), (r) * Math.Sin(theta + length)));
            GL.End();
        }
    }
}
