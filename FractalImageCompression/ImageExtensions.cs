
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

        public static Image[] FromBitmap(SKBitmap bitmap)
        {
            Image[] images = new Image[3]
            {
                    new Image(bitmap.Height, bitmap.Width),
                    new Image(bitmap.Height, bitmap.Width),
                    new Image(bitmap.Height, bitmap.Width)
            };
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);
                    images[0].Data[y, x] = (float)pixel.Red / 255;
                    images[1].Data[y, x] = (float)pixel.Green / 255;
                    images[2].Data[y, x] = (float)pixel.Blue / 255;
                }
            }
            return images;
        }

        public static SKBitmap ToBitmap(Image[] imageData)
        {
            SKBitmap bitmap = new SKBitmap(imageData[0].Width, imageData[0].Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    byte red = (byte)Math.Clamp(imageData[0].Data[y, x] * 255, 0, 255);
                    byte green = (byte)Math.Clamp(imageData[1].Data[y, x] * 255, 0, 255);
                    byte blue = (byte)Math.Clamp(imageData[2].Data[y, x] * 255, 0, 255);
                    bitmap.SetPixel(x, y, new SKColor(red, green, blue));
                }
            }
            return bitmap;
        }
    }
}
