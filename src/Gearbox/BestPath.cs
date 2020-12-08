namespace Gearbox
{
    public class BestPath
    {
        public bool isCircular;     // does the final move lead to a repeated position?
        public BestPathNode[] nodes;
    }

    public class BestPathNode
    {
        public Move         move;
        public string       uci;
        public string       san;
        public HashValue    hash;
    }
}
