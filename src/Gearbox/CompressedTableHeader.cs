namespace Gearbox
{
    public class CompressedTableHeader
    {
        public const string CorrectSignature = "Gearbox Endgame Table";

        public string Signature { get; set; }
        public int TableSize { get; set; }   // number of (White,Black) 3-byte entries in the table
        public int BlockSize { get; set; }   // number of entries per compressed block
        public int[][] WhiteFrequency { get; set; }       // A list of pairs [score, count]
        public int[][] BlackFrequency { get; set; }       // A list of pairs [score, count]
    }
}
