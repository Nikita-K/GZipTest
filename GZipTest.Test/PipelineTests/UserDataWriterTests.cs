using GZipTest.Pipeline.IO;
using GZipTest.Pipeline;
using GZipTest.Utilities;
using System;
using System.IO;
using System.Threading;
using Xunit;
using GZipTest.Test.Helpers;

namespace GZipTest.Test
{
    public class UserDataWriterTests : IDisposable
    {
        private readonly BlockingQueue<DataBlock> _inputDataQueue;
        private MemoryStream _memoryStream;

        public UserDataWriterTests()
        {
            _memoryStream = new MemoryStream();
            _inputDataQueue = new BlockingQueue<DataBlock>();
        }

        [Fact]
        public void ShouldStopAfterReadingZeroBytes()
        {
            var dataConsumerr = new UserDataWriter(_memoryStream, _inputDataQueue);

            dataConsumerr.Start();
            Assert.True(dataConsumerr.Completion.IsAlive);

            _inputDataQueue.Enqueue(new DataBlock(long.MaxValue, new byte[0]));
            Thread.Sleep(10);

            Assert.False(dataConsumerr.Completion.IsAlive);
        }

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(5, 10)]
        public void ShouldWriteBlocksFromQueueToStream(int blocksCount, int chunkSize)
        {
            var expectBlocksList = DataProvider.PrepareDataBlocks(blocksCount, chunkSize);
            var expectData = new byte[chunkSize * blocksCount];

            int offset = 0;
            foreach (var block in expectBlocksList)
            {
                _inputDataQueue.Enqueue(block);
                Buffer.BlockCopy(block.Data, 0,
                   expectData, offset, block.Data.Length);
                offset += block.Data.Length;
            }

            var dataConsumer = new UserDataWriter(_memoryStream, _inputDataQueue);
            dataConsumer.Start();
            dataConsumer.Completion.Join();

            var actualData = _memoryStream.ToArray();

            Assert.Equal(expectData, actualData);
        }

        public void Dispose()
        {
            _memoryStream.Dispose();
        }
    }
}
