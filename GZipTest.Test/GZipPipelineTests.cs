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
        public void ShouldStopDecompressDataWithoutCustomHeader()
        {
            var rand = new Random();
            var invalidCompressedData = new byte[UserDataReader.DefaultChunkSize];
            rand.NextBytes(invalidCompressedData);

            using (var inputStream = new MemoryStream(invalidCompressedData))
            using (var outputStream = new MemoryStream())
            {
                Assert.Throws<InvalidDataException>(
                    () => { new GZipPipeline(CompressionMode.Decompress, inputStream, outputStream); });
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(UserDataReader.DefaultChunkSize / 2)]
        [InlineData(UserDataReader.DefaultChunkSize)]
        [InlineData(UserDataReader.DefaultChunkSize + UserDataReader.DefaultChunkSize / 2)]
        [InlineData(UserDataReader.DefaultChunkSize * 2)]
        [InlineData(UserDataReader.DefaultChunkSize * 5)]
        public void ShouldStopDecompressIvalidData(int dataSize)
        {
            var rand = new Random();
            var sourceData = new byte[dataSize];
            rand.NextBytes(sourceData);

            byte[] compressedData;
            using (var inputStream = new MemoryStream(sourceData))
            using (var outputStream = new MemoryStream())
            using (var compressPipeline = 
                new GZipPipeline(CompressionMode.Compress, inputStream, outputStream))
            {
                compressPipeline.Process(CancellationToken.None);
                compressedData = outputStream.ToArray();
            }

            var customHeaderLength = GZipDataWriter.CustomHeader.Length;
            var idx = rand.Next(customHeaderLength, compressedData.Length / 2);
            rand.NextBytes(new Span<byte>(compressedData, idx, compressedData.Length - idx));

            using (var inputStream = new MemoryStream(compressedData))
            using (var outputStream = new MemoryStream())
            using (var decompressPipeline = 
                new GZipPipeline(CompressionMode.Decompress, inputStream, outputStream))
            {
                decompressPipeline.Process(CancellationToken.None);
                Assert.True(decompressPipeline.Exception != null);
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
        public void ShouldCompressAndDecompressData(int dataSize)
        {
            var rand = new Random();
            var sourceData = new byte[dataSize];
            rand.NextBytes(sourceData);

            byte[] compressedData;
            using (var inputStream = new MemoryStream(sourceData))
            using (var outputStream = new MemoryStream())
            using (var compressPipeline = 
                new GZipPipeline(CompressionMode.Compress, inputStream, outputStream))
            {
                compressPipeline.Process(CancellationToken.None);
                compressedData = outputStream.ToArray();
            }

            byte[] decompressedData;
            using (var inputStream = new MemoryStream(compressedData))
            using (var outputStream = new MemoryStream())
            using (var decompressPipeline = 
                new GZipPipeline(CompressionMode.Decompress, inputStream, outputStream))
            {
                decompressPipeline.Process(CancellationToken.None);
                decompressedData = outputStream.ToArray();
            }

            Assert.Equal(sourceData, decompressedData);
        }
    }
}
