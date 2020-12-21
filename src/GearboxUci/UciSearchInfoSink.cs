using System;
using System.Linq;
using Gearbox;

namespace GearboxUci
{
    internal class UciSearchInfoSink: ISearchInfoSink
    {
        // http://wbec-ridderkerk.nl/html/UCIProtocol.html

        public void OnBeginSearchMove(Board board, Move move, int limit)
        {
            Console.WriteLine("info currmove {0}", move);
            Console.Out.Flush();
        }

        public void OnBestPath(Board board, BestPath path)
        {
            string text = string.Join(" ", path.nodes.Select(n => n.uci));
            Console.WriteLine("info pv {0}", text);
            Console.Out.Flush();
        }
    }
}
