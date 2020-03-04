using GZipTest.Dataflow;
using System;
using System.IO.Compression;

namespace GZipTest.Pipeline.Factory
{
    public class DataTransformerFactory
    {
        public IDataTransformer<DataBlock, DataBlock> CreateDataTransformer(CompressionMode mode)
        {
            switch (mode)
            {
                case CompressionMode.Decompress:
                    return new Decompressor();
                case CompressionMode.Compress:
                    return new Compressor();
                default:
                    throw new ArgumentException($"Unsupported compression mode: {mode}");
            }
        }
    }
}
