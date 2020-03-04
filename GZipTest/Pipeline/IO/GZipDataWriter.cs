using GZipTest.Dataflow;
using System;
using System.IO;
using System.Text;

namespace GZipTest.Pipeline.IO
{
    public class GZipDataWriter : IDataConsumer<DataBlock>
    {
        public static readonly byte[] CustomHeader = Encoding.ASCII.GetBytes("magic string");
        public readonly BinaryWriter _outputStream;

        public GZipDataWriter(BinaryWriter outputStream)
        {
            _ = outputStream ?? throw new ArgumentNullException($@"Parameter {nameof(outputStream)} cannot be null.");

            _outputStream = outputStream;
            _outputStream.Write(CustomHeader);
        }

        public bool IsCompleted => true;

        public void Process(DataBlock dataBlock)
        {
            _outputStream.Write(dataBlock.Position);
            _outputStream.Write(dataBlock.Data.Length);
            _outputStream.Write(dataBlock.Data);
        }
    }
}
