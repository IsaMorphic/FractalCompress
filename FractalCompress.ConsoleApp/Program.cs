using CommandLine;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FractalCompress.ConsoleApp
{
    public class Options
    {
        [Option('i', "inputfile", HelpText = "Path to a file that serves as input. May be either compressed or uncompressed.", Required = true)]
        public string InputFile { get; set; }

        [Option('o', "outputfile", HelpText = "Path to a file that serves as output. Depending on whether inputfile is compressed, output will be either compressed or decompressed.", Required = true)]
        public string OutputFile { get; set; }

        [Option('d', "domainsize", HelpText = "Specifies the square size of domain blocks. Should be a factor of rangeblocks and no lower than 1.", Required = true)]
        public int DomainSize { get; set; }

        [Option('r', "rangesize", HelpText = "Specifies the square size of range blocks. Should be a factor of both the width and height and now lower than 1.", Required = true)]
        public int RangeSize { get; set; }

        [Option('n', "numiter", HelpText = "Specifies how many times to iterate affine transformations during decompression. Should be no lower than 1. Lower values result in a noisier image, higher values result in longer decompression time.", Required = false, Default = 8)]
        public int Iterations { get; set; }

        [Option('c', "contrast", HelpText = "During compression, specifies how much detail may be retained in the image. (Should be between 0 and 1. Higher values result in retention of finer details, but increases amount of noise)", Required = false, Default = 0.25f)]
        public float Contrast { get; set; }
    }

    class Program
    {
        static Atom[][,] CompressRGBBitmap(SKBitmap bitmap, int sourceSize, int destSize, float contrast)
        {
            Image[] originalData = ImageExtensions.FromBitmap(bitmap);

            Atom[][,] compressedData = new Atom[3][,];
            compressedData[0] = Compressor.Compress(originalData[0], sourceSize, destSize, contrast);
            compressedData[1] = Compressor.Compress(originalData[1], sourceSize, destSize, contrast);
            compressedData[2] = Compressor.Compress(originalData[2], sourceSize, destSize, contrast);

            return compressedData;
        }

        static SKBitmap DecompressRGBBitmap(Atom[][,] compressedData, int sourceSize, int destSize, int iter)
        {
            Image[] decompressed = new Image[3]
            {
                Compressor.Decompress(compressedData[0], sourceSize, destSize, iter).Last(),
                Compressor.Decompress(compressedData[1], sourceSize, destSize, iter).Last(),
                Compressor.Decompress(compressedData[2], sourceSize, destSize, iter).Last()
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
                try
                {
                    Atom[][,] compressedData = CompressRGBBitmap(inputBitmap, options.RangeSize, options.DomainSize, options.Contrast);
                    WriteCompressedDataToFile(compressedData, options.OutputFile);
                    inputBitmap.Dispose();
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occured while compressing the image. Double check your parameters and try again.");
                }
            }
            else
            {
                try
                {
                    Atom[][,] compressedData = ReadCompressedDataFromFile(options.InputFile);
                    using (SKBitmap outputBitmap = DecompressRGBBitmap(compressedData, options.RangeSize, options.DomainSize, options.Iterations))
                    using (SKFileWStream fileHandle = new SKFileWStream(options.OutputFile))
                    {
                        SKPixmap.Encode(fileHandle, outputBitmap, SKEncodedImageFormat.Png, 100);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occured while decompressing the image. Double check your parameters and try again.");
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
