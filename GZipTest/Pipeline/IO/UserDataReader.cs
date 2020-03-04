using GZipTest.Dataflow;
using System;
using System.IO;

namespace GZipTest.Pipeline.IO
{
    public class UserDataReader : IDataProducer<DataBlock>
    {
        public const int DefaultChunkSize = 1048576;

        private int _blockPosition = 0;
        private readonly int _chunkSize;
        private readonly BinaryReader _inputStream;

        public UserDataReader(BinaryReader inputStream, int chunkSize = DefaultChunkSize)
        {
            _ = inputStream ?? throw new ArgumentNullException($@"Parameter {nameof(inputStream)} cannot be null.");
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size cannot be less than 1.");
            }

            _chunkSize = chunkSize;
            _inputStream = inputStream;
        }

        public DataBlock GetData()
        {
            if(_inputStream.BaseStream.Position 
                == _inputStream.BaseStream.Length)
            {
                return null;
            }

            var data = _inputStream.ReadBytes(_chunkSize);
            return new DataBlock(_blockPosition++, data);
        }
    }
}
