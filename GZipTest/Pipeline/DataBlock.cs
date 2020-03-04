using System;
using System.Collections.Generic;
using System.Linq;

namespace GZipTest.Pipeline
{
    public class DataBlock
    {
        public int Position { get; }
        public byte[] Data { get; }

        public DataBlock(int position, byte[] data)
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException($@"Parameter {nameof(position)} cannot be negative.");
            }

            Position = position;
            Data = data;
        }

        public override bool Equals(object obj)
        {
            var block = obj as DataBlock;
            return block != null &&
                   Position == block.Position &&
                   Data.SequenceEqual(block.Data);
        }

        public override int GetHashCode()
        {
            var hashCode = 636864127;
            hashCode = hashCode * -1521134295 + Position.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Data);
            return hashCode;
        }
    }
}
