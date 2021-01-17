using System;
using System.Collections.Generic;
using System.IO;

namespace EndgameTableGen
{
    internal class ChildWriter : IDisposable
    {
        internal const int ChildRecordBytes = 4;
        internal const int IndexRecordBytes = 7;

        private FileStream outChildFile;
        private FileStream outIndexFile;
        private readonly List<int> childTableIndexList = new();   // eliminates duplicate transitions caused by symmetry.
        private int childFileOffset;
        private int indexFileOffset;
        private byte[] data = MakeDataBuffer();
        private byte[] nil = MakeDataBuffer();

        private static byte[] MakeDataBuffer()
        {
            // Make a data buffer that can hold the following struct:
            // uint8  num_children
            // int32  child_file_offset
            // int16  best_foreign_score
            byte[] data = new byte[IndexRecordBytes];

            // Pre-populate with a nil record.

            // num_children = 0
            data[0] = 0x00;

            // child_file_offset = -1
            data[1] = 0xff;
            data[2] = 0xff;
            data[3] = 0xff;
            data[4] = 0xff;

            // best_foreign_score = UndefinedScore
            data[5] = (byte)(TableGenerator.UndefinedScore & 0xff);
            data[6] = (byte)((TableGenerator.UndefinedScore >> 8) & 0xff);

            return data;
        }

        public ChildWriter(string childFileName, string indexFileName)
        {
            outChildFile = File.Create(childFileName);
            outIndexFile = File.Create(indexFileName);
            childFileOffset = 0;
        }

        public void Dispose()
        {
            if (outChildFile != null)
            {
                outChildFile.Dispose();
                outChildFile = null;
            }

            if (outIndexFile != null)
            {
                outIndexFile.Dispose();
                outIndexFile = null;
            }
        }

        public void BeginParent(int parent_tindex)
        {
            if (parent_tindex < indexFileOffset)
                throw new ArgumentException($"parent_tindex = {parent_tindex} comes before most recent index {indexFileOffset-1}. Should have been in sorted order.");

            // Pad the index file with filler slots so that
            // the ChildReader can seek directly to the correct location in it.
            while (indexFileOffset < parent_tindex)
            {
                outIndexFile.Write(nil, 0, 7);
                ++indexFileOffset;
            }
        }

        public void AppendChild(int child_tindex)
        {
            // More than one legal move can lead to the same table index, due to symmetry.
            // Save only distinct child table indexes.
            if (!childTableIndexList.Contains(child_tindex))
                childTableIndexList.Add(child_tindex);
        }

        public void FinishParent(int best_foreign_score)
        {
            if (childTableIndexList.Count > byte.MaxValue)
                throw new Exception($"Invalid number of children = {childTableIndexList.Count}");

            if (best_foreign_score < short.MinValue || best_foreign_score > short.MaxValue)
                throw new Exception($"Invalid best_foreign_score = {best_foreign_score}");

            // Not strictly necessary, but I want to make this algorithm
            // immune to any changes in how the legal move generator orders its output.
            // This could help with debugging later.
            childTableIndexList.Sort();

            // Write list of children to child file.
            foreach (int child_tindex in childTableIndexList)
            {
                data[0] = (byte)(child_tindex);
                data[1] = (byte)(child_tindex >> 8);
                data[2] = (byte)(child_tindex >> 16);
                data[3] = (byte)(child_tindex >> 24);
                outChildFile.Write(data, 0, ChildRecordBytes);
            }

            // Write index record to index file.

            data[0] = (byte)childTableIndexList.Count;

            if (childTableIndexList.Count == 0)
            {
                // When there are no child table indexes for a parent node,
                // it makes more sense to be a little cautious: put -1 as the offset.
                data[1] = 0xff;
                data[2] = 0xff;
                data[3] = 0xff;
                data[4] = 0xff;
            }
            else
            {
                data[1] = (byte)(childFileOffset);
                data[2] = (byte)(childFileOffset >> 8);
                data[3] = (byte)(childFileOffset >> 16);
                data[4] = (byte)(childFileOffset >> 24);
            }

            data[5] = (byte)(best_foreign_score);
            data[6] = (byte)(best_foreign_score >> 8);

            outIndexFile.Write(data, 0, IndexRecordBytes);
            ++indexFileOffset;

            childFileOffset += childTableIndexList.Count;
            childTableIndexList.Clear();
        }
    }

    internal class ChildReader : IDisposable
    {
        private byte[] data = new byte[ChildWriter.IndexRecordBytes];
        private FileStream childFile;
        private FileStream indexFile;

        public ChildReader(string childFileName, string indexFileName)
        {
            childFile = File.OpenRead(childFileName);
            indexFile = File.OpenRead(indexFileName);
        }

        public void Dispose()
        {
            if (childFile != null)
            {
                childFile.Dispose();
                childFile = null;
            }

            if (indexFile != null)
            {
                indexFile.Dispose();
                indexFile = null;
            }
        }

        public int Read(List<int> childTableIndexList, int parent_tindex)
        {
            childTableIndexList.Clear();

            // Read the index record.
            long index_position = (long)ChildWriter.IndexRecordBytes * parent_tindex;
            long check = indexFile.Seek(index_position, SeekOrigin.Begin);
            if (check != index_position)
                throw new Exception($"Cannot seek to position {index_position} in index file.");
            int nbytes = indexFile.Read(data, 0, ChildWriter.IndexRecordBytes);
            if (nbytes != ChildWriter.IndexRecordBytes)
                throw new Exception($"Tried to read {ChildWriter.IndexRecordBytes} bytes from index file, but received {nbytes}.");

            // Decode the index record.
            int numChildren = (int)data[0];
            int childFileOffset = ((int)data[1]) | ((int)data[2] << 8) | ((int)data[3] << 16) | ((int)data[4] << 24);
            int bestForeignScore = (int)((short)data[5] | ((short)(data[6] << 8)));

            if (numChildren > 0)        // don't bother seeking if not going to read anything
            {
                if (childFileOffset < 0)
                    throw new Exception($"Negative child file offset {childFileOffset}, but numChildren={numChildren} is positive.");

                // Seek to the beginning of the child tindex values.
                long child_position = (long)ChildWriter.ChildRecordBytes * childFileOffset;
                check = childFile.Seek(child_position, SeekOrigin.Begin);
                if (check != child_position)
                    throw new Exception($"Cannot seek to position {child_position} in child file.");

                // Read the list of child tindex values.
                for (int i = 0; i < numChildren; ++i)
                {
                    nbytes = childFile.Read(data, 0, ChildWriter.ChildRecordBytes);
                    if (nbytes != ChildWriter.ChildRecordBytes)
                        throw new Exception($"Tried to read {ChildWriter.ChildRecordBytes} bytes from child file, but received {nbytes}.");

                    int child_tindex = ((int)data[0]) | ((int)data[1] << 8) | ((int)data[2] << 16) | ((int)data[3] << 24);
                    childTableIndexList.Add(child_tindex);
                }
            }

            return bestForeignScore;
        }
    }
}
