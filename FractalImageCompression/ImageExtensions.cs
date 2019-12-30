
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

        public static SKBitmap ToBitmap(this Image image)
        {
            SKBitmap bitmap = new SKBitmap(image.Width, image.Height, SKColorType.Gray8, SKAlphaType.Opaque);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    byte pixelColor = (byte)(Math.Min(Math.Max(image.Data[y, x] * 255, 0), 255));
                    bitmap.SetPixel(x, y, new SKColor(pixelColor, pixelColor, pixelColor));
                }
            }
            return bitmap;
        }
    }
}
