using GZipTest.Dataflow;
using GZipTest.Pipeline.IO;
using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest.Pipeline.Factory
{
    public class DataConsumerFactory
    {
        public IDataConsumer<DataBlock> CreateDataWriter(CompressionMode mode, BinaryWriter outputStream)
        {
            switch (mode)
            {
                case CompressionMode.Decompress:
                    return new UserDataWriter(outputStream);
                case CompressionMode.Compress:
                    return new GZipDataWriter(outputStream);
                default:
                    throw new ArgumentException($"Unsupported compression mode: {mode}");
            }
        }
    }
}
