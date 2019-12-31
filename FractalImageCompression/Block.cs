using System;
using System.Collections.Generic;
using System.Text;

namespace FractalImageCompression
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
