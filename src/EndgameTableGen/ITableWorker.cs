namespace EndgameTableGen
{
    internal interface ITableWorker
    {
        void Start();
        Table GenerateTable(int[,] config);      // [2,5] array of nonking counts
        void Finish();
    }
}
