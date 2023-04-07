using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BoxSharp
{
    public class Box<T> : Polygon<T>
    {
        public readonly Vector2 Size;
        public void SetRemove()
        {
            Remove = true;
        }

        public Box(Vector2 size) : base(
            new Vector2[] {
                new(-size.X/2,size.Y/2),
                new(size.X/2,size.Y/2),
                new(size.X/2,-size.Y/2),
                new(-size.X/2,-size.Y/2)
            })
        { Size = size; }
        public Line XAxis;
        public Line YAxis;
        internal override void Update(float time)
        {
            base.Update(time);
            XAxis.Start = YAxis.Start = Position;
            XAxis.Direction = RightVector;
            YAxis.Direction = UpVector;
        }
        public bool IsIntersectingWith(Box<T> box)
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
