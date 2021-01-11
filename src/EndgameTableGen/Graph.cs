using System;
using System.IO;

namespace EndgameTableGen
{
    /*
        We need an memory-conserving inverse lookup:
        Given an "after move" position, find all "before move"
        positions that have legal moves leading to that "after move" position.
        In graph theory terms, positions are nodes and legal moves are edges.

        Strategy:
        We generate all legal positions.
        For each position, generate all legal moves.
        For each legal move, write the tuple
        (white_to_move, before_tindex, after_tindex)
        to disk.
        Sort and index the resulting list by the "after" tuple.
        Therefore, given any after-position, we can find a list of before-positions
        that precede it by one ply.
        If one side is checkmated in the after-position, then all the corresponding
        before-positions can be marked as mate-in-one positions.
        Iterating one ply at a time, we can work backwards and fill in the whole table.
    */

    internal struct GraphEdge
    {
        public int  before_tindex;
        public int  after_tindex;
    }

    internal class EdgeWriter : IDisposable
    {
        internal const int BytesPerEdge = 8;
        internal const int EdgesPerBuffer = 10000;
        internal const int BytesPerBuffer = BytesPerEdge * EdgesPerBuffer;

        private FileStream outfile;
        private byte[] buffer = new byte[BytesPerBuffer];
        private int nbytes;

        public EdgeWriter(string filename)
        {
            outfile = File.Create(filename);
        }

        public void Dispose()
        {
            if (outfile != null)
            {
                if (nbytes > 0)
                    outfile.Write(buffer, 0, nbytes);

                outfile.Dispose();
                outfile = null;
            }
        }

        public void WriteEdge(GraphEdge edge)
        {
            buffer[nbytes++] = (byte)(edge.after_tindex >> 24);
            buffer[nbytes++] = (byte)(edge.after_tindex >> 16);
            buffer[nbytes++] = (byte)(edge.after_tindex >> 8);
            buffer[nbytes++] = (byte)(edge.after_tindex);

            buffer[nbytes++] = (byte)(edge.before_tindex >> 24);
            buffer[nbytes++] = (byte)(edge.before_tindex >> 16);
            buffer[nbytes++] = (byte)(edge.before_tindex >> 8);
            buffer[nbytes++] = (byte)(edge.before_tindex);

            if (nbytes == BytesPerBuffer)
            {
                outfile.Write(buffer, 0, BytesPerBuffer);
                nbytes = 0;
            }
        }
    }

    internal class EdgeReader : IDisposable
    {
        private FileStream infile;
        private byte[] buffer = new byte[EdgeWriter.BytesPerBuffer];
        private int nbytes;
        private int offset;

        public EdgeReader(string filename)
        {
            infile = File.OpenRead(filename);
        }

        public void Dispose()
        {
            if (infile != null)
            {
                infile.Dispose();
                infile = null;
            }
        }

        public bool ReadEdge(out GraphEdge edge)
        {
            if (offset == nbytes)
            {
                nbytes = infile.Read(buffer, 0, buffer.Length);
                offset = 0;
            }

            if (offset + EdgeWriter.BytesPerEdge > nbytes)
            {
                edge.after_tindex = -1;
                edge.before_tindex = -1;
                return false;   // end of file
            }

            edge.after_tindex = ReadNode();
            edge.before_tindex = ReadNode();
            return true;
        }

        private int ReadNode()
        {
            int tindex = ((int)buffer[offset++]) << 24;
            tindex |= ((int)buffer[offset++]) << 16;
            tindex |= ((int)buffer[offset++]) << 8;
            tindex |= ((int)buffer[offset++]);
            return tindex;
        }
    }

    internal class EdgeFileSorter : IDisposable
    {
        private readonly string work_dir;
        private readonly int radix;
        private readonly int table_size;     // the number of entries in the table, NOT the size in bytes.
        private readonly EdgeReader[] readerForDigit;
        private readonly EdgeWriter[] writerForDigit;

        public EdgeFileSorter(string work_dir, int radix, int table_size)
        {
            this.work_dir = work_dir;
            this.radix = radix;
            this.table_size = table_size;
            this.readerForDigit = new EdgeReader[radix];
            this.writerForDigit = new EdgeWriter[radix];
        }

        public void Dispose()
        {
            CloseOutputFiles();
            CloseInputFiles();
        }

        public void Sort(string filename)
        {
            Directory.CreateDirectory(work_dir);

            // This is a radix sort, so that we can sort efficiently without using a lot of memory.

            // Spread the one input files into 'radix' piles, based on the final digit.
            using (var reader = new EdgeReader(filename))
            {
                OpenOutputFiles();
                Spread(reader, 1);
                CloseOutputFiles();
            }

            // Keep respreading to the next pile.
            int residue = table_size / radix;
            for (int power = radix; residue > 0; power *= radix, residue /= radix)
            {
                MoveFilesForNextGeneration();
                OpenInputFiles();
                OpenOutputFiles();

                for (int inDigit = 0; inDigit < radix; ++inDigit)
                    Spread(readerForDigit[inDigit], power);

                CloseOutputFiles();
                CloseInputFiles();
            }

            // Pack the spread files back into the original single file.
            // FIXFIXFIX - create index here too?
            MoveFilesForNextGeneration();
            OpenInputFiles();
            using (var writer = new EdgeWriter(filename))
            {
                int prev_tindex = -1;
                for (int inDigit = 0; inDigit < radix; ++inDigit)
                {
                    while (readerForDigit[inDigit].ReadEdge(out GraphEdge edge))
                    {
                        // Verify that we really have sorted!
                        // There are multiple edges with the same after_tindex,
                        // and we leave before_tindex in whatever random order they settle.
                        if (edge.after_tindex < prev_tindex)
                            throw new Exception($"Sort failure in {filename}");

                        writer.WriteEdge(edge);
                        prev_tindex = edge.after_tindex;
                    }
                }
            }
            CloseInputFiles();

            Directory.Delete(work_dir, true);
        }

        private void DisposeArray<T>(T[] array) where T : class, IDisposable
        {
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] != null)
                {
                    array[i].Dispose();
                    array[i] = null;
                }
            }
        }

        private string InWorkFileName(int digit)
        {
            return Path.Combine(work_dir, digit.ToString("D2") + ".in");
        }

        private string OutWorkFileName(int digit)
        {
            return Path.Combine(work_dir, digit.ToString("D2") + ".out");
        }

        private void OpenInputFiles()
        {
            for (int digit = 0; digit < radix; ++digit)
                readerForDigit[digit] = new EdgeReader(InWorkFileName(digit));
        }

        private void OpenOutputFiles()
        {
            for (int digit = 0; digit < radix; ++digit)
                writerForDigit[digit] = new EdgeWriter(OutWorkFileName(digit));
        }

        private void CloseInputFiles()
        {
            DisposeArray(readerForDigit);
        }

        private void CloseOutputFiles()
        {
            DisposeArray(writerForDigit);
        }

        private void Spread(EdgeReader reader, int power)
        {
            while (reader.ReadEdge(out GraphEdge edge))
            {
                int outDigit = (edge.after_tindex / power) % radix;
                writerForDigit[outDigit].WriteEdge(edge);
            }
        }

        private void MoveFilesForNextGeneration()
        {
            for (int digit = 0; digit < radix; ++digit)
            {
                string inFileName = InWorkFileName(digit);
                string outFileName = OutWorkFileName(digit);
                File.Move(outFileName, inFileName, true);
            }
        }
    }
}
