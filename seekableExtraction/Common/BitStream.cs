using System;
using System.IO;

namespace seekableExtraction.Common
{
    public class BitStream : Stream
    {
        Stream baseStream;
        long bitPosition = 0;
        long bitstream_length;
        BitOrder bit_order;
        byte currentbyte;
        byte currentMask = 0x80;

        public BitStream(Stream stream, BitOrder order) {
            bit_order = order;
            baseStream = stream;
            bitstream_length = baseStream.Length > (long.MaxValue >> 3)
                               ? long.MaxValue
                               : (baseStream.Length << 3) - 1;
            Position = 0;
        }

        private void reverse_current_byte() {
            //Reverse bit order in a byte
            //e.g. 00111000b ==> 00011100b
            //or   10100001b ==> 10000101b
            //The code below is taken from https://graphics.stanford.edu/~seander/bithacks.html
            //Which is in Public Domain, Thank you very much Sean Eron Anderson!
            //
            //This (https://stackoverflow.com/a/3590938) is also worth a watch, contains a benchmark of bitorder reverse algorithms.
            //I end up picking 7 operations one over the lookup table approach, it just looks much cleaner
            //I also tried 4 operations one but that produced wrong output for me.

            currentbyte = (byte) (((currentbyte * 0x0802 & 0x22110) | (currentbyte * 0x8020 & 0x88440)) * 0x10101 >> 16);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => bitstream_length;

        public override long Position {
            get => bitPosition;

            set {
                if (value > Length)
                    throw new NotSupportedException("Out of range");
                bitPosition = value;
                currentMask = (byte) (0x80 >> (byte) value % 8);
                baseStream.Position = value / 8;
                currentbyte = (byte) baseStream.ReadByte();
                if (bit_order == BitOrder.LeastSignificantFirst)
                    reverse_current_byte();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public bool readBit()
        {
            if (bitPosition > Length)
                throw new NotSupportedException("Out of range");
            bool value = (currentbyte & currentMask) > 0;
            currentMask >>= 1;
            if (currentMask == 0x00)
            {
                currentMask = 0x80;
                currentbyte = (byte) baseStream.ReadByte();
                if (bit_order == BitOrder.LeastSignificantFirst)
                    reverse_current_byte();
            };
            bitPosition++;
            return value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                Position = offset;
            else if (origin == SeekOrigin.Current)
                Position += offset;
            else
                Position = Length - offset;
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    public enum BitOrder { 
        MostSignificantFirst, //Does nothing to the bytes readed
        LeastSignificantFirst //Reverses the bytes readed by program
    }
}
