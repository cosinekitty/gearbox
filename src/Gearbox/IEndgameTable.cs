using System;

namespace Gearbox
{
    public interface IEndgameTable : IDisposable
    {
        int GetScore(int tableIndex, bool whiteToMove);
    }
}
