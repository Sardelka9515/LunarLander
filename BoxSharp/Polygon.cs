using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Numerics;

namespace BoxSharp
{
    public struct VecTuple
    {
        public VecTuple(Vector2 local, Vector2 world)
        {
            this.local = local; this.world = world;
        }
        public Vector2 local;
        public Vector2 world;
    }
    public class Polygon<T> : Shape<T>
    {
        public override ShapeType Type => ShapeType.Polygon;

        public readonly VecTuple[] Vertices;
        public readonly VecTuple[] Axes;
        public readonly VecTuple[] EdgeNormals;

        /// <param name="vertices">
        /// Coordinates of vertices in this polygon, in local space</param>
        /// <exception cref="ArgumentException"></exception>
        public Polygon(Vector2[] vertices, float density = 1)
        {
            if (vertices.Length <= 2)
                throw new ArgumentException("Invalid polygon shape", nameof(vertices));

            Vertices = vertices.Select(x => new VecTuple(x, default)).ToArray();
            EdgeNormals = new VecTuple[Vertices.Length];
            List<VecTuple> axes = new(vertices.Length);
            int i = 0;
            EnumEdgesLocal(Vertices, (l) =>
            {
                var dir = EdgeNormals[i].local = Vector2.Normalize(new(-l.Direction.Y, l.Direction.X));

                // Flip vector to same direction
                if (dir.X < 0 || (dir.X == 0 && dir.Y < 0))
                    dir = -dir;

                var angle = MathF.Atan2(dir.Y, dir.X);

                // Skip axes with similar angle
                if (!axes.Any(x => MathF.Abs(MathF.Atan2(x.local.Y, x.local.X) - angle) < EPSILON))
                    axes.Add(new(dir, default));
                i++;
            });
            Axes = axes.ToArray();
            ComputeMass(density);
        }

