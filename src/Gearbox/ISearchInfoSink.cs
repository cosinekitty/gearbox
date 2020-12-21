namespace Gearbox
{
    public interface ISearchInfoSink
    {
        void OnBeginSearchMove(Board board, Move move, int limit);
        void OnBestPath(Board board, BestPath path);
    }
}
