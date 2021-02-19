using System.IO;

namespace EndgameTableGen
{
    internal class BitWriter
    {
        FileStream outfile;
        byte[] accum = new byte[1];
        int bitCount;

        public BitWriter(FileStream outfile)
        {
            this.outfile = outfile;
        }

        public void Write(string binary)
        {
            foreach (char c in binary)
            {
                accum[0] = (byte)((accum[0] << 1) | (c - '0'));
                if (++bitCount == 8)
                {
                    outfile.Write(accum, 0, 1);
                    bitCount = 0;
                }
            }
        }

        public void Write(int data, int nbits)
        {
            for (int i = 0; i < nbits; ++i)
            {
                accum[0] = (byte)((accum[0] << 1) | (1 & (data >> ((nbits - 1) - i))));
                if (++bitCount == 8)
                {
                    outfile.Write(accum, 0, 1);
                    bitCount = 0;
                }
            }
        }

        public void Flush()
        {
            while (bitCount > 0)
                Write("0");
        }
    }
}
