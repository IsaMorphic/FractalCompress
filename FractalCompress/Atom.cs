/*
 *  Copyright (C) 2020 Chosen Few Software
 *  This file is part of FractalCompress.
 *
 *  FractalCompress is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  FractalCompress is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with FractalCompress.  If not, see <https://www.gnu.org/licenses/>.
 */
using System.IO;

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
                (byte)MathExtensions.Clamp(Contrast * 255, 0, 255),
                (byte)MathExtensions.Clamp(Brightness * 255, 0, 255),
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