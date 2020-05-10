using System;

namespace seekableExtraction.Common
{
    public static class NumberUtil
    {
        /// <summary>
        /// Convert bytes that represent a number to decimal (base 10) number
        /// </summary>
        /// <param name="input">
        /// bytes that's supposed to represent a number<br/>
        /// For example [0x37,0x37,0x37] (represents 777)<br/>
        /// </param>
        /// <param name="_base">
        /// Positional Notation https://en.wikipedia.org/wiki/Positional_notation<br/>
        /// If the number is in binary, use 2<br/>
        /// Is the number is in octal, use 8
        /// </param>
        /// <returns></returns>
        public static long Bytes_to_number(byte[] input, int _base = 10)
        {
            return Convert.ToInt64(System.Text.Encoding.UTF8.GetString(input, 0, input.Length).Replace("\0", ""), _base);
        }
    }
}
