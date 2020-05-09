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
            if (File.Exists(option.archive_filepath + ".statemap"))
                File.Delete(option.archive_filepath + ".statemap");
            Extractor x = seekableExtraction.AutoExtractor.findExtractor(option);
            if (x != null)
            {
                Console.WriteLine("File is supported. extractor type: " + x.GetType());
                x.Initialize();
                Console.WriteLine($"Has {x.FileList.Count} files in total");
                foreach (vFile file in x.FileList.Values)
                {
                    Console.WriteLine($"Path: {file.AbsolutePath}");
                    byte[] contents = x.Read(file.AbsolutePath, 30, 0).data;
                    Console.WriteLine($"Content: {seekableExtraction.Common.ByteUtil.To_readable_string(contents)}");
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
