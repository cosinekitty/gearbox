using System;
using System.Collections.Generic;
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

        public override string ToString() => $"{before_tindex}:{after_tindex}";
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

    internal class EdgeIndexer : IDisposable
    {
        private string indexFileName;
        private string edgeFileName;
        private long indexLength;
        private FileStream indexFile;
        private FileStream edgeFile;
        private byte[] buffer = new byte[8];

        public EdgeIndexer(string indexFileName, string edgeFileName)
        {
            this.indexFileName = indexFileName;
            this.edgeFileName = edgeFileName;
            this.indexFile = File.OpenRead(indexFileName);
            this.edgeFile = File.OpenRead(edgeFileName);
            this.indexLength = this.indexFile.Length / 4;
        }

        public void Dispose()
        {
            if (edgeFile != null)
            {
                edgeFile.Dispose();
                edgeFile = null;
            }
        }

        public void GetBeforeTableIndexes(List<int> outlist, int after_table_index)
        {
            outlist.Clear();
            int position = EdgeFileOffset(after_table_index);
            if (position >= 0)
            {
                edgeFile.Seek(8L * position, SeekOrigin.Begin);
                if (ReadEdge(out GraphEdge edge))
                {
                    // The very first edge should have a matching after_tindex.
                    if (edge.after_tindex != after_table_index)
                        throw new Exception($"Expected after_tindex={after_table_index}, but found {edge.after_tindex} in file: {edgeFileName}");

                    outlist.Add(edge.before_tindex);
                    while (ReadEdge(out edge) && (edge.after_tindex == after_table_index))
                        outlist.Add(edge.before_tindex);
                }
            }
        }

        private bool ReadEdge(out GraphEdge edge)
        {
            // We have a somewhat redundant implementation of EdgeReader.ReadEdge() here.
            // But that one is optimized for reading large batches.
            // This one reads just a single edge from the file.
            // We are seeking around and reading tiny snippets of the file, not 80K blocks.

            int nread = edgeFile.Read(buffer, 0, 8);
            if (nread != 8)
            {
                edge.before_tindex = -1;
                edge.after_tindex = -1;
                return false;
            }

            edge.after_tindex = (((int)buffer[0]) << 24) | (((int)buffer[1]) << 16) | (((int)buffer[2]) << 8) | ((int)buffer[3]);
            edge.before_tindex = (((int)buffer[4]) << 24) | (((int)buffer[5]) << 16) | (((int)buffer[6]) << 8) | ((int)buffer[7]);
            return true;
        }

        private int EdgeFileOffset(int after_table_index)
        {
            if (after_table_index < 0 || after_table_index >= indexLength)
                throw new ArgumentException($"Invalid after_table_index={after_table_index}");
            indexFile.Seek(4L * after_table_index, SeekOrigin.Begin);
            int nread = indexFile.Read(buffer, 0, 4);
            if (nread != 4)
                throw new Exception($"Read incorrect number of bytes: {nread}");

            int position = ((int)buffer[0] << 24) | ((int)buffer[1] << 16) | ((int)buffer[2] << 8) | (int)buffer[3];
            return position;
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

        public void Sort(string edgeFileName, string outIndexFileName)
        {
            // This is a radix sort, so that we can sort efficiently without using a lot of memory.

            // Spread the one input files into 'radix' piles, based on the final digit.
            using (var reader = new EdgeReader(edgeFileName))
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
            MoveFilesForNextGeneration();
            OpenInputFiles();
            using (var indexWriter = File.OpenWrite(outIndexFileName))
            {
                int prev_tindex = -1;
                using (var edgeWriter = new EdgeWriter(edgeFileName))
                {
                    int offset = 0;         // number of edges written to the sorted output edge file
                    for (int inDigit = 0; inDigit < radix; ++inDigit)
                    {
                        while (readerForDigit[inDigit].ReadEdge(out GraphEdge edge))
                        {
                            if (edge.after_tindex != prev_tindex)
                            {
                                // Verify that we really have sorted!
                                // There are multiple edges with the same after_tindex,
                                // and we leave before_tindex in whatever random order they settle.
                                if (edge.after_tindex < prev_tindex)
                                    throw new Exception($"Sort failure in {edgeFileName}");

                                // Time to write another index record.
                                // Pad with -1 values to fill invalid/unused slots.
                                for (int tindex = prev_tindex + 1; tindex < edge.after_tindex; ++tindex)
                                    WriteIndex(indexWriter, -1);

                                WriteIndex(indexWriter, offset);
                            }

                            edgeWriter.WriteEdge(edge);
                            prev_tindex = edge.after_tindex;
                            ++offset;
                        }
                    }
                }

                // Pad out the index file so that any valid table_index has a placeholder (-1) entry.
                for (int tindex = prev_tindex + 1; tindex < table_size; ++tindex)
                    WriteIndex(indexWriter, -1);
            }
            CloseInputFiles();
            DeleteInputFiles();
        }

        private readonly byte[] IndexBuffer = new byte[4];

        private void WriteIndex(FileStream indexWriter, int offset)
        {
            IndexBuffer[0] = (byte)(offset >> 24);
            IndexBuffer[1] = (byte)(offset >> 16);
            IndexBuffer[2] = (byte)(offset >> 8);
            IndexBuffer[3] = (byte)(offset);
            indexWriter.Write(IndexBuffer, 0, 4);
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

        private void DeleteInputFiles()
        {
            for (int digit = 0; digit < radix; ++digit)
            {
                string inFileName = InWorkFileName(digit);
                File.Delete(inFileName);
            }
        }
    }
}
