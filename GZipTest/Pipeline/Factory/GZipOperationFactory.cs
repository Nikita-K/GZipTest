using GZipTest.Utilities;
using System;
using System.IO.Compression;

namespace GZipTest.Pipeline.Factory
{
    public class GZipOperationFactory
    {
        public Func<byte[], byte[]> GetGZipOperation(CompressionMode mode)
        {
            switch (mode)
            {
                case CompressionMode.Decompress:
                    return (byte[] inputData) => { return inputData.Decompress(); };
                case CompressionMode.Compress:
                    return (byte[] inputData) => { return inputData.Compress(); };
                default:
                    throw new ArgumentException($"Unsupported compression mode: {mode}");
            }
        }
    }
}
