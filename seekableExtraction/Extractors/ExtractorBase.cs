using System;
using System.Collections.Generic;
using seekableExtraction.Common;

namespace seekableExtraction.Extractors
{
    public abstract class Extractor
    {
        public event Action<ExtractorEventArgs> InfoHandler, WarningHandler;
        public abstract Dictionary<string, vFolder> FolderList
        {
            get;
        }
        public abstract Dictionary<string, vFile> FileList
        {
            get;
        }
        private ExtractorOptions options;

        public Extractor(ExtractorOptions options = null) {
            if (options == null)
                this.options = new ExtractorOptions();
            else
                this.options = options;

            //The simplified code looks unmaintainable
            //this.options = options == null ? new ExtractorOptions() : options;
        }

        /// <summary>
        /// Read data from file
        /// </summary>
        /// <param name="filepath">Filepath inside archive file to read from</param>
        /// <param name="length">How many bytes to read</param>
        /// <param name="offset">Offset in extracted file</param>
        public virtual (byte[] data, int bytesRead) Read(string filename, int length, long offset)
                                                 => Read(FileList[filename], length, offset);
        public abstract (byte[] data, int bytesRead) Read(vFile file, int length, long offset);


        public abstract bool Load_statemap();
        public abstract bool Write_statemap();
        public abstract bool Generate_statemap(bool checkHeaderIntegrity = true);

        /// <summary>
        /// Check compatibility of file and extractor (in a fast manner).<br/>
        /// If return value if true, means you can use the file in extractor.
        /// The result might not be 100% accurate
        /// </summary>
        public static bool Check_compatibility(ExtractorOptions option) => false;


        /// <summary>
        /// Get Extractor to a state that is ready to Read()
        /// </summary>
        public virtual void Initialize() {
            if (!Load_statemap()) {
                Generate_statemap();
                Write_statemap();
            }
        }


        public virtual void RaiseInfoEvent(string type, string message = null)
        {
            InfoHandler(new ExtractorEventArgs(type, message));
        }
        public virtual void RaiseWarningEvent(string type, string message = null)
        {
            WarningHandler(new ExtractorEventArgs(type, message));
        }
    }

    public class ExtractorOptions {
        public string archive_filepath;
    }

    #region Event
    public class ExtractorEventArgs : EventArgs
    {
        public readonly string type, message;
        public ExtractorEventArgs(string type, string message)
        {
            this.type = type;
            this.message = message;
        }
        public ExtractorEventArgs(string type)
        {
            this.type = type;
            message = null;
        }
    }
    #endregion

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
