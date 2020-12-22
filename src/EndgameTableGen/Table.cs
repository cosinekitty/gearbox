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
    }
}
