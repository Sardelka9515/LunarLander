using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Numerics;
using System.Reflection.Metadata;

namespace BoxSharp
{
    public class Polygon<T> : Shape<T>
    {
        public readonly Vector2[] LocalVertices;
        public readonly float[] LocalAxes;
        public readonly Vector2[] WorldVertices;
        public readonly float[] WorldAxes;

        /// <param name="vertices">
        /// Coordinates of vertices in this polygon, in local space</param>
        /// <exception cref="ArgumentException"></exception>
        public Polygon(Vector2[] vertices)
        {
            if (vertices.Length <= 2)
                throw new ArgumentException("Invalid polygon shape", nameof(vertices));

            LocalVertices = vertices;
            WorldVertices = new Vector2[LocalVertices.Length];
            List<float> angles = new(vertices.Length);
            EnumEdges(LocalVertices, (l) =>
            {
                var dir = l.Direction;
                // Flip vector to same direction
                if (dir.X < 0 || (dir.X == 0 && dir.Y < 0))
                    dir = -dir;
                // Projection along the edge, so the axis is perpendicular to the edge
                var angle = MathF.Atan2(dir.X, -dir.Y);
                // Skip axes with similar angle
                if (!angles.Any(x => MathF.Abs(x - angle) < 0.00001f))
                    angles.Add(angle);
            });
            LocalAxes = angles.ToArray();
            WorldAxes = new float[LocalAxes.Length];
        }

        internal override void Update(float time)
        {
            base.Update(time);
            for (int i = 0; i < LocalVertices.Length; i++)
            {
                WorldVertices[i] = RotationMatrix * LocalVertices[i] + Position;
            }
            for (int i = 0; i < LocalAxes.Length; i++)
            {
                WorldAxes[i] = LocalAxes[i] + Angle;
            }
        }

        public void EnumEdges(Action<Line> proc)
            => EnumEdges(WorldVertices, proc);

        public static void EnumEdges(Vector2[] vertices, Action<Line> proc)
        {
            var len = vertices.Length;
            for (int i = 1; i < len; i++)
            {
                proc(new(vertices[i - 1], vertices[i] - vertices[i - 1]));
            }
            // Closing side
            proc(new(vertices[len - 1], vertices[0] - vertices[len - 1]));
        }

        public unsafe bool IsIntersectingWith(Polygon<T> p)
        {
            if (HasGap(WorldAxes, this, p))
                return false;

            if (HasGap(p.WorldAxes, this, p))
                return false;

            return true;
        }

        public static bool HasGap(float[] axes, Polygon<T> p1, Polygon<T> p2)
        {
            float low1, low2, high1, high2;
            for (int i = 0; i < axes.Length; i++)
            {
                // Get a reverse transformation matrix for projection
                var tm = Matrix2x2.Rotation(-axes[i]);
                p1.Project(ref tm, out low1, out high1);
                p2.Project(ref tm, out low2, out high2);
                if (low1 > high2 || high1 < low2)
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Project the shape using specified transformation matrix and get the shadow on x axis (two points)
        /// </summary>
        public void Project(ref Matrix2x2 tm, out float low, out float high)
        {
            low = high = tm.TransformX(WorldVertices[0]);
            for (int i = 1; i < WorldVertices.Length; i++)
            {
                var point = tm.TransformX(WorldVertices[i]);
                if (point < low) low = point;
                else if (point > high) high = point;
            }
        }
    }
}
