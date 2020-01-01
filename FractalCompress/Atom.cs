using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalCompress
{
    public struct Atom
    {
        public byte X { get; }
        public byte Y { get; }

        public float Contrast { get; }
        public float Brightness { get; }

        public Transform Transform { get; }

        public Atom(byte x, byte y, float contrast, float brightness, Transform transform)
        {
            X = x;
            Y = y;
            Contrast = contrast;
            Brightness = brightness;
            Transform = transform;
        }

        public byte[] Serialize()
        {
            return new byte[] {
                X, Y,
                (byte)Math.Clamp(Contrast * 255, 0, 255),
                (byte)Math.Clamp(Brightness * 255, 0, 255),
                (byte)Transform.Reflection,
                (byte)Transform.Rotation
            };
        }

        public static bool Deserialize(Stream stream, out Atom atom)
        {
            byte[] buffer = new byte[6];
            if (stream.Read(buffer, 0, buffer.Length) == 0)
            {
                atom = default(Atom);
                return false;
            }

            byte x = buffer[0];
            byte y = buffer[1];
            float contrast = (float)buffer[2] / 255;
            float brightness = (float)buffer[3] / 255;
            byte reflection = buffer[4];
            byte rotation = buffer[5];
            Transform transform = new Transform((Reflection)reflection, (Rotation)rotation);
            atom = new Atom(x, y, contrast, brightness, transform);
            return true;
        }
    }
}