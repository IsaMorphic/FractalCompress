using System;
using System.Collections.Generic;
using System.Text;

namespace FractalCompress
{
    internal static class MathExtensions
    {
        public static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
