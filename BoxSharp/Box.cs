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

        public Box(Vector2 size) : base(
            new Vector2[] {
                new(-size.X/2,size.Y/2),
                new(size.X/2,size.Y/2),
                new(size.X/2,-size.Y/2),
                new(-size.X/2,-size.Y/2)
            })
        { Size = size; }
    }
}
