using System;
using System.Collections.Generic;
using System.Linq;

namespace Gearbox
{
    public class CompressedTableHeader
    {
        public const string CorrectSignature = "Gearbox Endgame Table";

        public string Signature;
        public int TableSize;   // number of (White,Black) 3-byte entries in the table
        public int BlockSize;   // number of entries per compressed block
        public HuffmanNode WhiteTree;
        public HuffmanNode BlackTree;
    }

    public class HuffmanNode
    {
        public int? Score;
        public int Count;
        public HuffmanNode Left;
        public HuffmanNode Right;
    }

    public static class HuffmanEncoder
    {
        public static HuffmanNode Compile(int[][] freq)
        {
            List<HuffmanNode> tree = freq.Select(p => new HuffmanNode { Score=p[0], Count=p[1] }).ToList();
            while (tree.Count > 1)
            {
                // Always keep the tree sorted in ascending order of count.
                tree.Sort((a, b) => (a.Count - b.Count));

                // Merge the two least frequent nodes into a single node.
                HuffmanNode a = tree[0];
                HuffmanNode b = tree[1];
                tree[0] = new HuffmanNode
                {
                    Count = a.Count + b.Count,
                    Left = a,
                    Right = b,
                };
                tree.RemoveAt(1);
            }

            // The lone remaining node is the root of the tree
            return tree[0];
        }

        public static Dictionary<int, string> MakeEncoding(HuffmanNode tree)
        {
            var dict = new Dictionary<int, string>();
            Encode(dict, tree, "");
            return dict;
        }

        private static void Encode(Dictionary<int, string> dict, HuffmanNode node, string text)
        {
            if (node.Left != null)
                Encode(dict, node.Left, text + "0");

            if (node.Right != null)
                Encode(dict, node.Right, text + "1");

            if (node.Score.HasValue)
                dict.Add(node.Score.Value, text);
        }
    }
}
