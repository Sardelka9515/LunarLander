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
        public Vector2 Acceleration;
        public float AngularAcceleration;
        public float Angle;
        public float AngularVelocity;
        public Vector2 Position;
        public Vector2 Velocity;
        public int CollisionIndex = -1;
        internal bool Remove;

        internal virtual void Update(float time)
        {
            Velocity += time * Acceleration;
            AngularVelocity += time * AngularAcceleration;
            Position += time * Velocity;
            Angle += time * AngularVelocity;
            RotationMatrix = Matrix2x2.Rotation(Angle);
        }
    }
}
