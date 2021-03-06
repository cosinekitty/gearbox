using System;

namespace EndgameTableGen
{
    internal abstract class Table : IDisposable
    {
        internal const int BytesPerPosition = 3;
        protected const int MaxTableSize = int.MaxValue / BytesPerPosition;
        protected int size;       // the total number of entries, NOT BYTES

        public const int MinScore = -2048;
        public const int MaxScore = +2047;

        protected Table(int size)
        {
            if (size < 0 || size > MaxTableSize)
                throw new ArgumentException("Invalid table size: " + size);

            this.size = size;
        }

        public int Size => size;
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
