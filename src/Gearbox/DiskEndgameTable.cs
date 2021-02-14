using System;
using System.IO;

namespace Gearbox
{
    internal class DiskEndgameTable : IEndgameTable
    {
        private const int BytesPerPosition = 3;
        private const int MaxTableSize = int.MaxValue / BytesPerPosition;
        private const int EnemyMatedScore = +2000;
        private const int FriendMatedScore = -2000;

        private string filename;
        private FileStream infile;
        private readonly int size;       // the total number of entries, NOT BYTES
        private byte[] data = new byte[3];

        public DiskEndgameTable(string filename)
        {
            infile = File.OpenRead(filename);
            long fsize = infile.Length / BytesPerPosition;

            if (fsize < 1 || fsize > MaxTableSize)
                throw new ArgumentException("Invalid table size: " + fsize);

            this.size = (int)fsize;
            this.filename = filename;
        }

        public void Dispose()
        {
            if (infile != null)
            {
                infile.Dispose();
                infile = null;
            }
        }

        public int GetScore(int tindex, bool whiteToMove)
        {
            int score = whiteToMove ? RawWhiteScore(tindex) : RawBlackScore(tindex);

            // Scores stored in the table are in the range -2048..+2047.
            // The score 0 is a draw (or unreachable position).
            // Other scores are mate-in-plies values.
            // -2000 means the player to move is in a checkmate right now.
            // +1999 means the side to move can checkmate in one ply, etc.
            // Adjust for Gearbox score encoding.

            if (score < 0)
                score += (Score.FriendMated - FriendMatedScore);
            else if (score > 0)
                score += (Score.EnemyMated - EnemyMatedScore);

            return score;
        }

        private void ReadTableEntry(int tindex)
        {
            if (tindex < 0 || tindex >= size)
                throw new ArgumentException($"Invalid tindex={tindex} for table with size={size}");
            long pos = (long)BytesPerPosition * tindex;
            long check = infile.Seek(pos, SeekOrigin.Begin);
            if (check != pos)
                throw new Exception($"Tried to seek to {pos}, but ended up at {check} in file {filename}");
            int nread = infile.Read(data, 0, BytesPerPosition);
            if (nread != BytesPerPosition)
                throw new Exception($"Tried to read {BytesPerPosition} bytes, but actually read {nread}");
        }

        private int RawWhiteScore(int tindex)
        {
            ReadTableEntry(tindex);
            int s = ((int)data[0] << 4) | ((int)data[1] >> 4);
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }

        private int RawBlackScore(int tindex)
        {
            ReadTableEntry(tindex);
            int s = (((int)data[1] & 0x0f) << 8) | (int)data[2];
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }
    }
}
