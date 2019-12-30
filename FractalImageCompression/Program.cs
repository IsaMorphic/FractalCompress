using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

namespace FractalImageCompression
{
    class Program
    {
        const int SRC_SIZE = 4;
        const int DST_SIZE = 2;
        const int ITERATIONS = 8;
        const float CONTRAST = 0.25f;

        static Image[] LoadImage(string path)
        {
            using (SKBitmap bitmap = SKBitmap.Decode(path))
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
        }

        static SKBitmap ToBitmap(Image[] imageData)
        {
            SKBitmap bitmap = new SKBitmap(imageData[0].Width, imageData[0].Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    byte red = (byte)(Math.Min(Math.Max(imageData[0].Data[y, x] * 255, 0), 255));
                    byte green = (byte)(Math.Min(Math.Max(imageData[1].Data[y, x] * 255, 0), 255));
                    byte blue = (byte)(Math.Min(Math.Max(imageData[2].Data[y, x] * 255, 0), 255));
                    bitmap.SetPixel(x, y, new SKColor(red, green, blue));
                }
            }
            return bitmap;
        }

        static List<Block> MakeAllTransforms(Image[,] sourceBlocks)
        {
            int scaleFactor = SRC_SIZE / DST_SIZE;
            List<Block> blocks = new List<Block>();
            for (int y = 0; y < sourceBlocks.GetLength(0); y++)
            {
                for (int x = 0; x < sourceBlocks.GetLength(1); x++)
                {
                    for (int reflection = 0; reflection < (int)Reflection.All; reflection++)
                        for (int rotation = 0; rotation < (int)Rotation.All; rotation++)
                        {
                            Transform transform = new Transform((Reflection)reflection, (Rotation)rotation);
                            Image transformedBlock = sourceBlocks[y, x]
                                .Reduce(scaleFactor)
                                .ApplyTransform(transform);
                            blocks.Add(new Block(x, y, transform, transformedBlock));
                        }
                }
            }
            return blocks;
        }

        static Atom[,] Compress(Image image)
        {
            Image[,] sourceBlocks = image.ExtractBlocksOfSize(SRC_SIZE);
            Image[,] destBlocks = image.ExtractBlocksOfSize(DST_SIZE);

            List<Block> allTransformedBlocks = MakeAllTransforms(sourceBlocks);

            Atom[,] compressedData = new Atom[destBlocks.GetLength(0), destBlocks.GetLength(1)];
            Parallel.For(0, destBlocks.GetLength(0), y =>
            {
                Parallel.For(0, destBlocks.GetLength(1), x =>
                {
                    Console.WriteLine($"Compressing Cell: [{y}, {x}]");
                    float minDistance = float.PositiveInfinity;

                    Image destBlock = destBlocks[y, x];
                    foreach (Block block in allTransformedBlocks)
                    {
                        float contrast = CONTRAST;
                        float brightness = destBlock.Add(block.Image.Multiply(-contrast)).Average();

                        float distance = destBlock.DistanceTo(block.Image.Multiply(contrast).Add(brightness));
                        if (distance < minDistance)
                        {
                            Atom data = block.AtomicData;
                            compressedData[y, x] = new Atom(data.X, data.Y, contrast, brightness, data.Transform);
                            minDistance = distance;
                        }
                    }
                });
            });
            return compressedData;
        }

        static Image[] Decompress(Atom[,] compressedData, int iter)
        {
            int scaleFactor = SRC_SIZE / DST_SIZE;
            int height = compressedData.GetLength(0) * DST_SIZE;
            int width = compressedData.GetLength(1) * DST_SIZE;

            List<Image> iterations = new List<Image> { Image.Random(height, width) };
            for (int i = 0; i < iter; i++)
            {
                Image currentImage = new Image(height, width);
                Image[,] sourceBlocks = iterations.Last().ExtractBlocksOfSize(SRC_SIZE);
                for (int y = 0; y < compressedData.GetLength(0); y++)
                {
                    for (int x = 0; x < compressedData.GetLength(1); x++)
                    {
                        Atom transformData = compressedData[y, x];
                        Image destBlock = sourceBlocks[transformData.Y, transformData.X].Reduce(scaleFactor)
                            .ApplyTransform(transformData.Transform)
                            .Multiply(transformData.Contrast)
                            .Add(transformData.Brightness);
                        currentImage.Insert(x * DST_SIZE, y * DST_SIZE, destBlock);
                    }
                }
                iterations.Add(currentImage);
            }
            return iterations.ToArray();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Loading base image...");
            Image[] originalData = LoadImage(args[0]);

            Console.WriteLine("Base image loaded, beginning compression...");
            Atom[][,] compressedData = new Atom[3][,];

            Console.WriteLine("Compressing red channel...");
            compressedData[0] = Compress(originalData[0]);

            Console.WriteLine("Compressing green channel...");
            compressedData[1] = Compress(originalData[1]);

            Console.WriteLine("Compressing blue channel...");
            compressedData[2] = Compress(originalData[2]);

            Console.WriteLine("Compression completed, beginning decompression...");
            Image[] decompressed = new Image[3]
            {
                Decompress(compressedData[0], ITERATIONS).Last(),
                Decompress(compressedData[1], ITERATIONS).Last(),
                Decompress(compressedData[2], ITERATIONS).Last()
            };
            Console.WriteLine("Image decompressed successfully!");

            using (SKFileWStream fileHandle = new SKFileWStream("output.png"))
            using (SKBitmap bitmap = ToBitmap(decompressed))
            {
                Console.WriteLine("Writing decompressed image to file...");
                SKPixmap.Encode(fileHandle, bitmap, SKEncodedImageFormat.Png, 100);
            }
        }
    }
}
