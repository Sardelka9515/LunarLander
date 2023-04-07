using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace LunarLander
{
    internal static class Extensions
    {
        public static SharpD2D.Drawing.PointF ToPointF(this Vector2 vec) => new(vec.X, vec.Y);

        public static SharpD2D.Drawing.Line ToLine(this BoxSharp.Line l) => new(l.X1, l.Y1, l.X2, l.Y2);
    }
}
