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
namespace FractalCompress
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
