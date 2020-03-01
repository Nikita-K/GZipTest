using GZipTest.Utilities;
using System.Text;
using Xunit;

namespace GZipTest.Test
{
    public class ByteArayExtensionsTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("Test string")]
        public void ShouldCompressString(string expectString)
        {
            byte[] bytesToCompress = Encoding.ASCII.GetBytes(expectString);
            var compressedBytes = bytesToCompress.Compress();
            var decompressedBytes = compressedBytes.Decompress();
            var actualString = Encoding.ASCII.GetString(decompressedBytes);

            Assert.Equal(expectString, actualString);
            Assert.NotEqual(bytesToCompress, compressedBytes);
            Assert.NotEqual(compressedBytes, decompressedBytes);
        }
    }
}
