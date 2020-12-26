using System;
using System.Collections.Generic;

namespace EndgameTableGen
{
    internal struct GraphEdge
    {
        public int packed_config_id;
        public int next_tindex;
        public int reverse_tindex;
    }

    internal struct GraphEdgeList
    {
        public int front;
        public int count;
    }

    internal class GraphPool
    {
        public readonly List<GraphEdge> pool = new List<GraphEdge>();
        public readonly GraphEdgeList[] whiteTable;     // whiteTable[tindex] = list of transitions to other nodes
        public readonly GraphEdgeList[] blackTable;

        public GraphPool(int size)
        {
            whiteTable = new GraphEdgeList[size];
            blackTable = new GraphEdgeList[size];
        }

        public void StartNewList(int tindex, bool wturn, int nmoves)
        {
            GraphEdgeList[] table = (wturn ? whiteTable : blackTable);
            if (table[tindex].count > 0)
                throw new Exception(string.Format("Attempt to start a new list at duplicate tindex={0}, wturn={1}", tindex, wturn));
            table[tindex].front = pool.Count;
            table[tindex].count = nmoves;
        }

        public GraphEdgeList GetList(int tindex, bool wturn)
        {
            return wturn ? whiteTable[tindex] : blackTable[tindex];
        }
    }
}
