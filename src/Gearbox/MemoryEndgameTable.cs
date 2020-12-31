using System;
using System.IO;

namespace Gearbox
{
    internal class MemoryEndgameTable : IEndgameTable
    {
        private const int BytesPerPosition = 3;
        private const int MaxTableSize = int.MaxValue / BytesPerPosition;
        private const int EnemyMatedScore  = +2000;
        private const int FriendMatedScore = -2000;

        private readonly byte[] data;
        private readonly int size;       // the total number of entries, NOT BYTES

        public MemoryEndgameTable(int size)
        {
            if (size < 1 || size > MaxTableSize)
                throw new ArgumentException("Invalid table size: " + size);

            this.size = size;
            data = new byte[BytesPerPosition * size];
        }

        public static MemoryEndgameTable Load(string filename)
        {
            using (FileStream infile = File.OpenRead(filename))
            {
                long size = infile.Length / BytesPerPosition;
                if (size > MaxTableSize)
                    throw new Exception($"Endgame table file is too large: {filename}");
                var table = new MemoryEndgameTable((int)size);
                int nBytesRead = infile.Read(table.data, 0, table.data.Length);
                if (nBytesRead != table.data.Length)
                    throw new Exception(string.Format("Could not read {0} bytes from {1}", table.data.Length, filename));
                return table;
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

        private int RawWhiteScore(int tindex)
        {
            int k = 3 * tindex;
            int s = ((int)data[k] << 4) | ((int)data[k+1] >> 4);
            // Sign-extend to restore negative scores.
            if (0 != (s & 0x800))
                s |= ~0xfff;
            return s;
        }

        private int RawBlackScore(int tindex)
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
