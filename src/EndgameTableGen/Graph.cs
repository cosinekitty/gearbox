namespace EndgameTableGen
{
    internal struct GraphEdge
    {
        public long w_next_id;      // FIXFIXFIX: make smaller, using radix of number of nonking pieces in config
        public int next_tindex;
        public int reverse_tindex;
    }

    internal struct GraphNode
    {
        public GraphEdge[] whiteEdgeList;
        public GraphEdge[] blackEdgeList;
    }
}
