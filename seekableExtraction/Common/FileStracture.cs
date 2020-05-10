using System;
using System.Collections.Generic;
using System.Text;

namespace seekableExtraction.Common
{
    public class vFolder : vFile {
        public List<vFolder> SubFolders;
        public List<vFile> Files;
        public static vFolder rootFolder {
            get {
                return new vFolder("/");
            }
        }

        /// <param name="Filepath">Full filepath to folder</param>
        /// <param name="UnifyPath">Unify filepath before processing</param>
        public vFolder(string Filepath, long size = 0, bool UnifyPath = true) : base(Filepath, size, UnifyPath)
        {
            SubFolders = new List<vFolder>();
            Files = new List<vFile>();
        }
    }

    public class vFile
    {
        public readonly string AbsolutePath;
        public readonly string Prefix;
        public readonly string Name;
        public readonly long Size;

        /// <param name="Filepath">Full filepath to folder</param>
        /// <param name="UnifyPath">Unify filepath before processing</param>
        public vFile(string Filepath, long size=0, bool UnifyPath = true)
        {
            if (UnifyPath)
                Filepath = Unify_filepath(Filepath);
            
            AbsolutePath = Filepath;
            var pair = Parse_filepath(AbsolutePath);
            Prefix = pair.Prefix;
            Name = pair.Filename;

            Size = size;
        }

        /// <summary>
        /// Given a filepath string, this function returns a filepath that:<br/>
        /// 1. Starts with "/"<br/>
        /// 2. Ends with "/"<br/>
        /// 3. Replaced all "\" with "/"
        /// </summary>
        public static string Unify_filepath(string input) {
            input = input.Replace(@"\", "/");
            input = (input.StartsWith("/") ? "" : "/") +
                     input +
                    (input.EndsWith("/") ? "" : "/");
            return input;
        }

        /// <summary>
        /// Given a filepath string (will be unified before processing), return a tuple in following format:<br/>
        /// (Prefix, Filename)  a Tuple object<br/>
        /// Prefix will starts with and ends with "/"<br/>
        /// Filename doesn't contain any slashes.
        /// </summary>
        public static (string Prefix, string Filename) Parse_filepath(string input)
        {
            string path = Unify_filepath(input);
            if (path == "/")
                return ("/", "");
            else {
                int delimiter_index = path.LastIndexOf('/', path.Length - 2);
                return (path.Substring(0, delimiter_index + 1),
                        path.Substring(delimiter_index + 1, path.Length - delimiter_index - 2));
            }
        }
    }
}
