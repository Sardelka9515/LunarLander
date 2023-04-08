using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BoxSharp
{
    public abstract class Shape<T>
    {
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
        public readonly float Mass;
        public readonly float InverseMass;
        public Vector2 Gravity;

        internal bool Remove;
        Vector2 _acceleration;
        Vector2 _force;
        public Shape(float mass)
        {
            Mass = mass;
            InverseMass = mass == 0 ? 0 : 1 / mass;
        }
        
        public void ApplyForce(Vector2 f)
        {
            _force += f;
        }

        internal virtual void Update(float time)
        {
            AngularVelocity += time * AngularAcceleration;
            Angle += time * AngularVelocity;
            RotationMatrix = Matrix2x2.Rotation(Angle);
            ApplyForce(Gravity * Mass);
            _acceleration = _force * InverseMass;
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
