using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LunarLander
{
    public struct Matrix2x2
    {
        public static readonly Matrix2x2 Identity = new(1, 0, 0, 1);
        public Matrix2x2(float m11, float m12, float m21, float m22)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
        }
        // [M11,M12]
        // [M21,M22]

        public float M11;
        public float M12;
        public float M21;
        public float M22;

        public float GetDeterminant()
        {
            return M11 * M22 - M12 * M21;
        }

        public Matrix2x2 Inverse()
        {
            return 1 / GetDeterminant() * new Matrix2x2(M22, -M12, -M21, M11);
        }

        public static Matrix2x2 Rotation(float angle)
        {
            var s = MathF.Sin(angle);
            var c = MathF.Cos(angle);
            return new(c, -s, s, c);
        }

        public static Matrix2x2 operator *(Matrix2x2 a, Matrix2x2 b)
        {
            return new(
                a.M11 * b.M11 + a.M12 * b.M21,
                a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21,
                a.M21 * b.M12 + a.M22 * b.M22);
        }

        public static unsafe Matrix2x2 operator *(Matrix2x2 a, float b)
        {
            *(Vector4*)&a *= b;
            return a;
        }

        public static unsafe Matrix2x2 operator *(float b, Matrix2x2 a)
        {
            *(Vector4*)&a *= b;
            return a;
        }

        public static Vector2 operator *(Matrix2x2 a, Vector2 v)
        {
            return new(v.X * a.M11 + v.Y * a.M12, v.X * a.M21 + v.Y * a.M22);
        }
    }
    public static class Extensions
    {
        public static SharpD2D.Drawing.PointF ToPointF(this Vector2 vec) => new(vec.X, vec.Y);

        public static byte[] ToByteArray(this Stream s)
        {
            using MemoryStream dest = new();
            s.CopyTo(dest);
            return dest.ToArray();
        }

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
