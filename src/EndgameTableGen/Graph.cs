using System;
using System.Collections.Generic;
using System.IO;

namespace EndgameTableGen
{
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

        public void GetAfterTableIndexes(List<int> outlist, int before_table_index)
        {
            outlist.Clear();
            int position = EdgeFileOffset(before_table_index);
            if (position >= 0)
            {
                edgeFile.Seek(8L * position, SeekOrigin.Begin);
                if (ReadEdge(out GraphEdge edge))
                {
                    // The very first edge should have a matching before_tindex.
                    if (edge.before_tindex != before_table_index)
                        throw new Exception($"Expected before_tindex={before_table_index}, but found {edge.before_tindex} in file: {edgeFileName}");

                    outlist.Add(edge.before_tindex);
                    while (ReadEdge(out edge) && (edge.before_tindex == before_table_index))
                        outlist.Add(edge.after_tindex);
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

        private int EdgeFileOffset(int before_table_index)
        {
            if (before_table_index < 0 || before_table_index >= indexLength)
                throw new ArgumentException($"Invalid before_table_index={before_table_index}");
            indexFile.Seek(4L * before_table_index, SeekOrigin.Begin);
            int nread = indexFile.Read(buffer, 0, 4);
            if (nread != 4)
                throw new Exception($"Read incorrect number of bytes: {nread}");

            int position = ((int)buffer[0] << 24) | ((int)buffer[1] << 16) | ((int)buffer[2] << 8) | (int)buffer[3];
            return position;
        }

    }

    internal static class IndexWriter
    {
        public static void MakeIndex(int table_size, string edgeFileName, string outIndexFileName)
        {
            // Pack the spread files back into the original single file.
            using (var edgeReader = new EdgeReader(edgeFileName))
            using (var indexWriter = File.OpenWrite(outIndexFileName))
            {
                int prev_tindex = -1;
                int offset = 0;
                while (edgeReader.ReadEdge(out GraphEdge edge))
                {
                    if (edge.before_tindex != prev_tindex)
                    {
                        // Verify that we really have sorted!
                        // There are multiple edges with the same before_tindex,
                        // and we leave before_tindex in whatever random order they settle.
                        if (edge.before_tindex < prev_tindex)
                            throw new Exception($"Sort failure in {edgeFileName}");

                        // Time to write another index record.
                        // Pad with -1 values to fill invalid/unused slots.
                        for (int tindex = prev_tindex + 1; tindex < edge.before_tindex; ++tindex)
                            WriteIndex(indexWriter, -1);

                        WriteIndex(indexWriter, offset);
                        prev_tindex = edge.before_tindex;
                    }

                    ++offset;
                }

                // Pad out the index file so that any valid table_index has a placeholder (-1) entry.
                for (int tindex = prev_tindex + 1; tindex < table_size; ++tindex)
                    WriteIndex(indexWriter, -1);
            }
        }

        private static readonly byte[] IndexBuffer = new byte[4];

        private static void WriteIndex(FileStream indexWriter, int offset)
        {
            IndexBuffer[0] = (byte)(offset >> 24);
            IndexBuffer[1] = (byte)(offset >> 16);
            IndexBuffer[2] = (byte)(offset >> 8);
            IndexBuffer[3] = (byte)(offset);
            indexWriter.Write(IndexBuffer, 0, 4);
        }
    }
}
