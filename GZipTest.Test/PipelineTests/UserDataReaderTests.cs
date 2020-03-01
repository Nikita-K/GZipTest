using GZipTest.Pipeline.IO;
using GZipTest.Pipeline;
using GZipTest.Utilities;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace GZipTest.Test
{
    public class UserDataReaderTests : IDisposable
    {
        private MemoryStream _memoryStream;
        private readonly BlockingQueue<DataBlock> _processedDataQueue;

        public UserDataReaderTests()
        {
            _processedDataQueue = new BlockingQueue<DataBlock>();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void ShouldThrowExceptionIfValueOfChunkSizeLessThanOne(int invalidChunkSize)
        {
            _memoryStream = new MemoryStream();
            Assert.Throws<ArgumentException>(() => {
                new UserDataReader(_memoryStream, _processedDataQueue, invalidChunkSize);
            });
        }

        [Fact]
        public void ShouldStopAfterReadingZeroBytes()
        {
            _memoryStream = new MemoryStream(new byte[0]);
            var dataProducer = new UserDataReader(_memoryStream, _processedDataQueue);

            dataProducer.Start();
            Thread.Sleep(10);

            Assert.False(dataProducer.Completion.IsAlive);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(1024)]
        public void ShouldProduceBlocksWithSizeLessThanOrEqualToChunkSize(int chunkSize)
        {
            var expectData = PrepareData(chunkSize);
            var actualData = new byte[expectData.Length];
            _memoryStream = new MemoryStream(expectData);
            var dataProducer = new UserDataReader(_memoryStream, _processedDataQueue, chunkSize);

            dataProducer.Start();
            dataProducer.Completion.Join();

            int offset = 0;
            foreach (var dataBlock in _processedDataQueue.ToArray())
            {
                Assert.True(dataBlock.Data.Length <= chunkSize);
                Buffer.BlockCopy(dataBlock.Data, 0, 
                    actualData, offset, dataBlock.Data.Length);
                offset += dataBlock.Data.Length;
            }

            Assert.True(actualData.Length > 0);
            Assert.Equal(expectData, actualData);
        }

        private byte[] PrepareData(int chunkSize)
        {
            var rnd = new Random();
            var dataLength = rnd.Next(0, 100) * chunkSize + rnd.Next(0, chunkSize);
            var data = new byte[dataLength];
            rnd.NextBytes(data);

            return data;
        }

        public void Dispose()
        {
            _memoryStream?.Dispose();
        }
    }
}
