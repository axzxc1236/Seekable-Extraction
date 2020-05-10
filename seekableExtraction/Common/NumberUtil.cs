using System;
using System.Text;

namespace seekableExtraction.Common
{
    public static class NumberUtil
    {
        /// <summary>
        /// Convert bytes that represent a number to decimal (base 10) number
        /// </summary>
        /// <param name="input">
        /// bytes that's supposed to represent a number<br/>
        /// For example [0x37,0x37,0x37] (represents 777)
        /// </param>
        /// <param name="_base">
        /// Positional Notation https://en.wikipedia.org/wiki/Positional_notation<br/>
        /// If the number is in binary, use 2<br/>
        /// Is the number is in octal, use 8
        /// </param>
        /// <param name="removeNonDigitBytes">Remove bytes that are not digits</param>
        /// <returns>decimal number</returns>
        public static long ASCII_bytes_to_number(byte[] input, int _base = 10, bool removeNonDigitBytes = true)
        {
            return ASCII_string_to_number(Encoding.ASCII.GetString(input), _base, removeNonDigitBytes);
        }

        /// <summary>
        /// Convert bytes that represent a number to decimal (base 10) number
        /// </summary>
        /// <param name="_base">
        /// Positional Notation https://en.wikipedia.org/wiki/Positional_notation<br/>
        /// If the number is in binary, use 2<br/>
        /// Is the number is in octal, use 8
        /// </param>
        /// <param name="removeNonDigitBytes">Remove bytes that are not digits</param>
        /// <returns>decimal number</returns>
        public static long ASCII_string_to_number(string input, int _base = 10, bool removeNonDigitBytes = true)
        {
            if (!removeNonDigitBytes)
                return Convert.ToInt64(input, _base);
            else
            {
                StringBuilder digitOnlyString = new StringBuilder(input.Length);
                foreach (char c in input)
                    if (char.IsDigit(c))
                        digitOnlyString.Append(c);
                return Convert.ToInt64(digitOnlyString.ToString(), _base);
            }
        }
    }
}
