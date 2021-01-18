namespace EndgameTableGen
{
    internal abstract class TableSweeper
    {
        public abstract void Sweep(
            TableGenerator generator,
            Table table,
            string whiteChildFileName,
            string whiteIndexFileName,
            string blackChildFileName,
            string blackIndexFileName);
    }
}
