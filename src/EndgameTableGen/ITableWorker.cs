namespace EndgameTableGen
{
    internal interface ITableWorker
    {
        void Start();
        void GenerateTable(int[,] config);      // [2,5] array of nonking counts
        void Finish();
    }
}
