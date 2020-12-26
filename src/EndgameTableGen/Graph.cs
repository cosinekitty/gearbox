namespace EndgameTableGen
{
    internal struct GraphEdge
    {
        public int packed_config_id;
        public int next_tindex;
        public int reverse_tindex;
    }

    internal struct GraphNode
    {
        public GraphEdge[] whiteEdgeList;
        public GraphEdge[] blackEdgeList;
    }
}
