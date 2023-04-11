using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
            // Will be re-centered after mass calculation
            new Vector2[] {
                new(0,0),
                new(size.X,0),
                new(size.X,-size.Y),
                new(0,-size.Y)
            })
        {
            Size = size;
            Debug.Assert(Axes.Length == 2);
        }
    }
}
