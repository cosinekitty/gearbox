using System;
using System.Collections.Generic;
using System.IO;

namespace EndgameTableGen
{
    internal class TableSweeper_Backward : TableSweeper
    {
        private MemoryTable bestScoreSoFar;
        private string work_dir;
        private List<string> fileDeletionList = new();

        public override void Init(int max_table_size)
        {
            bestScoreSoFar = new MemoryTable(0, max_table_size);
        }

        public override void Sweep(
            TableGenerator generator,
            Table table,
            string whiteChildFileName,
            string whiteIndexFileName,
            string blackChildFileName,
            string blackIndexFileName)
        {
            bestScoreSoFar.Resize(table.Size);
            bestScoreSoFar.SetAllScores(TableGenerator.UndefinedScore);

            work_dir = Path.Combine(TableGenerator.OutputDirectory(), "work_" + generator.CurrentConfigId.ToString("D10"));
            if (Directory.Exists(work_dir))
                Directory.Delete(work_dir, true);
            Directory.CreateDirectory(work_dir);

            using (EdgeIndexer whiteIndexer = Sort(true,  generator, whiteChildFileName, whiteIndexFileName))
                using (EdgeIndexer blackIndexer = Sort(false, generator, blackChildFileName, blackIndexFileName))
                    BackPropagate(generator, whiteIndexer, blackIndexer);

            foreach (string fn in fileDeletionList)
                File.Delete(fn);
            fileDeletionList.Clear();
            Directory.Delete(work_dir, true);
        }

        private void BackPropagate(TableGenerator generator, EdgeIndexer whiteIndexer, EdgeIndexer blackIndexer)
        {
            generator.Log("BackPropagate: starting {0:D10}", generator.CurrentConfigId);
        }

        private EdgeIndexer Sort(bool isWhiteTurn, TableGenerator generator, string inChildFileName, string inIndexFileName)
        {
            // Convert the (parent_tindex => [child_tindex, child_tindex, ...]) mapping to its inverse:
            // (child_tindex => [parent_tindex, parent_tindex, ...]).
            // We do this by writing (parent_tindex, child_tindex) records, then sorting them by child_tindex.

            int table_size = bestScoreSoFar.Size;
            string edgeFileName = Path.Combine(work_dir, isWhiteTurn ? "w.edge" : "b.edge");

            // First convert to (parent_tindex, child_tindex) records...
            // FIXFIXFIX: consider generating this format in the first place.
            // The only problem now is that would break the forward-prop algorithm.
            using (var reader = new ChildReader(inChildFileName, inIndexFileName))
            {
                using (var writer = new EdgeWriter(edgeFileName))
                {
                    generator.Log("Inverting {0} => {1}", inChildFileName, edgeFileName);

                    var after_tindex_list = new List<int>();
                    GraphEdge edge;
                    for (edge.before_tindex = 0; edge.before_tindex < table_size; ++edge.before_tindex)
                    {
                        int best_foreign_score = reader.Read(after_tindex_list, edge.before_tindex);
                        if (best_foreign_score >= TableGenerator.FriendMatedScore)
                        {
                            if (isWhiteTurn)
                                bestScoreSoFar.SetWhiteScore(edge.before_tindex, best_foreign_score);
                            else
                                bestScoreSoFar.SetBlackScore(edge.before_tindex, best_foreign_score);
                        }

                        foreach (int after_tindex in after_tindex_list)
                        {
                            edge.after_tindex = after_tindex;
                            writer.WriteEdge(edge);
                        }
                    }
                }
            }

            // We don't need the child files any more.
            File.Delete(inChildFileName);
            File.Delete(inIndexFileName);

            // Now we are ready to sort!
            string outIndexFileName = Path.Combine(work_dir, isWhiteTurn ? "w.index" : "b.index");
            using (var sorter = new EdgeFileSorter(work_dir, 16, table_size))
            {
                generator.Log("Sorting {0}", edgeFileName);
                sorter.Sort(edgeFileName, outIndexFileName);
            }

            fileDeletionList.Add(edgeFileName);
            fileDeletionList.Add(outIndexFileName);
            return new EdgeIndexer(outIndexFileName, edgeFileName);
        }
    }
}
