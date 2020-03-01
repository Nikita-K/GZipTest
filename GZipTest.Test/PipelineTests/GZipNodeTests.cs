using System;
using System.Threading;
using System.Collections.Generic;
using System.IO.Compression;
using GZipTest.Utilities;
using GZipTest.Pipeline;
using GZipTest.Pipeline.Factory;
using Xunit;
using System.Linq;

namespace GZipTest.Test
{
    public class GZipNodeTests
    {
        private readonly GZipOperationFactory _opFactory = new GZipOperationFactory();
        private readonly DataBlock _emptyDataBlock;
        private BlockingQueue<DataBlock> _inputDataQueue;
        private BlockingQueue<DataBlock> _processedDataQueue;

        public GZipNodeTests()
        {
            _emptyDataBlock = new DataBlock(long.MaxValue, new byte[0]);
            _inputDataQueue = new BlockingQueue<DataBlock>();
            _processedDataQueue = new BlockingQueue<DataBlock>();
        }

        [Theory]
        [MemberData(nameof(GetArgs))]
        public void ShouldThrowExceptionIfConstructorArgIsNull(
            BlockingQueue<DataBlock> inputDataQueue, BlockingQueue<DataBlock>  compressedDataQueue)
        {
            Assert.Throws<ArgumentNullException>(() => {
                new GZipNode(_opFactory.GetGZipOperation(CompressionMode.Compress), 
                    inputDataQueue, compressedDataQueue);
            });
        }

        [Fact]
        public void ShouldStopAfterReceivingEmptyBlock()
        {
            var compresssionNode = new GZipNode(
                _opFactory.GetGZipOperation(CompressionMode.Compress), 
                _inputDataQueue, _processedDataQueue);
            compresssionNode.Start(CancellationToken.None);

            // Check that node started
            Assert.True(compresssionNode.Completion.IsAlive);

            _inputDataQueue.Enqueue(new DataBlock(0, new byte[0]));
            Thread.Sleep(10);

            Assert.False(compresssionNode.Completion.IsAlive);
        }

        [Fact]
        public void ShouldStopAfterCallingComplete()
        {
            var compresssionNode = new GZipNode(
                _opFactory.GetGZipOperation(CompressionMode.Compress),
                _inputDataQueue, _processedDataQueue);
            compresssionNode.Start(CancellationToken.None);

            // Check that node started
            Assert.True(compresssionNode.Completion.IsAlive);

            compresssionNode.Complete();
            Thread.Sleep(100);

            Assert.False(compresssionNode.Completion.IsAlive);
        }

        [Fact]
        public void ShouldStopAfterCallingCancell()
        {
            var tokenSource = new CancellationTokenSource();
            var compresssionNode = new GZipNode(
                _opFactory.GetGZipOperation(CompressionMode.Compress),
                _inputDataQueue, _processedDataQueue);
            compresssionNode.Start(tokenSource.Token);

            // Check that node started
            Assert.True(compresssionNode.Completion.IsAlive);

            tokenSource.Cancel();
            Thread.Sleep(100);

            Assert.False(compresssionNode.Completion.IsAlive);
        }

        [Theory]
        [MemberData(nameof(GetSourceBlocks))]
        public void ShouldCompressBloksQueue(List<DataBlock> sourceBlocks)
        {
            _inputDataQueue = new BlockingQueue<DataBlock>(sourceBlocks);
            _inputDataQueue.Enqueue(_emptyDataBlock);

            var compresssionNode = new GZipNode(
                _opFactory.GetGZipOperation(CompressionMode.Compress),
                _inputDataQueue, _processedDataQueue);

            compresssionNode.Start(CancellationToken.None);
            compresssionNode.Completion.Join();

            Assert.Equal(sourceBlocks.Count + 1, _processedDataQueue.Count);
            foreach (var compressedData in _processedDataQueue.ToArray())
            {
                if(compressedData.Data.Length == 0)
                {
                    continue;
                }

                var sourceBlock = sourceBlocks.Find((DataBlock item) => { return item.Position == compressedData.Position; });
                Assert.NotNull(sourceBlock);
                Assert.Equal(sourceBlock.Data.Compress(), compressedData.Data);
            }
        }

        [Theory]
        [MemberData(nameof(GetCompressedBlocks))]
        public void ShouldDecompressBloksQueue(List<DataBlock> compressedBlocks)
        {
            _inputDataQueue = new BlockingQueue<DataBlock>(compressedBlocks);
            _inputDataQueue.Enqueue(_emptyDataBlock);

            var compresssionNode = new GZipNode(
                _opFactory.GetGZipOperation(CompressionMode.Decompress),
                _inputDataQueue, _processedDataQueue);

            compresssionNode.Start(CancellationToken.None);
            compresssionNode.Completion.Join();

            Assert.Equal(compressedBlocks.Count + 1, _processedDataQueue.Count);
            foreach (var decompressedData in _processedDataQueue.ToArray())
            {
                if (decompressedData.Data.Length == 0)
                {
                    continue;
                }

                var sourceBlock = compressedBlocks.Find((DataBlock item) => { return item.Position == decompressedData.Position; });
                Assert.NotNull(sourceBlock);
                Assert.Equal(sourceBlock.Data.Decompress(), decompressedData.Data);
            }
        }

        public static IEnumerable<object[]> GetArgs()
        {
            yield return new object[] { null, null };
            yield return new object[] { new BlockingQueue<DataBlock>(), null };
            yield return new object[] { null, new BlockingQueue<DataBlock>() };
        }

        public static IEnumerable<object[]> GetSourceBlocks()
        {
            int maxPacketsNum = 5;
            int minDataLen = 1;
            int maxDataLen = 1000;
            Random rnd = new Random();

            for (uint i = 0; i < maxPacketsNum; i++)
            {
                var dataBlocks = new List<DataBlock>();
                for (int j = 0; j < i; j++)
                {
                    var data = new byte[rnd.Next(minDataLen, maxDataLen)];
                    rnd.NextBytes(data);
                    dataBlocks.Add(new DataBlock(j, data));
                }

                yield return new object[] { dataBlocks };
            }
        }

        public static IEnumerable<object[]> GetCompressedBlocks()
        {
            foreach (var arg in GetSourceBlocks())
            {
                var sourceBloks = arg[0] as List<DataBlock>;
                var compresssedBlocks = new List<DataBlock>();
                compresssedBlocks = sourceBloks.Select(x => new DataBlock(x.Position, x.Data.Compress())).ToList();

                yield return new object[] { compresssedBlocks };
            }
        }
    }
}
