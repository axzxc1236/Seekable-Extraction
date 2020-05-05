﻿using System;
using System.Collections.Generic;
using System.IO;
using seekableExtraction.Common;

namespace seekableExtraction.Extractors
{
    /* Documents to read:
     * https://www.gnu.org/software/tar/manual/html_node/Formats.html
     * https://en.wikipedia.org/wiki/Tar_(computing)
     * https://www.gnu.org/software/tar/manual/html_node/Standard.html
     * 
     */
    public class Tar : Extractor
    {

        string filepath, statemapPath;
        Dictionary<string, TarState> states;
        Dictionary<string, vFolder> folderList;
        Dictionary<string, vFile> fileList;
        TarState current_state;
        public Tar(ExtractorOptions options) : base(options)
        {
            filepath = options.archive_filepath;
            statemapPath = filepath + ".statemap";
            states = new Dictionary<string, TarState>();
            folderList = new Dictionary<string, vFolder>();
            fileList = new Dictionary<string, vFile>();
        }
        public override Dictionary<string, vFile> FileList => fileList;
        public override Dictionary<string, vFolder> FolderList => folderList;

        public override (byte[], int) Read(vFile file, int length, long offset) {
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

        public override bool Generate_statemap(bool checkHeaderIntegrity = true)
        {
            //The code contains many magic number that are corressponds to blocksize (512)
            using (FileStream fs = File.OpenRead(filepath))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                //1024 refers to end-of-file marker
                while (reader.BaseStream.Position + 1024 != reader.BaseStream.Length)
                {
                    long start_position = reader.BaseStream.Position;

                    //check header integrity
                    if (checkHeaderIntegrity && !verify_header(reader))
                        throw new FileCorruoptedException(
                            $"The provided tar file might be corrupted (Header position: 0x{reader.BaseStream.Position.ToString("X")})");

                    current_state = new TarState();
                    current_state.fullFilepath = ByteUtil.To_readable_string(reader.ReadBytes(100)).Trim('\0');
                    if (current_state.fullFilepath == "././@LongLink")
                        throw new NotSupportedException("[TODO] tar file that has long file path is not supported");
                    if (states.ContainsKey(current_state.fullFilepath))
                        throw new ParseErrorException("Dupelicated filepath found in tar file, There is currently no plan to support hardlink/softlink");

                    //Skip metadata that has no use to this program
                    //File mode, Owner's numeric user ID and Group's numeric user ID
                    reader.BaseStream.Position += 24;

                    //File size
                    current_state.size = NumberUtil.Bytes_to_number(reader.ReadBytes(12), 8);

                    //Skip metadata that has no use to this program
                    //Last modification time in numeric Unix time format (octal)  and  Checksum for header record
                    reader.BaseStream.Position += 20;

                    byte type_flag = reader.ReadByte();

                    if (type_flag == '0' || type_flag == '\0')
                        current_state.type = '0'; //File
                    else if (type_flag == '5')
                        current_state.type = '5'; //Folder
                    else
                        throw new NotSupportedException("This Tar file is currently not supported, type:" + ByteUtil.Encode_to_string(type_flag));

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

                    //final processing
                    current_state.fullFilepath = vFile.Unify_filepath(current_state.fullFilepath);
                    long roundup_filesize = current_state.size;
                    if (current_state.size % 512 > 0)
                        roundup_filesize += 512 - current_state.size % 512;

                    current_state.offset_in_tar_file = start_position + 512; //512 == size of tar header roundup
                    reader.BaseStream.Position = current_state.offset_in_tar_file + roundup_filesize;

                    states.Add(current_state.fullFilepath, current_state);

                    //Register the folder/file we read.
                    if (type_flag == '5')
                    {
                        //This is a folder
                        vFolder folder = new vFolder(current_state.fullFilepath, current_state.size);
                        folderList.Add(folder.AbsolutePath, folder);
                    }
                    else
                    {
                        //This is a file
                        vFile file = new vFile(current_state.fullFilepath, current_state.size);
                        folderList[file.Prefix].Files.Add(file);
                        fileList.Add(file.AbsolutePath, file);
                    }

                }
                return true;
            }
        }

        /// <summary>
        /// Verify Integrity of tar v7 header, Stream POSITION WILL BE RESTORED after integrity check.<br/>
        /// WARNING: has try/catch block can silence exceptions (I might need to come up with another way to handle this....)
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
            try
            {
                //v7 header
                if (reader.BaseStream.Position + 257 > reader.BaseStream.Length)
                    throw new FileCorruoptedException("There is not enough space to fit header in this tar file, assume the file as corrupted");
                for (int i = 0; i < 148; i++)
                    checksum += reader.ReadByte();
                reader.BaseStream.Position += 1;
                expected_checksum = (int)NumberUtil.Bytes_to_number(reader.ReadBytes(6), 8);
                reader.BaseStream.Position += 1;

                for (int i = 0; i < 101; i++)
                    checksum += reader.ReadByte();


                //Checksum ustar header (if it exists)
                if (reader.BaseStream.Position + 243 < reader.BaseStream.Length &&
                    ByteUtil.Encode_to_string(reader.ReadBytes(6), ' ') == "75 73 74 61 72 00") //ustar marker
                {
                    checksum += 559;
                    for (int i = 0; i < 237; i++)
                        checksum += reader.ReadByte();
                }

                reader.BaseStream.Position = initial_position;
                return expected_checksum == checksum;
            }
            catch (Exception e1)
            {
                //Ugly exception handling... I hope I will come up with a better way to handle this.
                try
                {
                    reader.BaseStream.Position = initial_position;
                }
                catch (Exception e2)
                {
                    throw e2;
                }
                throw e1;
            }
        }

        public override bool Load_statemap()
        {
            states.Clear();
            fileList.Clear();
            folderList.Clear();
            folderList.Add("/", vFolder.rootFolder);
            if (!File.Exists(statemapPath))
                return false;
            using (StreamReader reader = File.OpenText(statemapPath))
            {
                string s;
                while ((s = reader.ReadLine()) != null)
                {
                    if (s == "" || s.StartsWith('#')) continue;
                    string[] rawStates = s.Split('\0');
                    TarState state = new TarState();
                    state.type = rawStates[0][0];
                    state.fullFilepath = rawStates[1];
                    state.offset_in_tar_file = long.Parse(rawStates[2]);
                    state.size = long.Parse(rawStates[3]);

                    if (state.type == '0')
                        fileList.Add(state.fullFilepath, new vFile(state.fullFilepath, state.size));
                    else
                        folderList.Add(state.fullFilepath, new vFolder(state.fullFilepath, state.size));

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

        public new static bool Check_compatibility(ExtractorOptions option) {
            //Naive implementation to check compatibility... probably will change it in the future
            vFile file = new vFile(option.archive_filepath);
            return file.Name.EndsWith(".tar");
        }
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
