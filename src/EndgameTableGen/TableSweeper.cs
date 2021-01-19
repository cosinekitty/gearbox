namespace EndgameTableGen
{
    internal abstract class TableSweeper
    {
        public abstract void Init(int max_table_size);

        public abstract void Sweep(
            TableGenerator generator,
            Table table,
            int max_search_ply,
            string whiteChildFileName,
            string whiteIndexFileName,
            string blackChildFileName,
            string blackIndexFileName);
    }
}
