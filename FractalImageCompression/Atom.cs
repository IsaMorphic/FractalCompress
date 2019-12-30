using System;
using System.Collections.Generic;
using System.Text;

namespace FractalImageCompression
{
    public struct Atom
    {
        public int X { get; }
        public int Y { get; }
        public float Contrast { get; }
        public float Brightness { get; }
        public Transform Transform { get; }

        public Atom(int x, int y, float contrast, float brightness, Transform transform) : this()
        {
            X = x;
            Y = y;
            Contrast = contrast;
            Brightness = brightness;
            Transform = transform;
        }
    }
}
