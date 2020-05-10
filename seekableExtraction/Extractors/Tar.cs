using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using seekableExtraction.Common;

namespace seekableExtraction.Extractors
{
    /* Documents to read:
     * https://www.gnu.org/software/tar/manual/html_node/Formats.html
     * https://en.wikipedia.org/wiki/Tar_(computing)
     * https://www.gnu.org/software/tar/manual/html_node/Standard.html
     * https://manpages.debian.org/testing/libarchive-dev/tar.5.en.html
     * http://www.open-std.org/jtc1/sc22/open/n4217.pdf#page=2986 (PAX header section in POSIX standard)
     */
    public class Tar : Extractor
    {
        const int min_tar_size = 1024; //A valid non-multi-volume tar file should have at least 1024 bytes

        string filepath, statemapPath;
        Dictionary<string, TarState> states;
        Dictionary<string, VFolder> folderList;
        Dictionary<string, VFile> fileList;
        TarState current_state;
        public Tar(ExtractorOptions options) : base(options)
        {
            if (new FileInfo(options.archive_filepath).Length < min_tar_size)
                throw new NotSupportedException("The provided tar file is invalid");
            filepath = options.archive_filepath;
            if (options.statemap_filepath == "")
                options.statemap_filepath = filepath + ".statemap";
            statemapPath = options.statemap_filepath;
            states = new Dictionary<string, TarState>();
            folderList = new Dictionary<string, VFolder>();
            folderList.Add("/", VFolder.RootFolder);
            fileList = new Dictionary<string, VFile>();
        }
        public override Dictionary<string, VFile> FileList => fileList;
        public override Dictionary<string, VFolder> FolderList => folderList;

        public override (byte[], int) Read(VFile file, int length, long offset)
        {
            //Prevent overflow to say... next tar header
            int actual_read_length = Math.Min((int)(file.Size - offset), length);

            byte[] buffer = new byte[length];
            int bytesread = 0;
            TarState state = states[file.AbsolutePath];

            using (FileStream fs = File.OpenRead(filepath))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                reader.BaseStream.Position = state.offset_in_tar_file + offset;
                bytesread = reader.Read(buffer, bytesread, actual_read_length);
            }
            return (buffer, bytesread);
        }

