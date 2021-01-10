using System;

namespace EndgameTableGen
{
    internal interface ITableWorker : IDisposable
    {
        void Start();
        Table GenerateTable(int[,] config);      // [2,5] array of nonking counts
        void Finish();
    }
}
