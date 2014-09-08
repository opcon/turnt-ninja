using System.Collections.Generic;
using BeatDetection.Core;

namespace BeatDetection.Game
{
    class Stage
    {
        private double _TotalTime;
        private float[] _Beats;
        private List<PolarPolygon> _Polygons; 

        public Stage()
        {

        }

        public void Update(double time)
        {
            _TotalTime += time;
        }

        public void Draw(double time)
        {
            
        }
    }
}
