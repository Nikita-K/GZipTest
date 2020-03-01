using GZipTest.Pipeline;
using GZipTest.Pipeline.IO;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Xunit;

namespace GZipTest.Test
{
    public class GZipPipelineTests
    {
        [Fact]
        public void ShouldStopDecompressIvalidData()
        {
            var rand = new Random();
            var invalidCompressedData = new byte[UserDataReader.DefaultChunkSize];
            rand.NextBytes(invalidCompressedData);

            using (var inputStream = new MemoryStream(invalidCompressedData))
            using (var outputStream = new MemoryStream())
            {
                var compressPipeline = new GZipPipeline(CompressionMode.Decompress, inputStream, outputStream);
                compressPipeline.Start(CancellationToken.None);
                compressPipeline.Completion.Join();

                Assert.True(compressPipeline.Exception != null);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(UserDataReader.DefaultChunkSize / 2)]
        [InlineData(UserDataReader.DefaultChunkSize)]
        [InlineData(UserDataReader.DefaultChunkSize + UserDataReader.DefaultChunkSize / 2)]
        [InlineData(UserDataReader.DefaultChunkSize * 2)]
        [InlineData(UserDataReader.DefaultChunkSize * 5)]
        public void ShouldStopCompressAndDecompressData(int dataSize)
        {
            var rand = new Random();
            var sourceData = new byte[dataSize];
            rand.NextBytes(sourceData);

            byte[] compressedData;
            using (var inputStream = new MemoryStream(sourceData))
            using (var outputStream = new MemoryStream())
            {
                var compressPipeline = new GZipPipeline(CompressionMode.Compress, inputStream, outputStream);
                compressPipeline.Start(CancellationToken.None);
                compressPipeline.Completion.Join();

                compressedData = outputStream.ToArray();
            }

            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var outputStream = new MemoryStream())
            {
                var decompressPipeline = new GZipPipeline(CompressionMode.Decompress, inputStream, outputStream);
                decompressPipeline.Start(CancellationToken.None);
                decompressPipeline.Completion.Join();

                decompressedData = outputStream.ToArray();
            }

            Assert.Equal(sourceData, decompressedData);
        }
    }
}
