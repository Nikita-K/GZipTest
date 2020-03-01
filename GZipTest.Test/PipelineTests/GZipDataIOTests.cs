using GZipTest.Pipeline;
using GZipTest.Pipeline.IO;
using GZipTest.Test.Helpers;
using GZipTest.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GZipTest.Test.PipelineTests
{
    public class GZipDataIOTests
    {
        private readonly BlockingQueue<DataBlock> _inputDataQueue;
        private readonly BlockingQueue<DataBlock> _outputDataQueue;

        public GZipDataIOTests()
        {
            _inputDataQueue = new BlockingQueue<DataBlock>();
            _outputDataQueue = new BlockingQueue<DataBlock>();
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(1, 10)]
        [InlineData(10, 10)]
        [InlineData(10, 1)]
        public void ShouldWriteAndReadDataBlocks(int blocksCount, int chunkSize)
        {
            var expectBlocks = DataProvider.PrepareDataBlocks(blocksCount, chunkSize);
            var expectData = new byte[chunkSize * blocksCount];

            int offset = 0;
            foreach (var block in expectBlocks)
            {
                _inputDataQueue.Enqueue(block);
                Buffer.BlockCopy(block.Data, 0,
                   expectData, offset, block.Data.Length);
                offset += block.Data.Length;
            }

            byte[] gzipData;
            using (var outputStream = new MemoryStream())
            {
                var gzipDataWriter = new GZipDataWriter(outputStream, _inputDataQueue);
                gzipDataWriter.Start();
                gzipDataWriter.Completion.Join();

                gzipData = outputStream.ToArray();
            }

            DataBlock[] actualBlocks;
            using (var inputStream = new MemoryStream(gzipData))
            {
                var gzipDataReader = new GZipDataReader(inputStream, _outputDataQueue);
                gzipDataReader.Start();
                gzipDataReader.Completion.Join();

                actualBlocks = _outputDataQueue.ToArray();
            }

            Assert.Equal(expectBlocks, actualBlocks);
        }
    }
}
