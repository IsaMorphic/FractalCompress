using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FractalImageCompression
{
    class Program
    {
        const int SRC_SIZE = 8;
        const int DST_SIZE = 4;
        const int ITERATIONS = 8;
        const float CONTRAST = 0.125f;

        static List<Block> MakeAllTransforms(Image[,] sourceBlocks)
        {
            int scaleFactor = SRC_SIZE / DST_SIZE;
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

        static Atom[][,] CompressRGBBitmap(SKBitmap bitmap)
        {
            Image[] originalData = ImageExtensions.FromBitmap(bitmap);

            Atom[][,] compressedData = new Atom[3][,];
            compressedData[0] = Compress(originalData[0]);
            compressedData[1] = Compress(originalData[1]);
            compressedData[2] = Compress(originalData[2]);

            return compressedData;
        }

        static SKBitmap DecompressRGBBitmap(Atom[][,] compressedData)
        {
            Image[] decompressed = new Image[3]
            {
                Decompress(compressedData[0], ITERATIONS).Last(),
                Decompress(compressedData[1], ITERATIONS).Last(),
                Decompress(compressedData[2], ITERATIONS).Last()
            };
            return ImageExtensions.ToBitmap(decompressed);
        }

        static Atom[,] ReadCompressedChannelFromStream(Stream stream, int width)
        {
            List<Atom> atoms = new List<Atom>();
            bool readSuccessful = false;
            do
            {
                readSuccessful = Atom.Deserialize(stream, out Atom atom);
                atoms.Add(atom);
            } while (readSuccessful);

            Atom[,] channel = new Atom[atoms.Count / width, width];
            for (int y = 0; y < atoms.Count / width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    channel[y, x] = atoms[y * width + x];
                }
            }
            return channel;
        }

        static Atom[][,] ReadCompressedDataFromFile(string path)
        {
            using (FileStream file = File.OpenRead(path))
            using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                int width;
                Atom[][,] compressedData = new Atom[3][,];

                var spec = archive.GetEntry("spec");
                using (StreamReader reader = new StreamReader(spec.Open()))
                    width = int.Parse(reader.ReadLine());

                var red = archive.GetEntry("red");
                using (Stream stream = red.Open())
                    compressedData[0] = ReadCompressedChannelFromStream(stream, width);

                var green = archive.GetEntry("green");
                using (Stream stream = green.Open())
                    compressedData[1] = ReadCompressedChannelFromStream(stream, width);

                var blue = archive.GetEntry("blue");
                using (Stream stream = blue.Open())
                    compressedData[2] = ReadCompressedChannelFromStream(stream, width);

                return compressedData;
            }
        }

        static void WriteCompressedChannelToStream(Atom[,] channel, Stream stream)
        {
            foreach (Atom atom in channel)
            {
                byte[] data = atom.Serialize();
                stream.Write(data, 0, data.Length);
            }
        }

        static void WriteCompressedDataToFile(Atom[][,] compressedData, string path)
        {
            using (FileStream file = File.Create(path))
            using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                var red = archive.CreateEntry("red");
                using (Stream stream = red.Open())
                    WriteCompressedChannelToStream(compressedData[0], stream);

                var green = archive.CreateEntry("green");
                using (Stream stream = green.Open())
                    WriteCompressedChannelToStream(compressedData[1], stream);

                var blue = archive.CreateEntry("blue");
                using (Stream stream = blue.Open())
                    WriteCompressedChannelToStream(compressedData[2], stream);

                var spec = archive.CreateEntry("spec");
                using (StreamWriter writer = new StreamWriter(spec.Open()))
                    writer.WriteLine(compressedData[0].GetLength(0));
            }
        }

        static void Main(string[] args)
        {
            SKBitmap inputBitmap = SKBitmap.Decode(args[0]);
            if (inputBitmap != null)
            {
                Atom[][,] compressedData = CompressRGBBitmap(inputBitmap);
                WriteCompressedDataToFile(compressedData, "compressed.frc");
                inputBitmap.Dispose();
            }
            else
            {
                Atom[][,] compressedData = ReadCompressedDataFromFile(args[0]);
                using (SKFileWStream fileHandle = new SKFileWStream("output.png"))
                using (SKBitmap outputBitmap = DecompressRGBBitmap(compressedData))
                {
                    SKPixmap.Encode(fileHandle, outputBitmap, SKEncodedImageFormat.Png, 100);
                }
            }
        }
    }
}
