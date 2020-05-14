using seekableExtraction.Common;
using seekableExtraction.Extractors;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace developerSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"F:\free mount test\deflate\compressed_optimal_literal_1111111111111111111111111111111111111111111111111111111111111111.txt";
            using (var fs = File.OpenRead(path))
            using (var bs = new BitStream(fs, BitOrder.LeastSignificantFirst))
            {
                for (int i = 0; i < fs.Length; i++)
                {
                    Console.Write($"Byte[{i}]:");
                    for (int j = 0; j < 8; j++)
                        Console.Write(bs.readBit() ? '1' : '0');
                    Console.WriteLine();
                }
            }


            //string path = @"F:\free mount test\deflate\compressed_optimal_literal_1111111111111111111111111111111111111111111111111111111111111111.txt";
            //using (var fs = File.OpenWrite(path))
            //using (var ds = new DeflateStream(fs, CompressionLevel.Optimal))
            //{
            //    var test = Encoding.ASCII.GetBytes("1111111111111111111111111111111111111111111111111111111111111111");
            //    ds.Write(test, 0, test.Length);
            //}


            //ExtractorOptions option = new ExtractorOptions();
            //option.archive_filepath = @"F:\free mount test\test.gz";
            //Extractor x = new Gzip(option);
            //x.Generate_statemap();

            //ExtractorOptions option = new ExtractorOptions();
            //option.archive_filepath = @"F:\free mount test\pax1.tar";
            //Extractor x = seekableExtraction.AutoExtractor.findExtractor(option);
            //if (x != null)
            //{
            //    Console.WriteLine("File is supported. extractor type: " + x.GetType());
            //    x.Generate_statemap();
            //    Console.WriteLine($"Has {x.FileList.Count} files in total");
            //    foreach (VFile file in x.FileList.Values)
            //    {
            //        Console.WriteLine($"Path: {file.AbsolutePath}");
            //        byte[] contents = x.Read(file.AbsolutePath, 30, 0).data;
            //        Console.WriteLine($"Content: {ByteUtil.To_readable_string(contents)}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("Sorry, the file is not supported");
            //}
            //Console.WriteLine("Hello World!");
        }
    }
}
