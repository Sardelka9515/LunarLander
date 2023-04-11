global using static BoxSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public struct Face { public Vector2 start; public Vector2 end; }
        public const float EPSILON = 0.0001f;
        public static Vector2 EdgeNormal(Vector2 dir) => new(-dir.Y, dir.X);
        public static bool Equals(float a, float b) => MathF.Abs(a - b) <= EPSILON;
        public static float Squared(float value) => value * value;
        public static unsafe int Clip(Vector2 n, float c, ref Face face)
        {
            int sp = 0;
            var result = stackalloc Vector2[2] {
                face.start,
                face.end
            };

            // Retrieve distances from each endpoint to the line
            // d = ax + by - c
            var d1 = n.DotProduct(face.start) - c;
            var d2 = n.DotProduct(face.end) - c;

            // If negative (behind plane) clip
            if (d1 <= 0.0f) result[sp++] = face.start;
            if (d2 <= 0.0f) result[sp++] = face.end;

            // If the points are on different sides of the plane
            if (d1 * d2 < 0.0f) // less than to ignore -0.0f
            {
                // Push intersection point
                var alpha = d1 / (d1 - d2);
                result[sp] = face.start + alpha * (face.end - face.start);
                ++sp;
            }

            // Assign our new converted values
            face.start = result[0];
            face.end = result[1];

            Debug.Assert(sp != 3);

            return sp;
        }
        public static unsafe void FindIncidentFace<T>(ref Face face, Polygon<T> RefPoly, Polygon<T> IncPoly, int referenceIndex)
        {
            var referenceNormal = RefPoly.EdgeNormals[referenceIndex].world;

            // Find most anti-normal face on incident polygon
            int incidentFace = 0;
            float minDot = float.MaxValue;
            for (int i = 0; i < IncPoly.Vertices.Length; ++i)
            {
                var dot = referenceNormal.DotProduct(IncPoly.EdgeNormals[i].world);
                if (dot < minDot)
                {
                    minDot = dot;
                    incidentFace = i;
                }
            }
            IncPoly.GetEdge(incidentFace, out face.start, out face.end);
        }

        public static bool BiasGreaterThan(float a, float b)
        {
            const float k_biasRelative = 0.95f;
            const float k_biasAbsolute = 0.01f;
            return a >= b * k_biasRelative + a * k_biasAbsolute;
        }

        public static float MixFriction(float a, float b)
            => MathF.Sqrt(a * b);

        // Two crossed vectors return a scalar 
        public static float CrossProduct(this Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }
        // More exotic (but necessary) forms of the cross product 
        // with a vector a and scalar s, both returning a vector 
        public static Vector2 CrossProduct(this Vector2 a, float s)
        {
            return new(s * a.Y, -s * a.X);
        }
        public static Vector2 CrossProduct(this float s, Vector2 a)
        {
            return new(-s * a.Y, s * a.X);
        }


        public static float DotProduct(this Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
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
    }
}
