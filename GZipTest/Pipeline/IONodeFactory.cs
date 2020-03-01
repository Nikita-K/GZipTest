using GZipTest.Pipeline.IO;
using GZipTest.Utilities;
using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest.Pipeline
{
    public class IONodeFactory
    {
        public IONodeBase CreateDataReader(CompressionMode mode, Stream ioStream, BlockingQueue<DataBlock> ioDataQueue)
        {
            switch (mode)
            {
                case CompressionMode.Decompress:
                    return new GZipDataReader(ioStream, ioDataQueue);
                case CompressionMode.Compress:
                    return new UserDataReader(ioStream, ioDataQueue);
                default:
                    throw new ArgumentException($"Unsupported compression mode: {mode}");
            }
        }

        public IONodeBase CreateDataWriter(CompressionMode mode, Stream ioStream, BlockingQueue<DataBlock> ioDataQueue)
        {
            switch (mode)
            {
                case CompressionMode.Decompress:
                    return new UserDataWriter(ioStream, ioDataQueue);
                case CompressionMode.Compress:
                    return new GZipDataWriter(ioStream, ioDataQueue);
                default:
                    throw new ArgumentException($"Unsupported compression mode: {mode}");
            }
        }
    }
}
