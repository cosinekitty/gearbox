using System;
using System.IO;

namespace EndgameTableGen
{
    internal class Table
    {
        private const int BytesPerPosition = 3;
        private const int MaxTableSize = int.MaxValue / BytesPerPosition;

        private readonly byte[] data;
        private readonly int size;       // the total number of entries, NOT BYTES

        public Table(int size)
        {
            if (size < 1 || size > MaxTableSize)
                throw new ArgumentException("Invalid table size: " + size);

            this.size = size;
            data = new byte[BytesPerPosition * size];
        }

        public static Table Load(string filename, int size)
        {
            var table = new Table(size);
            using (FileStream infile = File.OpenRead(filename))
            {
                if (infile.Length != table.data.Length)
                    throw new ArgumentException(string.Format("Expected {0} to be {1} bytes, but found {2}", filename, table.data.Length, infile.Length));

                int nBytesRead = infile.Read(table.data, 0, table.data.Length);
                if (nBytesRead != table.data.Length)
                    throw new Exception(string.Format("Could not read {0} bytes from {1}", table.data.Length, filename));
            }
            return table;
        }

        public void Save(string filename)
        {
            using (FileStream outfile = File.OpenWrite(filename))
            {
                outfile.Write(data, 0, data.Length);
            }
        }

        public void SetWhiteScore(int tindex, int score)
        {
            // The score must fit in a 12-bit signed integer.
            // This is because we have to pack two of them into 3 bytes of memory.

            if (score < -2048 || score > +2047)
                throw new ArgumentException("Score is out of range: " + score);

            int k = 3 * tindex;

            // Save the upper 8 bits of the score in the first byte.
            data[k] = (byte)(score >> 4);

            // Save the lower 4 bits of the score in the upper half of the second byte.
            data[k+1] = (byte) ((data[k+1] & 0x0f) | ((score & 0x00f) << 4));
        }

        public void SetBlackScore(int tindex, int score)
        {
            if (score < -2048 || score > +2047)
                throw new ArgumentException("Score is out of range: " + score);

            int k = 3 * tindex;

            // Save the upper 4 bits of the score in the lower half of the second byte.
            data[k+1] = (byte) ((data[k+1] & 0xf0) | ((score >> 8) & 0x0f));

            // Save the lower 8 bits of the score in the third byte.
            data[k+2] = (byte) (score & 0x0ff);
        }

        public int GetWhiteScore(int tindex)
        {
            int k = 3 * tindex;
            int s = ((int)data[k] << 4) | ((int)data[k+1] >> 4);
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }

        public int GetBlackScore(int tindex)
        {
            int k = 3 * tindex;
            int s = (((int)data[k+1] & 0x0f) << 8) | (int)data[k+2];
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }
    }
}
