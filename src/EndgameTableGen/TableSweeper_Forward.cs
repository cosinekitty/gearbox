using System.Collections.Generic;
using System.IO;

namespace EndgameTableGen
{
    internal class TableSweeper_Forward : TableSweeper
    {
        public override void Init(int max_table_size)
        {
        }

        public override void Sweep(
            TableGenerator generator,
            Table table,
            int max_search_ply,
            string whiteChildFileName,
            string whiteIndexFileName,
            string blackChildFileName,
            string blackIndexFileName)
        {
            using (var whiteChildReader = new ChildReader(whiteChildFileName, whiteIndexFileName))
            {
                using (var blackChildReader = new ChildReader(blackChildFileName, blackIndexFileName))
                {
                    ForwardPropagate(generator, table, max_search_ply, whiteChildReader, blackChildReader);
                }
            }

            // Clean up temporary files... they are large and we don't want them to fill up the hard disk!
            File.Delete(whiteChildFileName);
            File.Delete(whiteIndexFileName);
            File.Delete(blackChildFileName);
            File.Delete(blackIndexFileName);
        }

        private void ForwardPropagate(
            TableGenerator generator,
            Table table,
            int max_search_ply,
            ChildReader whiteChildReader,
            ChildReader blackChildReader)
        {
            // WriteChildren() has already initialized the score of all immediate checkmates with -2000.
            // These are considered ply=0, and thus they are even plies.
            // All losing positions are therefore even plies, and all winning positions are odd plies.
            // Start with ply 1 to find all moves that lead immediately to checkmate.
            // We have to find forced wins and losses for both White and Black, because
            // both sides have mating material in some configurations.

            var child_tindex_list = new List<int>();
            int size = table.Size;
            int prev_progress = 1;
            int curr_progress = 0;
            for (int ply = 1; prev_progress + curr_progress > 0 || ply <= max_search_ply + 1; ++ply)
            {
                prev_progress = curr_progress;
                int white_changes = 0;
                int black_changes = 0;
                if (0 != (ply & 1))
                {
                    // On odd plies, we look for parent nodes with forced wins for the side to move.
                    // A forced win occurs when at least one child node has the winning score
                    // corresponding to this odd ply value.
                    int winning_score = TableGenerator.EnemyMatedScore - ply;
                    for (int parent_tindex = 0; parent_tindex < size; ++parent_tindex)
                    {
                        // Check for winning position for White to move.
                        int parent_score = table.GetWhiteScore(parent_tindex);
                        if (parent_score == TableGenerator.UndefinedScore)
                        {
                            int best_score = whiteChildReader.Read(child_tindex_list, parent_tindex);
                            foreach (int child_tindex in child_tindex_list)
                            {
                                int child_score = table.GetBlackScore(child_tindex);
                                if (child_score != TableGenerator.UndefinedScore)
                                {
                                    parent_score = TableGenerator.AdjustScoreForPly(child_score);
                                    if (parent_score > best_score)
                                        best_score = parent_score;
                                }
                            }
                            if (best_score == winning_score)
                            {
                                table.SetWhiteScore(parent_tindex, best_score);
                                ++white_changes;
                            }
                        }

                        // Check for winning position for Black to move.
                        parent_score = table.GetBlackScore(parent_tindex);
                        if (parent_score == TableGenerator.UndefinedScore)
                        {
                            int best_score = blackChildReader.Read(child_tindex_list, parent_tindex);
                            foreach (int child_tindex in child_tindex_list)
                            {
                                int child_score = table.GetWhiteScore(child_tindex);
                                if (child_score != TableGenerator.UndefinedScore)
                                {
                                    parent_score = TableGenerator.AdjustScoreForPly(child_score);
                                    if (parent_score > best_score)
                                        best_score = parent_score;
                                }
                            }
                            if (best_score == winning_score)
                            {
                                table.SetBlackScore(parent_tindex, best_score);
                                ++black_changes;
                            }
                        }
                    }
                }
                else
                {
                    // On even plies, we look for parent nodes with forced losses for the side to move.
                    // A forced loss occurs when ALL children have known scores, and the score
                    // is the exact score we expect for losing at the specified ply level.
                    int losing_score = TableGenerator.FriendMatedScore + ply;
                    for (int parent_tindex = 0; parent_tindex < size; ++parent_tindex)
                    {
                        // Check for losing position for White to move.
                        int parent_score = table.GetWhiteScore(parent_tindex);
                        if (parent_score == TableGenerator.UndefinedScore)
                        {
                            bool all_children_valid = true;
                            int best_score = whiteChildReader.Read(child_tindex_list, parent_tindex);
                            foreach (int child_tindex in child_tindex_list)
                            {
                                int child_score = table.GetBlackScore(child_tindex);
                                if (child_score == TableGenerator.UndefinedScore)
                                {
                                    all_children_valid = false;
                                    break;
                                }
                                parent_score = TableGenerator.AdjustScoreForPly(child_score);
                                if (parent_score > best_score)
                                    best_score = parent_score;
                            }
                            if (all_children_valid && best_score == losing_score)
                            {
                                table.SetWhiteScore(parent_tindex, best_score);
                                ++white_changes;
                            }
                        }

                        // Check for losing position for Black to move.
                        parent_score = table.GetBlackScore(parent_tindex);
                        if (parent_score == TableGenerator.UndefinedScore)
                        {
                            bool all_children_valid = true;
                            int best_score = blackChildReader.Read(child_tindex_list, parent_tindex);
                            foreach (int child_tindex in child_tindex_list)
                            {
                                int child_score = table.GetWhiteScore(child_tindex);
                                if (child_score == TableGenerator.UndefinedScore)
                                {
                                    all_children_valid = false;
                                    break;
                                }
                                parent_score = TableGenerator.AdjustScoreForPly(child_score);
                                if (parent_score > best_score)
                                    best_score = parent_score;
                            }
                            if (all_children_valid && best_score == losing_score)
                            {
                                table.SetBlackScore(parent_tindex, best_score);
                                ++black_changes;
                            }
                        }
                    }
                }
                generator.Log("ForwardPropagate({0}): white={1}, black={2}", ply, white_changes, black_changes);
                curr_progress = white_changes + black_changes;
            }
        }
    }
}
