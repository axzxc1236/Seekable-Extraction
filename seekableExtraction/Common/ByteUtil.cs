using System;

namespace seekableExtraction.Common
{
    public static class ByteUtil
    {
        public static string Encode_to_string(byte[] input, char delimiter)
        {
            return BitConverter.ToString(input).Replace('-', delimiter);
        }
        public static string Encode_to_string(byte input)
        {
            return BitConverter.ToString(new byte[] { input });
        }

        public static byte[] Decode_from_string(string input, char delimiter)
        {
            string[] array = input.Split(delimiter);
            byte[] result = new byte[array.Length];
            int counter = 0;
            foreach (string s in array)
            {
                result[counter] = Convert.ToByte(array[counter], 16);
                counter++;
            }
            return result;
        }

        public static string To_readable_string(byte[] input)
        {
            return System.Text.Encoding.UTF8.GetString(input, 0, input.Length);
        }
    }
}
