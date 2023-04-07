using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BoxSharp
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

    }
}
