using System;
using System.IO;
using System.Linq;
using GZipTest.Dataflow;

namespace GZipTest.Pipeline.IO
{
    public class GZipDataReader : IDataProducer<DataBlock>
    {
        private readonly BinaryReader _inputStream;

        public GZipDataReader(BinaryReader inputStream)
        {
            _ = inputStream ?? throw new ArgumentNullException($@"Parameter {nameof(inputStream)} cannot be null.");

            _inputStream = inputStream;
            var customHeaderActual =
                    _inputStream.ReadBytes(GZipDataWriter.CustomHeader.Length);
            var isSupportFormat = customHeaderActual.SequenceEqual(GZipDataWriter.CustomHeader);

            if (!isSupportFormat)
            {
                throw new InvalidDataException("Unsupported file format.");
            }
        }

        public DataBlock GetData()
        {
            if (_inputStream.BaseStream.Position
                == _inputStream.BaseStream.Length)
            {
                return null;
            }

            int blockPosition = _inputStream.ReadInt32();
            int blockLength = _inputStream.ReadInt32();
            var data = _inputStream.ReadBytes(blockLength);

            if(blockLength != data.Length)
            {
                throw new InvalidDataException("Unexpected end of the stream");
            }

            return new DataBlock(blockPosition, data);
        }
    }
}
