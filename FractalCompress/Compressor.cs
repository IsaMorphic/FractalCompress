using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalCompress
{
    public static class Compressor
    {
        private static List<Block> MakeAllTransforms(Image[,] sourceBlocks, int sourceSize, int destSize)
        {
            int scaleFactor = sourceSize / destSize;
            List<Block> blocks = new List<Block>();
            for (byte y = 0; y < sourceBlocks.GetLength(0); y++)
            {
                for (byte x = 0; x < sourceBlocks.GetLength(1); x++)
                {
                    for (byte reflection = 0; reflection < (byte)Reflection.All; reflection++)
                        for (byte rotation = 0; rotation < (byte)Rotation.All; rotation++)
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

        public static Atom[,] Compress(Image image, int sourceSize, int destSize, float contrast)
        {
            Image[,] sourceBlocks = image.ExtractBlocksOfSize(sourceSize);
            Image[,] destBlocks = image.ExtractBlocksOfSize(destSize);

            List<Block> allTransformedBlocks = MakeAllTransforms(sourceBlocks, sourceSize, destSize);

            Atom[,] compressedData = new Atom[destBlocks.GetLength(0), destBlocks.GetLength(1)];
            Parallel.For(0, destBlocks.GetLength(0), y =>
            {
                Parallel.For(0, destBlocks.GetLength(1), x =>
                {
                    float minDistance = float.PositiveInfinity;

                    Image destBlock = destBlocks[y, x];
                    foreach (Block block in allTransformedBlocks)
                    {
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

        public static Image[] Decompress(Atom[,] compressedData, int sourceSize, int destSize, int iter)
        {
            int scaleFactor = sourceSize / destSize;
            int height = compressedData.GetLength(0) * destSize;
            int width = compressedData.GetLength(1) * destSize;

            List<Image> iterations = new List<Image> { Image.Random(height, width) };
            for (int i = 0; i < iter; i++)
            {
                Image currentImage = new Image(height, width);
                Image[,] sourceBlocks = iterations.Last().ExtractBlocksOfSize(sourceSize);
                Parallel.For(0, compressedData.GetLength(0), y =>
                {
                    Parallel.For(0, compressedData.GetLength(1), x =>
                    {
                        Atom transformData = compressedData[y, x];
                        Image destBlock = sourceBlocks[transformData.Y, transformData.X].Reduce(scaleFactor)
                            .ApplyTransform(transformData.Transform)
                            .Multiply(transformData.Contrast)
                            .Add(transformData.Brightness);
                        currentImage.Insert(x * destSize, y * destSize, destBlock);
                    });
                });
                iterations.Add(currentImage);
            }
            return iterations.ToArray();
        }
    }
}
