using PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace LunarLander
{
    public struct Line
    {
        public Line(Vector2 start, Vector2 dir)
        {
            Start = start;
            Direction = dir;
        }
        public Vector2 Start;
        public Vector2 Direction;
        public Vector2 End => Start + Direction;
        public float X1 => Start.X;
        public float Y1 => Start.Y;
        public float X2 => Start.X + Direction.X;
        public float Y2 => Start.Y + Direction.Y;

        public static implicit operator SharpD2D.Drawing.Line(Line l) => new(l.X1, l.Y1, l.X2, l.Y2);
    }
    public class Box
    {
        public Vector2 Acceleration;
        public float AngularAcceleration;
        public float Angle;
        public float AngularVelocity;
        public readonly Vector2 Size;
        public Vector2 Position;
        public Vector2 Velocity;
        public event Action PreUpdate;
        public event Action PostUpdate;
        internal bool Remove;
        public SharpD2D.Drawing.IBrush Brush;
        public void SetRemove()
        {
            Remove = true;
        }

        public void EnumEdges(Action<Line> proc)
        {
            var up = UpVector * Size.Y;
            var right = RightVector * Size.X;
            proc(new(Position + (up - right) / 2, right));
            proc(new(Position - (up + right) / 2, right));
            proc(new(Position + (right - up) / 2, up));
            proc(new(Position - (right + up) / 2, up));
            // proc(new(Position - UpVector * Size.Y / 2, RightVector * Size.X));
            // proc(new(UpVector * Size.Y, Position + RightVector * Size.X / 2));
            // proc(new(UpVector * Size.Y, Position - RightVector * Size.X / 2));
        }
        public Box(Vector2 size) { Size = size; }
        public Vector2 UpVector
        {
            get
            {
                var right = RightVector;
                return new(right.Y, -right.X);
            }
        }
        public Vector2 LeftVector
            => -RightVector;
        public Vector2 RightVector
            => new((float)Math.Cos(Angle), -(float)Math.Sin(Angle));
        public Line XAxis;
        public Line YAxis;
        internal void Update(float time)
        {
            PreUpdate?.Invoke();
            Velocity += time * Acceleration;
            AngularVelocity += time * AngularAcceleration;
            Position += time * Velocity;
            Angle += time * AngularVelocity;
            XAxis.Start = YAxis.Start = Position;
            XAxis.Direction = RightVector;
            YAxis.Direction = UpVector;
            PostUpdate?.Invoke();
        }

        public void ApplyVelocity(Vector2 increment)
        {
            Velocity += increment;
        }

        public void ApplyAngularVelocity(float increment)
        {
            AngularVelocity += increment;
        }

        public bool IsIntersectingWith(Box box)
        {
            if (!ProjectionHitX(box.Project(XAxis)))
                return false;
            if (!ProjectionHitY(box.Project(YAxis)))
                return false;
            if (!box.ProjectionHitX(Project(box.XAxis)))
                return false;
            if (!box.ProjectionHitY(Project(box.YAxis)))
                return false;
            return true;
        }
        private bool ProjectionHitX(Line l)
        {
            var halfWidth = Size.X / 2;
            var left = Position - RightVector * halfWidth;
            var right = Position + RightVector * halfWidth;
            var halfLine = l.Direction / 2;
            var halfLineLenSq = halfLine.LengthSquared();
            var lc = l.Start + halfLine;
            return lc.DistanceToSquared(Position) <= halfWidth * halfWidth || right.DistanceToSquared(lc) <= halfLineLenSq || left.DistanceToSquared(lc) <= halfLineLenSq;
        }

        private bool ProjectionHitY(Line l)
        {
            var halfHeight = Size.Y / 2;
            var down = Position - UpVector * halfHeight;
            var up = Position + UpVector * halfHeight;
            var halfLine = l.Direction / 2;
            var halfLineLenSq = halfLine.LengthSquared();
            var lc = l.Start + halfLine;
            return lc.DistanceToSquared(Position) <= halfHeight * halfHeight || down.DistanceToSquared(lc) <= halfLineLenSq || up.DistanceToSquared(lc) <= halfLineLenSq;
        }

        public unsafe Line Project(Line line)
        {
            var points = stackalloc Vector2[4];
            var up = Size.Y * UpVector / 2;
            var right = Size.X * RightVector / 2;
            var tl = Position + up - right;
            var tr = Position + up + right;
            var bl = Position - up - right; ;
            var br = Position - up + right; ;
            points[0] = tl.Project(line);
            points[1] = tr.Project(line);
            points[2] = bl.Project(line);
            points[3] = br.Project(line);

            // Find longest line
            Vector2 high = points[0];
            var low = high;
            for (int i = 1; i < 4; i++)
            {
                if (points[i].X == high.X)
                    goto compareY;

                if (points[i].X > high.X)
                    high = points[i];

                if (points[i].X < low.X)
                    low = points[i];
            }
            return new Line(high, low - high);

        compareY:
            for (int i = 1; i < 4; i++)
            {
                if (points[i].Y > high.Y)
                    high = points[i];

                if (points[i].Y < low.Y)
                    low = points[i];
            }
            return new Line(high, low - high);
        }
    }
}
