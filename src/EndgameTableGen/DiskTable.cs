using System;
using System.IO;

namespace EndgameTableGen
{
    internal class DiskTable : Table
    {
        private readonly string filename;
        private FileStream stream;

        public DiskTable(int size, string filename)
            : base(size)
        {
            this.filename = filename;
        }

        public override void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

        public void Create()
        {
            stream = File.Create(filename);
            Clear();
        }

        public void OpenForRead()
        {
            stream = File.OpenRead(filename);
        }

        public override Table ReadOnlyClone()
        {
            var clone = new DiskTable(size, filename);
            clone.OpenForRead();
            return clone;
        }

        public override void Clear()
        {
            const int BufferSize = 64 * 1024;
            var buffer = new byte[BufferSize];
            stream.Seek(0, SeekOrigin.Begin);
            long bytesRemaining = (long)size * BytesPerPosition;
            while (bytesRemaining > 0)
            {
                int nbytes = (bytesRemaining < BufferSize) ? (int)bytesRemaining : BufferSize;
                stream.Write(buffer, 0, nbytes);
                bytesRemaining -= nbytes;
            }
            stream.Flush();
        }

        public override void Save(string filename)
        {
            stream.Close();
            stream = File.OpenRead(filename);
        }

        private byte[] ReadScoreBytes(int tindex)
        {
            long k = (long)BytesPerPosition * tindex;
            var data = new byte[BytesPerPosition];
            stream.Seek(k, SeekOrigin.Begin);
            int n = stream.Read(data, 0, BytesPerPosition);
            if (n != BytesPerPosition)
                throw new Exception($"Tried to read {BytesPerPosition} bytes, but received {n}: tindex={tindex} in {filename}");
            return data;
        }

        private void WriteScoreBytes(int tindex, byte[] data)
        {
            long k = (long)BytesPerPosition * tindex;
            stream.Seek(k, SeekOrigin.Begin);
            stream.Write(data, 0, BytesPerPosition);
        }

        public override int GetWhiteScore(int tindex)
        {
            byte[] data = ReadScoreBytes(tindex);
            int s = ((int)data[0] << 4) | ((int)data[1] >> 4);
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }

        public override int GetBlackScore(int tindex)
        {
            byte[] data = ReadScoreBytes(tindex);
            int s = (((int)data[1] & 0x0f) << 8) | (int)data[2];
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }

        public override void SetWhiteScore(int tindex, int score)
        {
            byte[] data = ReadScoreBytes(tindex);

            // Save the upper 8 bits of the score in the first byte.
            data[0] = (byte)(score >> 4);

            // Save the lower 4 bits of the score in the upper half of the second byte.
            data[1] = (byte) ((data[1] & 0x0f) | ((score & 0x00f) << 4));

            WriteScoreBytes(tindex, data);
        }

        public override void SetBlackScore(int tindex, int score)
        {
            byte[] data = ReadScoreBytes(tindex);

            // Save the upper 4 bits of the score in the lower half of the second byte.
            data[1] = (byte) ((data[1] & 0xf0) | ((score >> 8) & 0x0f));

            // Save the lower 8 bits of the score in the third byte.
            data[2] = (byte) (score & 0x0ff);

            WriteScoreBytes(tindex, data);
        }
    }
}
