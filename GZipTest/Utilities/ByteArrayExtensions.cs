using System.IO;
using System.IO.Compression;

namespace GZipTest.Utilities
{
    public static class ByteArrayExtensions
    {
        public static byte[] Compress(this byte[] inputData)
        {
            using (var outputDataStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(outputDataStream, CompressionMode.Compress, true))
                {
                    compressionStream.Write(inputData, 0, inputData.Length);
                }
                return outputDataStream.ToArray();
            }
        }

        public static byte[] Decompress(this byte[] inputData)
        {
            using (var compressedDataStream = new MemoryStream(inputData))
            {
                using (var outputDataStream = new MemoryStream())
                { 
                    using (var decompressionStream = new GZipStream(compressedDataStream, CompressionMode.Decompress, true))
                    {
                        decompressionStream.CopyTo(outputDataStream);
                    }
                    return outputDataStream.ToArray();
                }
            }
        }
    }
}
