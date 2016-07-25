using System;
using System.Collections.Generic;
using System.Collections;
using OpenTK.Graphics;

namespace TurntNinja.Core
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
            //var c = new ColorMine.ColorSpaces.Rgb { R = color.R*255, G = color.G*255, B = color.B*255 };
            //var r = c.To<ColorMine.ColorSpaces.Hsl>();
            //H = r.H;
            //S = r.S;
            //L = r.L;
            var res = HUSL.ColorConverter.RGBToHUSL(new List<double> { color.R, color.G, color.B });
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
            //var c = new ColorMine.ColorSpaces.Hsl { H = color.H, L = color.L, S = color.S };
            //var r = c.ToRgb();
            //return new Color4((float)r.R/255, (float)r.G/255, (float)r.B/255, 1.0f);
            var res = HUSL.ColorConverter.HUSLToRGB(new List<double>{ color.H, color.S, color.L });
            return new Color4((byte)((res[0]) * 255), (byte)((res[1]) * 255), (byte)((res[2]) * 255), 255);
        }
    }
}

