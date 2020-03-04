using GZipTest.Dataflow;
using GZipTest.Pipeline.IO;
using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest.Pipeline.Factory
{
    public class DataProducerFactory
    {
        public IDataProducer<DataBlock> CreateDataReader(CompressionMode mode, BinaryReader inputStream)
        {
            switch (mode)
            {
                case CompressionMode.Decompress:
                    return new GZipDataReader(inputStream);
                case CompressionMode.Compress:
                    return new UserDataReader(inputStream);
                default:
                    throw new ArgumentException($"Unsupported compression mode: {mode}");
            }
        }
    }
}
