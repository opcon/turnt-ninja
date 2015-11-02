using System;
using System.Collections.Generic;
using System.Collections;
using OpenTK.Graphics;

namespace BeatDetection.Core
{
    public struct HUSLColor
    {
        public double H;
        public double S;
        public double L;

        public HUSLColor (double h, double s, double l)
        {
            H = h;
            S = s;
            L = l;
        }

        public HUSLColor(Color4 color)
        {
            var res = HUSL.ColorConverter.RGBToHUSL(new List<double>{color.R, color.G, color.B});
            H = res[0];
            S = res[1];
            L = res[2];
        }

        public static HUSLColor FromColor4(Color4 color)
        {
            return new HUSLColor(color);
        }

        public static Color4 ToColor4(HUSLColor color)
        {
            var res = HUSL.ColorConverter.HUSLToRGB(new List<double>{ color.H, color.S, color.L });
            return new Color4((byte)((res[0]) * 255), (byte)((res[1]) * 255), (byte)((res[2]) * 255), 255);
        }
    }
}

