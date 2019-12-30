using System;

namespace FractalImageCompression
{
    public enum Reflection
    {
        NoReflection = 0,
        HorizontalReflection = 1,
        All = 2,
    }

    public enum Rotation
    {
        NoRotation = 0,
        PosQuarterRotation = 1,
        HalfRotation = 2,
        NegQuarterRotation = 3,
        All = 4
    }

    public struct Transform
    {
        public Reflection Reflection { get; }
        public Rotation Rotation { get; }

        public Transform(Reflection reflection, Rotation rotation)
        {
            Reflection = reflection;
            Rotation = rotation;
        }
    }
}
