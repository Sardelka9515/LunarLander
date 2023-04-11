using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BoxSharp
{
    public enum ShapeType
    {
        Polygon
    }
    public abstract class Shape<T>
    {
        public abstract ShapeType Type { get; }

        public T Tag;

        // Left-handed coordinate system, so up is negative
        public Vector2 UpVector => RotationMatrix * -Vector2.UnitY;
        public Vector2 DownVector => RotationMatrix * Vector2.UnitY;
        public Vector2 LeftVector => RotationMatrix * -Vector2.UnitX;
        public Vector2 RightVector => RotationMatrix * Vector2.UnitX;

        public Matrix2x2 RotationMatrix = Matrix2x2.Identity;
        public float AngularAcceleration;
        public float Angle;
        public float AngularVelocity;
        public Vector2 Position;
        public Vector2 Velocity;
        public int CollisionIndex = -1;
        public float Mass => _mass;

        public float Restitution = 0.5f;
        public float StaticFriction = 0.6f;
        public float DynamicFriction = 0.4f;
        public float Inertia => _inertia;
        public Vector2 Gravity;

        internal bool Remove;
        internal Vector2 _acceleration;
        internal Vector2 _force;
        internal float _inertia;
        internal float _inverseInertia;
        internal float _mass;
        internal float _inverseMass;

        public void ApplyForce(Vector2 f)
        {
            _force += f;
        }

        public void ApplyImpulse(Vector2 impulse, Vector2 offset)
        {
            var toCenter = -offset;
            // Get cos between to center and impulse
            var liner = toCenter.DotProduct(impulse) / (impulse.Length() * offset.Length());
            Velocity += _inverseMass * impulse * MathF.Abs(liner);
            AngularVelocity += _inverseInertia * offset.CrossProduct(impulse);
        }

        internal virtual void Update(float time)
        {
            AngularVelocity += time * AngularAcceleration;
            Angle += time * AngularVelocity;
            RotationMatrix = Matrix2x2.Rotation(Angle);
            ApplyForce(Gravity * _mass);
            _acceleration = _force * _inverseMass;
            Velocity += time * _acceleration;
            Position += time * Velocity;
            _force = default;
        }
        public void SetRemove()
        {
            Remove = true;
        }
    }
}
