using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BoxSharp
{

    public partial class Manifold<T>
    {
        delegate void CollisionSolver(Manifold<T> m, Shape<T> A, Shape<T> B);
        static CollisionSolver[][] Solvers =
            new CollisionSolver[][]{
                new CollisionSolver[]{
                    PolygonToPolygon
                }
            };

        static unsafe void PolygonToPolygon(Manifold<T> m, Shape<T> sa, Shape<T> sb)
        {
            var A = (Polygon<T>)sa;
            var B = (Polygon<T>)sb;
            m.contactCount = 0;

            var penetrationA = Polygon<T>.FindPenetration(out var faceA, A, B);
            if (penetrationA >= 0.0f)
                return;

            var penetrationB = Polygon<T>.FindPenetration(out var faceB, B, A);
            if (penetrationB >= 0.0f)
                return;

            int referenceIndex;
            bool flip; // Always point from a to b

            Polygon<T> RefPoly; // Reference
            Polygon<T> IncPoly; // Incident

            // Determine which shape contains reference face
            if (BiasGreaterThan(penetrationA, penetrationB))
            {
                RefPoly = A;
                IncPoly = B;
                referenceIndex = faceA;
                flip = false;
            }

            else
            {
                RefPoly = B;
                IncPoly = A;
                referenceIndex = faceB;
                flip = true;
            }

            // World space incident face
            FindIncidentFace(ref m.incidentFace, RefPoly, IncPoly, referenceIndex);

            //        y
            //        ^  ->n       ^
            //      +---c ------posPlane--
            //  x < | i |\
            //      +---+ c-----negPlane--
            //             \       v
            //              r
            //
            //  r : reference face
            //  i : incident poly
            //  c : clipped point
            //  n : incident normal

            // Setup reference face vertices
            RefPoly.GetEdge(referenceIndex, out var v1, out var v2);

            // Calculate reference face side normal in world space
            var sidePlaneNormal = (v2 - v1);
            sidePlaneNormal = Vector2.Normalize(sidePlaneNormal);

            // Orthogonalize
            var refFaceNormal = new Vector2(-sidePlaneNormal.Y, sidePlaneNormal.X);
            // ax + by = c
            // c is distance from origin
            var refC = refFaceNormal.DotProduct(v1);
            var negSide = -sidePlaneNormal.DotProduct(v1);
            var posSide = sidePlaneNormal.DotProduct(v2);

            // Clip incident face to reference face side planes
            if (Clip(-sidePlaneNormal, negSide, ref m.incidentFace) < 2)
                return; // Due to floating point error, possible to not have required points

            if (Clip(sidePlaneNormal, posSide, ref m.incidentFace) < 2)
                return; // Due to floating point error, possible to not have required points

            // Flip
            m.normal = flip ? -refFaceNormal : refFaceNormal;

            // Keep points behind reference face
            int cp = 0; // clipped points behind reference face
            var separation = refFaceNormal.DotProduct(m.incidentFace.start) - refC;
            if (separation <= 0.0f)
            {
                m.contacts[cp] = m.incidentFace.start;
                m.penetration = -separation;
                ++cp;
            }
            else
                m.penetration = 0;

            separation = refFaceNormal.DotProduct(m.incidentFace.end) - refC;
            if (separation <= 0.0f)
            {
                m.contacts[cp] = m.incidentFace.end;

                m.penetration += -separation;
                ++cp;

                // Average penetration
                m.penetration /= cp;
            }

            m.contactCount = cp;
        }
    }
}
