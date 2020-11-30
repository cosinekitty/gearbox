using System;

namespace Gearbox
{
    // The Gearbox engine maintains two independent 64-bit hash values
    // for each chess position, for a total of 128 bits.
    public struct HashValue
    {
        public ulong a;     // used for transposition table indexing
        public ulong b;     // stored in the transposition table for verification

        public override string ToString()
        {
            return a.ToString("x16") + ":" + b.ToString("x16");
        }
    }
}