        public override bool Generate_statemap()
        {
            //Long filepath related variables, longPathName is used as cache for long filepath readed.
            bool hasLongFilepath = false;
            string stored_Longname = "";
            //PAX Extended filename
            bool hasPAXFileName = false;
            string stored_PAX_Filename = "";
            //PAX Global extended filename
            bool hasPAXGlobalFileName = false;
            string stored_PAX_Global_Filename = "";

            using (FileStream fs = File.OpenRead(filepath))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                while (!Skip_emprty_blocks(reader) &&
                        reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    long start_position = reader.BaseStream.Position;

                    //check header integrity
                    if (!verify_header(reader))
                        throw new FileCorruoptedException(
                            $"The provided tar file might be corrupted (Header position: 0x{reader.BaseStream.Position.ToString("X")})");
                    reader.BaseStream.Position = start_position;

                    current_state = new TarState();
                    current_state.fullFilepath = ByteUtil.To_readable_string(reader.ReadBytes(100)).Trim('\0');

                    if (states.ContainsKey(current_state.fullFilepath))
                        throw new ParseErrorException("Dupelicated filepath found in tar file, There is currently no plan to support hardlink/softlink");

                    //Skip metadata that has no use to this program
                    //File mode, Owner's numeric user ID and Group's numeric user ID
                    reader.BaseStream.Position += 24;

                    //File size
                    current_state.size = Read_file_size(reader);

                    //Skip metadata that has no use to this program
                    //Last modification time in numeric Unix time format (octal)  and  Checksum for header record
                    reader.BaseStream.Position += 20;

                    //Type flag
                    char type_flag = (char)reader.ReadByte();
                    if (type_flag == '\0') type_flag = TarType.File;

                    if (type_flag == TarType.File || //File
                        type_flag == TarType.Directory || //Directory
                        type_flag == TarType.GNU_Longname || //GNU_Longname
                        type_flag == TarType.PAX_Extended_header || //PAX Extended header
                        type_flag == TarType.PAX_Global_extended_header) //PAX Global extended header
                        current_state.type = type_flag;
                    else
                        throw new NotSupportedException("This Tar file is currently not supported, type:" + type_flag);

                    //Skip metadata that has no use to this program
                    //Name of linked file
                    reader.BaseStream.Position += 100;

                    //This is the end of v7 header

                    //Check for ustar marker
                    if (ByteUtil.Encode_to_string(reader.ReadBytes(6), ' ') == "75 73 74 61 72 00")
                    {
                        //Skip metadata that has no use to this program
                        //UStar version "00", Owner user name, Owner group name, Device major number and Device minor number
                        reader.BaseStream.Position += 82;
                        //filepath prefix
                        current_state.fullFilepath = ByteUtil.To_readable_string(reader.ReadBytes(155)).Trim('\0') + current_state.fullFilepath;
                        if (states.ContainsKey(current_state.fullFilepath))
                            throw new ParseErrorException("Dupelicated filepath found in tar file, There is currently no plan to support hardlink/softlink");
                    }

                    //Restore saved file name if there is any.
                    if (hasLongFilepath)
                    {
                        current_state.fullFilepath = stored_Longname;
                        hasLongFilepath = false;
                    }
                    else if (hasPAXFileName)
                    {
                        current_state.fullFilepath = stored_PAX_Filename;
                        hasPAXFileName = false;
                    }
                    else if (hasPAXGlobalFileName)
                    {
                        current_state.fullFilepath = stored_PAX_Global_Filename;
                    }

                    //final processing
                    current_state.fullFilepath = VFile.Unify_filepath(current_state.fullFilepath);
                    long roundup_filesize = current_state.size;
                    if (current_state.size % 512 > 0)
                        roundup_filesize += 512 - current_state.size % 512;

                    current_state.offset_in_tar_file = start_position + 512; //512 == size of tar header roundup
                    reader.BaseStream.Position = current_state.offset_in_tar_file;
                    if (type_flag == TarType.GNU_Longname)
                    {
                        //read longPathName before we skip it.
                        hasLongFilepath = true;
                        //TODO: support long path loger than 2147483647 bytes (aka 2147483647 UTF-8 characters) (I doubt I will do this in a decade)
                        stored_Longname = ByteUtil.To_readable_string(reader.ReadBytes((int)current_state.size)).Trim('\0');
                    }
                    else if (type_flag == TarType.PAX_Extended_header)
                    {
                        //PAX Extended header
                        (bool, string) result = read_PAX_header(reader, current_state.size);
                        hasPAXFileName = result.Item1;
                        stored_PAX_Filename = result.Item2;
                    }
                    else if (type_flag == TarType.PAX_Global_extended_header)
                    {
                        //PAX Global extended header
                        (bool, string) result = read_PAX_header(reader, current_state.size);
                        hasPAXGlobalFileName = result.Item1;
                        stored_PAX_Global_Filename = result.Item2;
                    }
                    else
                    {
                        states.Add(current_state.fullFilepath, current_state);
                    }
                    reader.BaseStream.Position = current_state.offset_in_tar_file + roundup_filesize;


                    //Register the folder/file we read.
                    if (type_flag == TarType.Directory)
                    {
                        //This is a folder
                        VFolder folder = new VFolder(current_state.fullFilepath, current_state.size);
                        folderList.Add(folder.AbsolutePath, folder);
                    }
                    else if (type_flag == TarType.File)
                    {
                        //This is a file
                        VFile file = new VFile(current_state.fullFilepath, current_state.size);
                        folderList[file.Prefix].Files.Add(file);
                        fileList.Add(file.AbsolutePath, file);
                    }
                    else if (type_flag != TarType.GNU_Longname &&
                             type_flag != TarType.PAX_Extended_header &&
                             type_flag != TarType.PAX_Global_extended_header)
                    {
                        //It is also not other supported flag???
                        throw new FileCorruoptedException($"Unknown type flag detected {(char)type_flag}, it's probably (99.9%) due to developer not taken care of the code.");
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Verify Integrity of tar v7 header, Stream POSITION WILL NOT BE RESTORED after integrity check.
        /// </summary>
        /// <returns>
        /// true if verification has passed, false if not (indicates that file might be corrupted)
        /// </returns>
        bool verify_header(BinaryReader reader)
        {
            long initial_position = reader.BaseStream.Position;
            //The bytes that is supposed to represent checksum is to be seem as " " (ascii space char, decimal value 32) during calculation.
            //The initial value of checksum (256) has accounted for those 8 bytes
            int checksum = 256, expected_checksum;

            //v7 header
            if (reader.BaseStream.Position + 257 > reader.BaseStream.Length)
                throw new FileCorruoptedException("There is not enough space to fit header in this tar file, assume the file as corrupted");
            for (int i = 0; i < 148; i++)
                checksum += reader.ReadByte();
            reader.BaseStream.Position += 1;
            expected_checksum = (int)NumberUtil.Bytes_to_number(reader.ReadBytes(6), 8);
            reader.BaseStream.Position += 1;

            for (long i = reader.BaseStream.Position; i < initial_position + 512; i++)
                checksum += reader.ReadByte();

            reader.BaseStream.Position = initial_position;
            return expected_checksum == checksum;
        }

        public override bool Load_statemap()
        {
            states.Clear();
            fileList.Clear();
            folderList.Clear();
            folderList.Add("/", VFolder.RootFolder);
            if (!File.Exists(statemapPath))
                return false;
            using (StreamReader reader = File.OpenText(statemapPath))
            {
                string s;
                while ((s = reader.ReadLine()) != null)
                {
                    if (s == "" || s.StartsWith("#")) continue;
                    string[] rawStates = s.Split('\0');
                    TarState state = new TarState();
                    state.type = rawStates[0][0];
                    state.fullFilepath = rawStates[1];
                    state.offset_in_tar_file = long.Parse(rawStates[2]);
                    state.size = long.Parse(rawStates[3]);

                    if (state.type == '0')
                        fileList.Add(state.fullFilepath, new VFile(state.fullFilepath, state.size));
                    else
                        folderList.Add(state.fullFilepath, new VFolder(state.fullFilepath, state.size));

                    states.Add(state.fullFilepath, state);
                }
            }

            return true;
        }
        public override bool Write_statemap()
        {
            using (StreamWriter writer = File.CreateText(statemapPath))
            {
                writer.WriteLine("# This is a statemap file that helps to read contents of a tar file");
                writer.WriteLine("# In a format that is");
                writer.WriteLine("# (filetype)(null)(fullfilepath)(null)(offset)(null)(filesize)");
                writer.WriteLine("# filetype 0 means normal file, 5 means folder");
                writer.WriteLine("# (null) means real null character \0(\\0)");
                writer.WriteLine("# Please don't modify this file or programs that depends on it might crash");
                foreach (TarState ts in states.Values)
                    writer.WriteLine(ts);
            }
            return true;
        }

        /// <summary>
        /// Reads next 512 bytes of BinaryReader and check for null character<br/>
        /// If there is any non-null character, the stream position is set back to what it originally was.<br/>
        /// otherwise the stream position will skip to next tar block.
        /// </summary>
        /// <returns>If there are skipped block, it returns true, otherwise false.</returns>
        bool Skip_emprty_blocks(BinaryReader reader)
        {
            if (reader.BaseStream.Position + 512 < reader.BaseStream.Length)
            {
                long initial_position = reader.BaseStream.Position;
                for (int i = 0; i < 512; i++)
                {
                    if (reader.ReadByte() != '\0')
                    {
                        reader.BaseStream.Position = initial_position;
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        long Read_file_size(BinaryReader reader)
        {
            try
            {
                if ((reader.ReadByte() & 0x80) == 0)
                {
                    //Normal file size format (<= 8GB)
                    reader.BaseStream.Position--;
                    return NumberUtil.Bytes_to_number(reader.ReadBytes(12), 8);
                }
                else
                {
                    //Special file size format
                    //First bit in the first byte is used to indicate is this size format is in use
                    //Then the rest of 95 bits is size in Binary, so on paper it supports up to Math.Pow(2,96)-1 byte file
                    //I could use BigInteger, but I think it's fine to leave size in long
                    //(Until some supercomputer needs to read a tar file that contains single file that exceeds 8PB file size????)
                    //(With code that they found online randomly???????????????????????????????)
                    //(Or maybe I underestimated how storage technology advances on consumer/prosumer grade hardware, if that happened before I quit coding for good or died, I can fix it by breaking backward compatibility)
                    reader.BaseStream.Position--;
                    byte[] encodedSize = reader.ReadBytes(12);
                    long size = 0;
                    encodedSize[0] &= 0x7F;
                    //This code is specifically coded to throw OverflowException when ....... the code doesn't work
                    //I can use bitwise operator but there will be no exception thrown
                    //which may cause incorrect file size (e.g. nagative or 0)
                    for (int i = 0; i < 12; i++)
                        size += (long)Math.Pow(256, 11 - i) * encodedSize[i];
                    return size;
                }
            }
            catch (OverflowException)
            {
                throw new NotSupportedException("Looks like you are working with very large tar file (>8PiB) (assuming there is no other bug), I am sorry to say this program doesn't support it.");
            }
        }

        /// <summary>
        /// Parse PAX header and return filepath encoded in PAX format if detected.<br/>
        /// This function assume "reader" passed in is at the beginning of a PAX header.<br/>
        /// WILL NOT reset reader.BaseStream's position in any case<br/>
        /// WARNING: This parser is not a complete implementation
        /// </summary>
        (bool containsFilepath, string filepath) read_PAX_header(BinaryReader reader, long total_header_length)
        {
            long initial_position = reader.BaseStream.Position;
            long readed_bytes = 0;

            while (readed_bytes < total_header_length)
            {
                //A header format is
                //"<length> <keyword>=<value>\n"
                //<Length> is a number represented in ascii, a signed integer.
                //<keyword> is a string that only contains lower case letters and digits
                //<value> is a string that can have arbitary data.
                //(on paper it might be encoded in something other than UTF-8, but there is currently no plan to support charset)

                //literally a char variable to hold a character, I can't came up with a good variable name
                char _char;

                //Parse <Length>
                StringBuilder Length_in_string = new StringBuilder();
                while ((_char = (char)reader.ReadByte()) != ' ')
                    if (!char.IsControl(_char))
                        Length_in_string.Append(_char);
                    else
                        throw new FileCorruoptedException($"Invalid PAX header detected, position 0x{(initial_position + readed_bytes).ToString("X")}");
                int Length = int.Parse(Length_in_string.ToString());

                //Parse <keyword>
                StringBuilder Keyword = new StringBuilder();
                while ((_char = (char)reader.ReadByte()) != '=')
                    if (char.IsLower(_char) || char.IsDigit(_char))
                        Keyword.Append(_char);
                    else
                        throw new FileCorruoptedException($"Invalid PAX header detected, position 0x{(initial_position + readed_bytes).ToString("X")}");

                //Parse <value>
                string value = ByteUtil.To_readable_string(reader.ReadBytes(Length - Length_in_string.Length - 1 - Keyword.Length - 1 - 1));

                //Discard \n
                reader.BaseStream.Position++;

                if (Keyword.ToString() == "path")
                    return (true, value);
                else if (Keyword.ToString() == "charset")
                    throw new NotSupportedException("The current Tar extractor implementation doesn't support other text encoding");

                readed_bytes += Length;
            }
            if (readed_bytes != total_header_length)
                throw new FileCorruoptedException($"PAX header \"might\" be corrupted, please contact software developer of seekableExtraction, position 0x{initial_position.ToString("X")}");
            return (false, string.Empty);
        }

        public new static bool Check_compatibility(ExtractorOptions option)
        {
            //Naive implementation to check compatibility... probably will change it in the future

            //Check file size
            if ((new FileInfo(option.archive_filepath)).Length < min_tar_size)
                return false;

            //Check file extension
            VFile file = new VFile(option.archive_filepath);
            return file.Name.EndsWith(".tar");
        }
    }

    static class TarType
    {
        public const char
            File = '0',
            Directory = '5',
            GNU_Longname = 'L',
            PAX_Extended_header = 'x',
            PAX_Global_extended_header = 'g';
    }

    class TarState
    {
        public string fullFilepath;
        public long offset_in_tar_file, size;
        public char type;

        public override string ToString()
        {
            return $"{type}\0{fullFilepath}\0{offset_in_tar_file}\0{size}";
        }
    }
}
