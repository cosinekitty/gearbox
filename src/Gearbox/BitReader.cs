using System;

namespace Gearbox
{
    internal class BitReader
    {
        private byte[] buffer;
        private byte accum;
        private int bitsInAccum;
        private int index;
        private int limit;

        public void Init(byte[] buffer, int limit)
        {
            this.buffer = buffer;
            this.accum = 0;
            this.bitsInAccum = 0;
            this.index = 0;
            this.limit = limit;
        }

        public int ReadBit()
        {
            if (bitsInAccum == 0)
            {
                if (index >= limit)
                    throw new Exception($"Attempt to read beyond limit of {limit} bytes.");
                accum = buffer[index++];
                bitsInAccum = 8;
            }
            int bit = (int)(accum >> 7);
            accum <<= 1;
            --bitsInAccum;
            return bit;
        }

        public int ReadInteger(int nbits)
        {
            int value = 0;
            for (int i = 0; i < nbits; ++i)
                value = (value << 1) | ReadBit();
            return value;
        }
    }
}
