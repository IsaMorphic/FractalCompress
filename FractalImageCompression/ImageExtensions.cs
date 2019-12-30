
using SkiaSharp;
using System;

namespace FractalImageCompression
{
    public static class ImageExtensions
    {
        public static Image[,] ExtractBlocksOfSize(this Image image, int blockSize)
        {
            int numBlocksY = image.Height / blockSize;
            int numBlocksX = image.Width / blockSize;
            Image[,] blocks = new Image[numBlocksY, numBlocksX];
            for (int y = 0; y < numBlocksY; y++)
            {
                for (int x = 0; x < numBlocksX; x++)
                {
                    blocks[y, x] = image.SubImage(x * blockSize, y * blockSize, (x + 1) * blockSize, (y + 1) * blockSize);
                }
            }
            return blocks;
        }
    }
}
