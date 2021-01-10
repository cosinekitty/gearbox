using System;

namespace EndgameTableGen
{
    internal abstract class Table : IDisposable
    {
        protected const int BytesPerPosition = 3;
        protected const int MaxTableSize = int.MaxValue / BytesPerPosition;
        protected readonly int size;       // the total number of entries, NOT BYTES

        public const int MinScore = -2048;
        public const int MaxScore = +2047;

        protected Table(int size)
        {
            if (size < 1 || size > MaxTableSize)
                throw new ArgumentException("Invalid table size: " + size);

            this.size = size;
        }

        public abstract void Dispose();
        public abstract void SetWhiteScore(int tindex, int score);
        public abstract void SetBlackScore(int tindex, int score);
        public abstract int GetWhiteScore(int tindex);
        public abstract int GetBlackScore(int tindex);
        public abstract void Save(string filename);
        public abstract void Clear();
        public abstract Table ReadOnlyClone();  // make a copy that is safe to use read-only in another thread
    }
}
