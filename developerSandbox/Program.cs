using System;
using seekableExtraction.Extractors;
using seekableExtraction.Common;
using System.IO;

namespace developerSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            ExtractorOptions option = new ExtractorOptions();
            option.archive_filepath = @"F:\free mount test\pax1.tar";
            Extractor x = seekableExtraction.AutoExtractor.findExtractor(option);
            if (x != null)
            {
                Console.WriteLine("File is supported. extractor type: " + x.GetType());
                x.Generate_statemap();
                Console.WriteLine($"Has {x.FileList.Count} files in total");
                foreach (VFile file in x.FileList.Values)
                {
                    Console.WriteLine($"Path: {file.AbsolutePath}");
                    byte[] contents = x.Read(file.AbsolutePath, 30, 0).data;
                    Console.WriteLine($"Content: {ByteUtil.To_readable_string(contents)}");
                }
            }
            else
            {
                Console.WriteLine("Sorry, the file is not supported");
            }
            Console.WriteLine("Hello World!");
        }
    }
}
