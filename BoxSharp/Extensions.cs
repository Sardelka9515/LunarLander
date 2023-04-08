using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BoxSharp
{
    public static class Extensions
    {
        public static Vector2 Project(this Vector2 from, Line to)
        {
            var dotValue = to.Direction.X * (from.X - to.Start.X) + to.Direction.Y * (from.Y - to.Start.Y);
            return new Vector2(to.Start.X + to.Direction.X * dotValue, to.Start.Y + to.Direction.Y * dotValue);
        }

        public static float DistanceToSquared(this Vector2 vec, Vector2 target)
        {
            var diffX = vec.X - target.X;
            var diffY = vec.Y - target.Y;
            return diffX * diffX + diffY * diffY;
        }

        public static float DistanceTo(this Vector2 vec, Vector2 target) => MathF.Sqrt(vec.DistanceToSquared(target));

        public static bool TryIntersect(this Line lineA, Line lineB, out Vector2 result, bool checkSegment = false)
        {
            var ax1 = lineA.X1;
            var ax2 = lineA.X2;
            var ay1 = lineA.Y1;
            var ay2 = lineA.Y2;

            var bx1 = lineB.X1;
            var bx2 = lineB.X2;
            var by1 = lineB.Y1;
            var by2 = lineB.Y2;

            var A1 = -lineA.Direction.Y;
            var B1 = lineA.Direction.X;
            var C1 = A1 * ax1 + B1 * ay1;

            var A2 = -lineB.Direction.Y;
            var B2 = lineB.Direction.X;
            var C2 = A2 * bx1 + B2 * by1;

            float det = A1 * B2 - A2 * B1;

            if (det == 0)
            {
                // Parallel
                result = default;
                return false;
            }

            float x = (B2 * C1 - B1 * C2) / det;
            float y = (A1 * C2 - A2 * C1) / det;

            if (checkSegment &&
                !((x.IsBetween(ax1, ax2) || y.IsBetween(ay1, ay2))
                && (x.IsBetween(bx1, bx2) || y.IsBetween(by1, by2))))
            {
                result = default;
                return false;
            }
            result = new Vector2(x, y);
            return true;
        }

        public static bool IsBetween(this float a, float b, float c)
        {
            return (c < a && a < b) || (b < a && a < c);
        }

        // Returns true if given point(x,y) is inside the given line segment
        private static bool IsInsideLine(Line line, double x, double y)
        {
            return (x >= line.X1 && x <= line.X2
                        || x >= line.X2 && x <= line.X1)
                   && (y >= line.X1 && y <= line.X2
                        || y >= line.X1 && y <= line.X2);
        }
    }
}
