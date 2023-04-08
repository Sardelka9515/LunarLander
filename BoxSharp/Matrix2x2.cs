using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BoxSharp
{
    public struct Matrix2x2
    {
        public static readonly Matrix2x2 Identity = new(1, 0, 0, 1);
        public Matrix2x2(float m11, float m12, float m21, float m22)
        {
            M11 = m11;
            M12 = m12;
            M21 = m21;
            M22 = m22;
        }
        // [M11,M12]
        // [M21,M22]

        public float M11;
        public float M12;
        public float M21;
        public float M22;

        public float GetDeterminant()
        {
            return M11 * M22 - M12 * M21;
        }

        public Matrix2x2 Inverse()
        {
            return 1 / GetDeterminant() * new Matrix2x2(M22, -M12, -M21, M11);
        }

        public float TransformX(Vector2 v)
        {
            return v.X * M11 + v.Y * M12;
        }


        public float TransformY(Vector2 v)
        {
            return v.X * M21 + v.Y * M22;
        }

        public static Matrix2x2 Rotation(float angle)
        {
            var s = MathF.Sin(angle);
            var c = MathF.Cos(angle);
            return new(c, -s, s, c);
        }

        public static Matrix2x2 operator *(Matrix2x2 a, Matrix2x2 b)
        {
            return new(
                a.M11 * b.M11 + a.M12 * b.M21,
                a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21,
                a.M21 * b.M12 + a.M22 * b.M22);
        }

        public static unsafe Matrix2x2 operator *(Matrix2x2 a, float b)
        {
            *(Vector4*)&a *= b;
            return a;
        }

        public static unsafe Matrix2x2 operator *(float b, Matrix2x2 a)
        {
            *(Vector4*)&a *= b;
            return a;
        }

        public static Vector2 operator *(Matrix2x2 a, Vector2 v)
        {
            return new(v.X * a.M11 + v.Y * a.M12, v.X * a.M21 + v.Y * a.M22);
        }
    }
}
