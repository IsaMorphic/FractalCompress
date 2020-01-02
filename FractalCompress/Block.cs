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
    public class Block
    {
        public Atom AtomicData { get; }
        public Image Image { get; }

        public Block(byte x, byte y, Transform transform, Image image)
        {
            AtomicData = new Atom(x, y, 1.0f, 0.0f, transform);
            Image = image;
        }

        public Block(Atom atomicData, Image image)
        {
            AtomicData = atomicData;
            Image = image;
        }
    }
}
