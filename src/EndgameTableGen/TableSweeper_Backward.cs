using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace EndgameTableGen
{
    internal class TableSweeper_Backward : TableSweeper
    {
        private MemoryTable bestScoreSoFar;
        private byte[] whiteUnresolvedChildren;
        private byte[] blackUnresolvedChildren;
        private string work_dir;
        private List<string> fileDeletionList = new();

        public override void Init(int max_table_size)
        {
            bestScoreSoFar = new MemoryTable(0, max_table_size);
            whiteUnresolvedChildren = new byte[max_table_size];
            blackUnresolvedChildren = new byte[max_table_size];
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

            using (EdgeIndexer whiteIndexer = Sort(true, generator, whiteChildFileName, whiteIndexFileName, whiteUnresolvedChildren))
                using (EdgeIndexer blackIndexer = Sort(false, generator, blackChildFileName, blackIndexFileName, blackUnresolvedChildren))
                    BackPropagate(generator, table, whiteIndexer, blackIndexer);

            foreach (string fn in fileDeletionList)
                File.Delete(fn);
            fileDeletionList.Clear();
            Directory.Delete(work_dir, true);
        }

        private int UnresolvedWhiteParents(int table_size)
        {
            int total = 0;
            for (int tindex = 0; tindex < table_size; ++tindex)
                if (whiteUnresolvedChildren[tindex] > 0)
                    ++total;
            return total;
        }

        private int UnresolvedBlackParents(int table_size)
        {
            int total = 0;
            for (int tindex = 0; tindex < table_size; ++tindex)
                if (blackUnresolvedChildren[tindex] > 0)
                    ++total;
            return total;
        }

        private void BackPropagate(TableGenerator generator, Table table, EdgeIndexer whiteIndexer, EdgeIndexer blackIndexer)
        {
            int table_size = table.Size;
            var parent_list = new List<int>();
            int whiteUnresolvedParents = UnresolvedWhiteParents(table_size);
            int blackUnresolvedParents = UnresolvedBlackParents(table_size);
            generator.Log("BackPropagate: starting {0:D10} with unresolved {1:N0} white, {2:N0} black.", generator.CurrentConfigId, whiteUnresolvedParents, blackUnresolvedParents);

            int progress = 1;
            for (int child_ply = 0; progress > 0; ++child_ply)
            {
                int parent_ply = child_ply + 1;
                int white_progress = 0;
                int black_progress = 0;

                if (0 != (parent_ply & 1))
                {
                    // On odd parent plies, we look for parent nodes with forced wins for the side to move.
                    // A forced win occurs when at least one child node has the winning score
                    // corresponding to this odd ply value, or the "bestSoFar" score has that value thanks to a foreign node.
                    int parent_winning_score = TableGenerator.EnemyMatedScore - parent_ply;
                    int child_losing_score = TableGenerator.FriendMatedScore + child_ply;
                    Debug.Assert(TableGenerator.AdjustScoreForPly(child_losing_score) == parent_winning_score);

                    // Check for winning position for White parent to move, Black child to move.
                    for (int child_tindex = 0; child_tindex < table_size; ++child_tindex)
                    {
                        if (table.GetBlackScore(child_tindex) == child_losing_score)
                        {
                            whiteIndexer.GetBeforeTableIndexes(parent_list, child_tindex);
                            foreach (int parent_tindex in parent_list)
                            {
                                if (whiteUnresolvedChildren[parent_tindex] == 0)
                                    throw new Exception($"Expected unresolved Black child {child_tindex} for White parent {parent_tindex}.");

                                if (bestScoreSoFar.GetWhiteScore(parent_tindex) < parent_winning_score)
                                    bestScoreSoFar.SetWhiteScore(parent_tindex, parent_winning_score);

                                --whiteUnresolvedChildren[parent_tindex];
                            }
                        }
                    }

                    // Check for winning position for Black parent to move, White child to move.
                    for (int child_tindex = 0; child_tindex < table_size; ++child_tindex)
                    {
                        if (table.GetWhiteScore(child_tindex) == child_losing_score)
                        {
                            blackIndexer.GetBeforeTableIndexes(parent_list, child_tindex);
                            foreach (int parent_tindex in parent_list)
                            {
                                if (blackUnresolvedChildren[parent_tindex] == 0)
                                    throw new Exception($"Expected unresolved White child {child_tindex} for Black parent {parent_tindex}.");

                                if (bestScoreSoFar.GetBlackScore(parent_tindex) < parent_winning_score)
                                    bestScoreSoFar.SetBlackScore(parent_tindex, parent_winning_score);

                                --blackUnresolvedChildren[parent_tindex];
                            }
                        }
                    }

                    // Promote winning best-scores to actual scores regardless of remaining children,
                    // if best-so-far reaches the threshold, and we haven't already finalized the parent score.
                    for (int parent_tindex = 0; parent_tindex < table_size; ++parent_tindex)
                    {
                        if (table.GetWhiteScore(parent_tindex) == TableGenerator.UndefinedScore &&
                            bestScoreSoFar.GetWhiteScore(parent_tindex) == parent_winning_score)
                        {
                            table.SetWhiteScore(parent_tindex, parent_winning_score);
                            ++white_progress;
                        }

                        if (table.GetBlackScore(parent_tindex) == TableGenerator.UndefinedScore &&
                            bestScoreSoFar.GetBlackScore(parent_tindex) == parent_winning_score)
                        {
                            table.SetBlackScore(parent_tindex, parent_winning_score);
                            ++black_progress;
                        }
                    }
                }
                else
                {
                    // On even parent plies, we look for parent nodes with forced losses for the side to move.
                    // A forced loss occurs when ALL children have known scores, and the score
                    // is the exact score we expect for losing at the specified ply level.
                    int parent_losing_score = TableGenerator.FriendMatedScore + parent_ply;
                    int child_winning_score = TableGenerator.EnemyMatedScore - child_ply;
                    Debug.Assert(TableGenerator.AdjustScoreForPly(child_winning_score) == parent_losing_score);

                    // Check for losing position for White parent, winning for Black child.
                    for (int child_tindex = 0; child_tindex < table_size; ++child_tindex)
                    {
                        if (table.GetBlackScore(child_tindex) == child_winning_score)
                        {
                            // Find all parent nodes that lead to this child node.
                            whiteIndexer.GetBeforeTableIndexes(parent_list, child_tindex);

                            foreach (int parent_tindex in parent_list)
                            {
                                if (whiteUnresolvedChildren[parent_tindex] == 0)
                                    throw new Exception($"Expected unresolved Black child {child_tindex} for White parent {parent_tindex}.");

                                if (bestScoreSoFar.GetWhiteScore(parent_tindex) < parent_losing_score)
                                    bestScoreSoFar.SetWhiteScore(parent_tindex, parent_losing_score);

                                --whiteUnresolvedChildren[parent_tindex];
                            }
                        }
                    }

                    // Check for losing position for Black parent, winning for White child.
                    for (int child_tindex = 0; child_tindex < table_size; ++child_tindex)
                    {
                        if (table.GetWhiteScore(child_tindex) == child_winning_score)
                        {
                            // Find all parent nodes that lead to this child node.
                            blackIndexer.GetBeforeTableIndexes(parent_list, child_tindex);

                            foreach (int parent_tindex in parent_list)
                            {
                                if (blackUnresolvedChildren[parent_tindex] == 0)
                                    throw new Exception($"Expected unresolved White child {child_tindex} for Black parent {parent_tindex}.");

                                if (bestScoreSoFar.GetBlackScore(parent_tindex) < parent_losing_score)
                                    bestScoreSoFar.SetBlackScore(parent_tindex, parent_losing_score);

                                --blackUnresolvedChildren[parent_tindex];
                            }
                        }
                    }

                    // Promote losing best-scores to actual scores when child count reaches zero.
                    for (int parent_tindex = 0; parent_tindex < table_size; ++parent_tindex)
                    {
                        if (whiteUnresolvedChildren[parent_tindex] == 0 &&
                            table.GetWhiteScore(parent_tindex) == TableGenerator.UndefinedScore)
                        {
                            int final_score = bestScoreSoFar.GetWhiteScore(parent_tindex);
                            if (final_score <= TableGenerator.UnreachablePos)
                                throw new Exception($"Invalid bestSoFar score {final_score} at White parent {parent_tindex}");
                            table.SetWhiteScore(parent_tindex, final_score);
                            ++white_progress;
                        }

                        if (blackUnresolvedChildren[parent_tindex] == 0 &&
                            table.GetBlackScore(parent_tindex) == TableGenerator.UndefinedScore)
                        {
                            int final_score = bestScoreSoFar.GetBlackScore(parent_tindex);
                            if (final_score <= TableGenerator.UnreachablePos)
                                throw new Exception($"Invalid bestSoFar score {final_score} at Black parent {parent_tindex}");
                            table.SetBlackScore(parent_tindex, final_score);
                            ++black_progress;
                        }
                    }
                }

                whiteUnresolvedParents = UnresolvedWhiteParents(table_size);
                blackUnresolvedParents = UnresolvedBlackParents(table_size);

                generator.Log("BackPropagate[{0:D10}:{1:D2}]:  progress(white={2:N0}  black={3:N0})  unresolved=(white={4:N0}  black={5:N0})",
                    generator.CurrentConfigId,
                    parent_ply,
                    white_progress,
                    black_progress,
                    whiteUnresolvedParents,
                    blackUnresolvedParents);

                progress = white_progress + black_progress;
            }
        }

        private EdgeIndexer Sort(
            bool isWhiteTurn,
            TableGenerator generator,
            string inChildFileName,
            string inIndexFileName,
            byte[] unresolvedChildren)
        {
            // Convert the (parent_tindex => [child_tindex, child_tindex, ...]) mapping to its inverse:
            // (child_tindex => [parent_tindex, parent_tindex, ...]).
            // We do this by writing (parent_tindex, child_tindex) records, then sorting them by child_tindex.

            int table_size = bestScoreSoFar.Size;
            string edgeFileName = Path.Combine(work_dir, isWhiteTurn ? "w.edge" : "b.edge");

            // Also tally how many children each parent has.
            Array.Clear(unresolvedChildren, 0, unresolvedChildren.Length);

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

                        int nchildren = after_tindex_list.Count;
                        if (nchildren > 0)
                        {
                            if (nchildren > byte.MaxValue)
                                throw new Exception($"Excessive child count = {nchildren}");

                            unresolvedChildren[edge.before_tindex] = (byte)nchildren;

                            foreach (int after_tindex in after_tindex_list)
                            {
                                edge.after_tindex = after_tindex;
                                writer.WriteEdge(edge);
                            }
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