        internal override void Update(float time)
        {
            base.Update(time);
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].world = RotationMatrix * Vertices[i].local + Position;
            }
            for (int i = 0; i < Axes.Length; i++)
            {
                Axes[i].world = RotationMatrix * Axes[i].local;
            }
            for (int i = 0; i < EdgeNormals.Length; i++)
            {
                EdgeNormals[i].world = RotationMatrix * EdgeNormals[i].local;
            }
        }

        public void EnumEdges(Action<Line> proc)
            => EnumEdgesWorld(Vertices, proc);

        public static void EnumEdgesWorld(VecTuple[] vertices, Action<Line> proc)
        {
            var len = vertices.Length;
            for (int i = 1; i < len; i++)
            {
                proc(new(vertices[i - 1].world, vertices[i].world - vertices[i - 1].world));
            }
            // Closing side
            proc(new(vertices[len - 1].world, vertices[0].world - vertices[len - 1].world));
        }
        public static void EnumEdgesLocal(VecTuple[] vertices, Action<Line> proc)
        {
            var len = vertices.Length;
            for (int i = 1; i < len; i++)
            {
                proc(new(vertices[i - 1].local, vertices[i].local - vertices[i - 1].local));
            }
            // Closing side
            proc(new(vertices[len - 1].local, vertices[0].local - vertices[len - 1].local));
        }

        /// <summary>
        /// Get the vertex that is furthest along the specified direction
        /// </summary>
        public Vector2 GetSupport(Vector2 dir)
        {
            Vector2 bestVertex = Vertices[0].world;
            float bestProjection = bestVertex.DotProduct(dir);
            for (int i = 1; i < Vertices.Length; ++i)
            {
                var v = Vertices[i].world;
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
        /// <param name="edgeIndex">The index of <paramref name="A"/>'s edge that's currently intersecting with <paramref name="B"/></param>
        /// <param name="A">The polygon whose edges are to be tested</param>
        /// <param name="B"></param>
        /// <returns>The penetration depth, negative implies that two polygons are overlapping</returns>
        public static float FindPenetration(out int edgeIndex, Polygon<T> A, Polygon<T> B, out Vector2 sunkContactPoint)
        {
            int bestIndex = 0;
            float bestDistance = float.MinValue;
            Vector2 sunkContact = default;
            for (int i = 0; i < A.EdgeNormals.Length; i++)
            {
                // Retrieve a face normal from A 
                Vector2 n = A.EdgeNormals[i].world;
                // Retrieve support point from B along -n 
                Vector2 s = B.GetSupport(-n);
                // Retrieve vertex on face from A
                Vector2 v = A.Vertices[i].world;
                // Compute penetration distance
                float d = n.DotProduct(s - v);
                // Store greatest distance 
                if (d > bestDistance)
                {
                    bestDistance = d;
                    bestIndex = i;
                    sunkContact = s;
                }
            }
            edgeIndex = bestIndex;
            sunkContactPoint = sunkContact;
            return bestDistance;
        }

        public void GetEdge(int index, out Vector2 start, out Vector2 end)
        {
            start = Vertices[index].world;
            end = Vertices[++index >= Vertices.Length ? 0 : index].world;
        }

        void ComputeMass(float density)
        {
            // Calculate centroid and moment of inertia
            Vector2 c = default; // centroid
            float area = 0.0f;
            float I = 0.0f;
            const float k_inv3 = 1.0f / 3.0f;

            for (int i1 = 0; i1 < Vertices.Length; ++i1)
            {
                // Triangle vertices, third vertex implied as (0, 0)
                Vector2 p1 = Vertices[i1].local;
                int i2 = i1 + 1 < Vertices.Length ? i1 + 1 : 0;
                Vector2 p2 = Vertices[i2].local;

                float D = MathF.Abs(p1.CrossProduct(p2));
                float triangleArea = D / 2;

                area += triangleArea;

                // Use area to weight the centroid average, not just vertex position
                c += triangleArea * k_inv3 * (p1 + p2);

                float x2 = p1.X * p1.X + p2.X * p1.X + p2.X * p2.X;
                float y2 = p1.Y * p1.Y + p2.Y * p1.Y + p2.Y * p2.Y;
                I += (0.25f * k_inv3 * D) * (x2 + y2);
            }

            c *= 1.0f / area;

            // Translate vertices to centroid (make the centroid (0, 0))
            for (int i = 0; i < Vertices.Length; ++i)
                Vertices[i].local -= c;

            _mass = density * area;
            _inverseMass = _mass == 0 ? 0 : 1 / _mass;

            _inertia = I * density;
            _inverseInertia = _inertia == 0 ? 0 : 1 / _inertia;
        }
        public void SetStatic()
        {
            _mass = _inverseMass = _inertia = _inverseInertia = 0;
        }

        /*
        private bool IsIntersectingWith(Polygon<T> p)
        {
            // Check for a separating axis with A's face planes
            float penetrationA = FindPenetration(out _, this, p, out _);
            if (penetrationA >= 0.0f)
                return false;

            // Check for a separating axis with B's face planes
            float penetrationB = FindPenetration(out _, p, this, out _);
            if (penetrationB >= 0.0f)
                return false;

            return true;
        }
        */

        #region SAT

        /// <summary>
        /// Check whether this polygon is intersecting with another using SAT (Separating Axis Theorem)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public unsafe bool IsTouching(Polygon<T> p)
        {
            if (HasGap(Axes, this, p))
                return false;

            if (HasGap(p.Axes, this, p))
                return false;

            return true;
        }

        public static bool HasGap(VecTuple[] axes, Polygon<T> p1, Polygon<T> p2)
        {
            float low1, low2, high1, high2;
            for (int i = 0; i < axes.Length; i++)
            {
                var axis = axes[i].world;
                p1.Project(axis, out low1, out high1);
                p2.Project(axis, out low2, out high2);
                if (low1 > high2 || high1 < low2)
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Project the shape using specified transformation matrix and get the shadow on x axis (two points)
        /// </summary>
        public void Project(Vector2 axis, out float low, out float high)
        {
            // Do reverse half matrix transformation to rotate it back to align with x axis
            // so we can just take the x coordinate
            float transformX(Vector2 v)
            {
                return axis.X * v.X + axis.Y * v.Y;
            }
            low = high = transformX(Vertices[0].world);
            for (int i = 1; i < Vertices.Length; i++)
            {
                var point = transformX(Vertices[i].world);
                if (point < low) low = point;
                else if (point > high) high = point;
            }
        }

        #endregion
    }
}
