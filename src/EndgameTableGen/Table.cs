namespace EndgameTableGen
{
    internal abstract class Table
    {
        public abstract void SetWhiteScore(int tindex, int score);
        public abstract void SetBlackScore(int tindex, int score);
        public abstract int GetWhiteScore(int tindex);
        public abstract int GetBlackScore(int tindex);
        public abstract void Save(string filename);
        public abstract void Clear();

        public static Table Load(string filename, int size)
        {
            // FIXFIXFIX: Come back and add support for DiskTable too.
            return MemoryTable.MemoryLoad(filename, size);
        }

        public static Table Create(int size)
        {
            // FIXFIXFIX: Come back and add support for DiskTable too.
            return new MemoryTable(size);
        }
    }
}
