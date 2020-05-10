using System;
using System.Collections.Generic;
using seekableExtraction.Common;

namespace seekableExtraction.Extractors
{
    public abstract class Extractor
    {
        public abstract Dictionary<string, VFolder> FolderList
        {
            get;
        }
        public abstract Dictionary<string, VFile> FileList
        {
            get;
        }
        private ExtractorOptions options;

        public Extractor(ExtractorOptions options = null)
        {
            if (options == null)
                this.options = new ExtractorOptions();
            else
                this.options = options;
        }

        /// <summary>
        /// Read data from file
        /// </summary>
        /// <param name="filepath">Filepath inside archive file to read from</param>
        /// <param name="length">How many bytes to read</param>
        /// <param name="offset">Offset in extracted file</param>
        public virtual (byte[] data, int bytesRead) Read(string filename, int length, long offset)
                                                 => Read(FileList[filename], length, offset);
        public abstract (byte[] data, int bytesRead) Read(VFile file, int length, long offset);


        public abstract bool Load_statemap();
        public abstract bool Write_statemap();
        public abstract bool Generate_statemap();

        /// <summary>
        /// Check compatibility of file and extractor (in a fast manner).<br/>
        /// If return value if true, means you can use the file in extractor.<br/>
        /// The result might not be 100% accurate (up to real implementation)
        /// </summary>
        public static bool Check_compatibility(ExtractorOptions option) => false;


        /// <summary>
        /// Get Extractor to a state that is ready to Read()
        /// </summary>
        public virtual void Initialize()
        {
            if (!Load_statemap())
            {
                Generate_statemap();
                Write_statemap();
            }
        }
    }

    public class ExtractorOptions
    {
        public string archive_filepath = "", statemap_filepath = "";
    }

    #region Exceptions

    public class NotSupportedException : Exception
    {
        public NotSupportedException(string message) : base(message)
        {

        }
        public NotSupportedException() : base()
        {

        }
    }

    public class ParseErrorException : Exception
    {
        public ParseErrorException(string message) : base(message)
        {

        }
        public ParseErrorException() : base()
        {

        }
    }

    public class ReadErrorException : Exception
    {
        public ReadErrorException(string message) : base(message)
        {

        }
        public ReadErrorException() : base()
        {

        }
    }

    class FileCorruoptedException : Exception
    {
        public FileCorruoptedException(string message) : base(message)
        {

        }
        public FileCorruoptedException() : base()
        {

        }
    }

    #endregion
}
