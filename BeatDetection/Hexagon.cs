using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace BeatDetection
{
    class Hexagon
    {
        public double theta;
        double width;
        double length;
        public double r;

        public double impactTime;
        public double impactDistance;
        public double speed;

        public Hexagon(double time, double sp, double distance = 100)
        {
            theta = 0;
            length = 1.57;
            width =  50;
            speed = sp;

            r = (time * sp + distance - 10);

            impactTime = time;
            impactDistance = distance;
        }

        public void Update(double time)
        {
            theta += time;
            r -= (time * speed);
        }

        public void Draw(double time)
        {
            GL.Begin(PrimitiveType.LineLoop);

            GL.Vertex2(new Vector2d(r * Math.Cos(theta), r * Math.Sin(theta)));
            GL.Vertex2(new Vector2d(r * Math.Cos(theta + length), r * Math.Sin(theta + length)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta + length), (r + width) * Math.Sin(theta + length)));
            GL.Vertex2(new Vector2d((r + width) * Math.Cos(theta), (r + width) * Math.Sin(theta)));


            GL.End();
        }
    }
}
