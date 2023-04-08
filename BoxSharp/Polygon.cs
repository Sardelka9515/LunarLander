using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Numerics;

namespace BoxSharp
{
    public record struct Axis(float angle, Vector2 dir);
    public class Polygon<T> : Shape<T>
    {
        public readonly Vector2[] LocalVertices;
        public readonly Vector2[] WorldVertices;
        public readonly Axis[] LocalAxes;
        public readonly Axis[] WorldAxes;

        /// <param name="vertices">
        /// Coordinates of vertices in this polygon, in local space</param>
        /// <exception cref="ArgumentException"></exception>
        public Polygon(Vector2[] vertices, float density = 1) : base(Extensions.GetPolygonArea(vertices) * density)
        {
            if (vertices.Length <= 2)
                throw new ArgumentException("Invalid polygon shape", nameof(vertices));

            LocalVertices = vertices;
            WorldVertices = new Vector2[LocalVertices.Length];
            List<Axis> axes = new(vertices.Length);
            EnumEdges(LocalVertices, (l) =>
            {
                var dir = l.Direction;
                // Flip vector to same direction
                if (dir.X < 0 || (dir.X == 0 && dir.Y < 0))
                    dir = -dir;
                // Projection along the edge, so the axis is perpendicular to the edge
                var angle = MathF.Atan2(dir.X, -dir.Y);
                // Skip axes with similar angle
                if (!axes.Any(x => MathF.Abs(x.angle - angle) < 0.00001f))
                    axes.Add(new(angle, dir));
            });
            LocalAxes = axes.ToArray();
            WorldAxes = new Axis[LocalAxes.Length];
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
                WorldAxes[i].angle = LocalAxes[i].angle + Angle;
                WorldAxes[i].dir = RotationMatrix * LocalAxes[i].dir;
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

        /// <summary>
        /// Get the vertex that is furthest along the specified direction
        /// </summary>
        public Vector2 GetSupport(Vector2 dir)
        {
            Vector2 bestVertex = WorldVertices[0];
            float bestProjection = bestVertex.DotProduct(dir);
            for (int i = 1; i < WorldVertices.Length; ++i)
            {
                var v = WorldVertices[i];
                float projection = v.DotProduct(dir);
                if (projection > bestProjection)
                {
                    bestVertex = v;
                    bestProjection = projection;
                }
            }
            return bestVertex;
        }

        /// <summary>
        /// Find the penetration depth and face index of A from B
        /// </summary>
        /// <param name="faceIndex">The index of <paramref name="A"/>'s edge that's currently intersecting with <paramref name="B"/></param>
        /// <param name="A">The polygon whose edges are to be tested</param>
        /// <param name="B"></param>
        /// <returns>The penetration depth</returns>
        public static float FindPenetration(out int faceIndex, Polygon<T> A, Polygon<T> B)
        {
            int bestIndex = 0;
            float bestDistance = float.MinValue;
            int i = 0;
            A.EnumEdges(getPenetration);
            faceIndex = bestIndex;
            return bestDistance;

            void getPenetration(Line edge)
            {
                // Retrieve a face normal from A 
                Vector2 n = new(-edge.Direction.Y, edge.Direction.X);
                // Retrieve support point from B along -n 
                Vector2 s = B.GetSupport(-n);
                // Retrieve vertex on face from A
                Vector2 v = edge.Start;
                // Compute penetration distance
                float d = n.DotProduct(s - v);
                // Store greatest distance 
                if (d > bestDistance)
                {
                    bestDistance = d;
                    bestIndex = i;
                }
                i++;
            };
        }

        public bool IsIntersectingWith(Polygon<T> p)
        {
            // Check for a separating axis with A's face planes
            float penetrationA = FindPenetration(out int faceA, this, p);
            if (penetrationA >= 0.0f)
                return false;

            // Check for a separating axis with B's face planes
            int faceB;
            float penetrationB = FindPenetration(out faceB, p, this);
            if (penetrationB >= 0.0f)
                return false;

            return true;
        }

        #region SAT

        /// <summary>
        /// Check whether this polygon is intersecting with another using SAT (Separating Axis Theorem)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public unsafe bool IsIntersectingWith_SAT(Polygon<T> p)
        {
            if (HasGap(WorldAxes, this, p))
                return false;

            if (HasGap(p.WorldAxes, this, p))
                return false;

            return true;
        }

        public static bool HasGap(Axis[] axes, Polygon<T> p1, Polygon<T> p2)
        {
            float low1, low2, high1, high2;
            for (int i = 0; i < axes.Length; i++)
            {
                // Get a reverse transformation matrix for projection
                var tm = Matrix2x2.Rotation(-axes[i].angle);
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

        #endregion
    }
}
