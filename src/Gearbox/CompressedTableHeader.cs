namespace Gearbox
{
    public class CompressedTableHeader
    {
        public const string CorrectSignature = "Gearbox Endgame Table";

        public string Signature;
        public int TableSize;   // number of (White,Black) 3-byte entries in the table
        public int BlockSize;   // number of entries per compressed block
        public int[][] WhiteTree;
        public int[][] BlackTree;
    }
}
