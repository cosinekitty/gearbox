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
        (before(config_id, white_to_move, table_index), after(config_id, white_to_move, table_index))
        to disk.
        Sort and index the resulting list by the "after" tuple.
        Therefore, given any after-position, we can find a list of before-positions
        that precede it by one ply.
        If one side is checkmated in the after-position, then all the corresponding
        before-positions can be marked as mate-in-one positions.
        Iterating one ply at a time, we can work backwards and fill in the whole table.

        Tricky: sometimes the after-position config_id transitions into another table
        and we have to compute the inverse position (swapping White and Black).
        That is why we have to store white_to_move in both "before" and "after" nodes.
    */

    internal struct GraphNode
    {
        public long config_id;
        public bool white_to_move;
        public int  table_index;
    }

    internal struct GraphEdge
    {
        public GraphNode before;    // the state of the board before the move
        public GraphNode after;     // the state of the board after the move
    }

    internal class EdgeWriter : IDisposable
    {
        internal const int BytesPerNode = 9;    // see explanation in WriteNode()
        internal const int BytesPerEdge = 2 * BytesPerNode;
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
            WriteNode(edge.before);
            WriteNode(edge.after);
        }

        private void WriteNode(GraphNode node)
        {
            // Serialize the GraphNode struct to an array of bytes.
            // Encoding:
            // config_id is a value 0..9,999,999,999.
            // Therefore it can fit in 34 bits.
            // Store it in 5 bytes, and use the high bit to store white_to_move also.
            buffer[nbytes++] = (byte)((node.white_to_move ? 0x80 : 0x00) | (byte)(node.config_id >> 32));
            buffer[nbytes++] = (byte)(node.config_id >> 24);
            buffer[nbytes++] = (byte)(node.config_id >> 16);
            buffer[nbytes++] = (byte)(node.config_id >> 8);
            buffer[nbytes++] = (byte)(node.config_id);

            // Store the table index in 4 bytes.
            // This means we need a total of 9 bytes per edge.
            buffer[nbytes++] = (byte)(node.table_index >> 24);
            buffer[nbytes++] = (byte)(node.table_index >> 16);
            buffer[nbytes++] = (byte)(node.table_index >> 8);
            buffer[nbytes++] = (byte)(node.table_index);

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
            nbytes = infile.Read(buffer, 0, buffer.Length);
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
                edge = new GraphEdge();
                return false;   // end of file
            }

            edge.before = ReadNode();
            edge.after = ReadNode();
            return true;
        }

        private GraphNode ReadNode()
        {
            var node = new GraphNode();

            node.white_to_move = (0 != (buffer[offset] & 0x80));

            node.config_id |= ((long)(buffer[offset++] & 0x7f)) << 32;
            node.config_id |= ((long)buffer[offset++]) << 24;
            node.config_id |= ((long)buffer[offset++]) << 16;
            node.config_id |= ((long)buffer[offset++]) << 8;
            node.config_id |= ((long)buffer[offset++]);

            node.table_index |= ((int)buffer[offset++]) << 24;
            node.table_index |= ((int)buffer[offset++]) << 16;
            node.table_index |= ((int)buffer[offset++]) << 8;
            node.table_index |= ((int)buffer[offset++]);

            return node;
        }
    }
}
