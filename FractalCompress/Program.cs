using CommandLine;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FractalCompress
{
    public class Options
    {
        [Option('i', "inputfile", Required = true)]
        public string InputFile { get; set; }

        [Option('o', "outputfile", Required = true)]
        public string OutputFile { get; set; }

        [Option('d', "domainsize", Required = true)]
        public int DomainSize { get; set; }

        [Option('r', "rangesize", Required = true)]
        public int RangeSize { get; set; }

        [Option('n', "iter", Required = false, Default = 8)]
        public int Iterations { get; set; }

        [Option('c', "contrast", Required = false, Default = 0.125f)]
        public float Contrast { get; set; }
    }

    class Program
    {
        static List<Block> MakeAllTransforms(Image[,] sourceBlocks, int sourceSize, int destSize)
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

        static Atom[,] Compress(Image image, int sourceSize, int destSize, float contrast)
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

        static Image[] Decompress(Atom[,] compressedData, int sourceSize, int destSize, int iter)
        {
            int scaleFactor = sourceSize / destSize;
            int height = compressedData.GetLength(0) * destSize;
            int width = compressedData.GetLength(1) * destSize;

            List<Image> iterations = new List<Image> { Image.Random(height, width) };
            for (int i = 0; i < iter; i++)
            {
                Image currentImage = new Image(height, width);
                Image[,] sourceBlocks = iterations.Last().ExtractBlocksOfSize(sourceSize);
                for (int y = 0; y < compressedData.GetLength(0); y++)
                {
                    for (int x = 0; x < compressedData.GetLength(1); x++)
                    {
                        Atom transformData = compressedData[y, x];
                        Image destBlock = sourceBlocks[transformData.Y, transformData.X].Reduce(scaleFactor)
                            .ApplyTransform(transformData.Transform)
                            .Multiply(transformData.Contrast)
                            .Add(transformData.Brightness);
                        currentImage.Insert(x * destSize, y * destSize, destBlock);
                    }
                }
                iterations.Add(currentImage);
            }
            return iterations.ToArray();
        }

        static Atom[][,] CompressRGBBitmap(SKBitmap bitmap, int sourceSize, int destSize, float contrast)
        {
            Image[] originalData = ImageExtensions.FromBitmap(bitmap);

            Atom[][,] compressedData = new Atom[3][,];
            compressedData[0] = Compress(originalData[0], sourceSize, destSize, contrast);
            compressedData[1] = Compress(originalData[1], sourceSize, destSize, contrast);
            compressedData[2] = Compress(originalData[2], sourceSize, destSize, contrast);

            return compressedData;
        }

        static SKBitmap DecompressRGBBitmap(Atom[][,] compressedData, int sourceSize, int destSize, int iter)
        {
            Image[] decompressed = new Image[3]
            {
                Decompress(compressedData[0], sourceSize, destSize, iter).Last(),
                Decompress(compressedData[1], sourceSize, destSize, iter).Last(),
                Decompress(compressedData[2], sourceSize, destSize, iter).Last()
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
                    writer.WriteLine(compressedData[0].GetLength(1));
            }
        }

        static void Run(Options options)
        {
            SKBitmap inputBitmap = SKBitmap.Decode(options.InputFile);
            if (inputBitmap != null)
            {
                Atom[][,] compressedData = CompressRGBBitmap(inputBitmap, options.RangeSize, options.DomainSize, options.Contrast);
                WriteCompressedDataToFile(compressedData, options.OutputFile);
                inputBitmap.Dispose();
            }
            else
            {
                Atom[][,] compressedData = ReadCompressedDataFromFile(options.InputFile);
                using (SKBitmap outputBitmap = DecompressRGBBitmap(compressedData, options.RangeSize, options.DomainSize, options.Iterations))
                using (SKFileWStream fileHandle = new SKFileWStream(options.OutputFile))
                {
                    SKPixmap.Encode(fileHandle, outputBitmap, SKEncodedImageFormat.Png, 100);
                }
            }
        }

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            result.WithParsed(Run);
        }
    }
}
