namespace EndgameTableGen
{
    internal struct GraphEdge
    {
        public long w_next_id;      // FIXFIXFIX: make smaller, using radix of number of nonking pieces in config
        public int next_tindex;
        public int reverse_tindex;
        public bool draw_by_insufficient_material;  // FIXFIXFIX: factor out into a separate table indexed by w_next_id
    }

    internal struct GraphNode
    {
        public GraphEdge[] whiteEdgeList;
        public GraphEdge[] blackEdgeList;
    }
}
