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

        public Shape<T> A;
        public Shape<T> B;
        public Face incidentFace;
        public float penetration;     // Depth of penetration from collision
        public Vector2 normal;          // From A to B
        public Vector2[] contacts = new Vector2[2];     // Points of contact during collision
        public int contactCount; // Number of contacts that occurred during collision
        public float restitution;               // Mixed restitution
        public float dynamicFriction;              // Mixed dynamic friction
        public float staticFriction;              // Mixed static friction
        public float intensity;  // Max contact velocity
        public void Solve()
        {
            Solvers[(int)A.Type][(int)B.Type](this, A, B);
        }

        public void MixMaterials()
        {
            // Calculate average restitution
            restitution = MathF.Min(A.Restitution, B.Restitution);

            // Calculate static and dynamic friction
            staticFriction = MixFriction(A.StaticFriction, B.StaticFriction);
            dynamicFriction = MixFriction(A.DynamicFriction, B.DynamicFriction);
        }
        void InfiniteMassCorrection()
        {
            A.Velocity = default;
            B.Velocity = default;
        }
        public void ApplyImpulse()
        {
            // Early out and positional correct if both objects have infinite mass
            if (Equals(A._inverseMass + B._inverseMass, 0))
            {
                InfiniteMassCorrection();
                return;
            }

            intensity = 0;
            for (int i = 0; i < contactCount; ++i)
            {
                // Calculate radii from COM to contact
                var ra = contacts[i] - A.Position;
                var rb = contacts[i] - B.Position;

                // Relative velocity
                var rv = B.Velocity + B.AngularVelocity.CrossProduct(rb) -
                          A.Velocity - A.AngularVelocity.CrossProduct(ra);

                // Relative velocity along the normal
                var contactVel = rv.DotProduct(normal);
                // Do not resolve if velocities are separating
                if (contactVel > 0)
                    return;

                intensity = MathF.Max(intensity, -contactVel);

                var raCrossN = ra.CrossProduct(normal);
                var rbCrossN = rb.CrossProduct(normal);
                var invMassSum = A._inverseMass + B._inverseMass + Squared(raCrossN) * A._inverseInertia + Squared(rbCrossN) * B._inverseInertia;

                // Calculate impulse scalar
                var j = -(1.0f + restitution) * contactVel;
                j /= invMassSum;
                j /= contactCount;

                // Apply impulse
                var impulse = normal * j;
                A.ApplyImpulse(-impulse, ra);
                B.ApplyImpulse(impulse, rb);



                // Friction impulse
                rv = B.Velocity + B.AngularVelocity.CrossProduct(rb) -
                     A.Velocity - A.AngularVelocity.CrossProduct(ra);

                var t = rv - (normal * rv.DotProduct(normal));
                t = Vector2.Normalize(t);

                // j tangent magnitude
                var jt = -rv.DotProduct(t);
                jt /= invMassSum;
                jt /= contactCount;

                // Don't apply tiny friction impulses
                if (Equals(jt, 0.0f))
                    return;

                // Coulumb's law
                Vector2 tangentImpulse;
                if (MathF.Abs(jt) < j * staticFriction)
                    tangentImpulse = t * jt;
                else
                    tangentImpulse = t * -j * dynamicFriction;

                // Apply friction impulse
                A.ApplyImpulse(-tangentImpulse, ra);
                B.ApplyImpulse(tangentImpulse, rb);
            }
        }

        public void PositionalCorrection()
        {
            const float k_slop = 0.05f; // Penetration allowance
            const float percent = 0.4f; // Penetration percentage to correct
            var correction = (MathF.Max(penetration - k_slop, 0.0f) / (A._inverseMass + B._inverseMass)) * normal * percent;
            A.Position -= correction * A._inverseMass;
            B.Position += correction * B._inverseMass;
        }
    }
}
