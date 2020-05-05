# Seekable Extraction

An attempt to create c# library that can random read from archive file.

This is an attemp to solve a question like  
  I currently have 10GB text file (pi.txt) that's in a archive file.  
  How to read 654,321,000th byte to 654,333,000th byte efficiently?

I Googled but I didn't get what I want.

# Example usage.

This code extracts all file content inside a archive file (currently only tar is supported)  
With a little bit of tweaking, you can extract certain bytes of file inside archive file.  
Note that a statemap will be generated... a experimental file format that I'm working on.

	ExtractorOptions option = new ExtractorOptions();
	option.archive_filepath = @"F:\free mount test\test_files\test_files.tar";
	
	Extractor x = seekableExtraction.AutoExtractor.findExtractor(option);
	if (x != null)
	{
		Console.WriteLine("File is supported. extractor type: " + x.GetType());
		x.Initialize();
		Console.WriteLine($"Has {x.FileList.Count} files in total");
		foreach (vFile file in x.FileList.Values) {
			Console.WriteLine($"Path: {file.AbsolutePath}");
			byte[] contents = x.Read(file.AbsolutePath, (int) file.Size, 0).data;
			Console.WriteLine($"Content: {BitConverter.ToString(contents)}");
		}
	}
	else {
		Console.WriteLine("Sorry, the file is not supported");
	}