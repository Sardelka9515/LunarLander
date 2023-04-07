using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Numerics;

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
            List<float> tmpVts = new(vertices.Length);
            for (int i = 1; i < vertices.Length; i++)
            {
                var dir = vertices[i] - vertices[i - 1];
                var angle = MathF.Atan2(dir.Y, dir.X);
                // Skip axes with similar angle
                if (!tmpVts.Any(x => Math.Abs(x - angle) < 0.001))
                    tmpVts.Add(angle);
            }
            LocalAxes = tmpVts.ToArray();
            WorldAxes = new float[LocalAxes.Length];
            WorldVertices = new Vector2[LocalVertices.Length];
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
        {
            var len = WorldVertices.Length;
            for (int i = 1; i < len; i++)
            {
                proc(new(WorldVertices[i - 1], WorldVertices[i] - WorldVertices[i - 1]));
            }
            // Last to first
            proc(new(WorldVertices[len - 1], WorldVertices[0] - WorldVertices[len - 1]));
        }

        public void IsColliding(Polygon<T> p)
        {
            throw new NotImplementedException();
        }
    }
}
