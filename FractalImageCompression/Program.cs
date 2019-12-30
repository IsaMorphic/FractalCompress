using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

namespace FractalImageCompression
{
    class Program
    {
        const int SRC_SIZE = 8;
        const int DST_SIZE = 4;

        static Image LoadImage(string path)
        {
            using (SKBitmap bitmap = SKBitmap.Decode(path))
            {
                Image newImage = new Image(bitmap.Height, bitmap.Width);
                for (int y = 0; y < newImage.Height; y++)
                {
                    for (int x = 0; x < newImage.Width; x++)
                    {
                        SKColor pixel = bitmap.GetPixel(x, y);
                        float avg = (pixel.Red + pixel.Green + pixel.Blue) / 3;
                        newImage.Data[y, x] = avg / 255;
                    }
                }
                return newImage;
            }
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
            for (int y = 0; y < destBlocks.GetLength(0); y++)
            {
                for (int x = 0; x < destBlocks.GetLength(1); x++)
                {
                    float minDistance = float.PositiveInfinity;

                    Image destBlock = destBlocks[y, x];
                    foreach (Block block in allTransformedBlocks)
                    {
                        float contrast = 0.75f;
                        float brightness = destBlock.Add(block.Image.Multiply(-contrast)).Average();

                        float distance = destBlock.DistanceTo(block.Image.Multiply(contrast).Add(brightness));
                        if (distance < minDistance)
                        {
                            Atom data = block.AtomicData;
                            compressedData[y, x] = new Atom(data.X, data.Y, contrast, brightness, data.Transform);
                            minDistance = distance;
                        }
                    }
                }
            }
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
            Image original = LoadImage(args[0]);
            Atom[,] compressedData = Compress(original);
            Image decompressed = Decompress(compressedData, 16).Last();

            using (SKFileWStream fileHandle = new SKFileWStream("output.png"))
            using (SKBitmap bitmap = decompressed.ToBitmap())
            {
                SKPixmap.Encode(fileHandle, bitmap, SKEncodedImageFormat.Png, 100);
            }
        }
    }
}
