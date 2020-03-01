using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GZipTest.Utilities;

namespace GZipTest.Pipeline.IO
{
    public class GZipDataReader : IONodeBase
    {
        public GZipDataReader(Stream ioStream, BlockingQueue<DataBlock> ioDataQueue) 
            : base(ioStream, ioDataQueue)
        {
        }

        protected override void Process(CancellationToken ct)
        {
            using (var reader = new BinaryReader(_ioStream))
            {
                var customHeaderActual = 
                    reader.ReadBytes(GZipDataWriter.CustomHeader.Length);
                var isSupportFormat = customHeaderActual.SequenceEqual(GZipDataWriter.CustomHeader);

                if(!isSupportFormat)
                {
                    throw new InvalidDataException("Unsupported file format.");
                }

                while (_ioStream.Position != _ioStream.Length)
                {
                    ct.ThrowIfCancellationRequested();

                    long blockPosition = reader.ReadInt64();
                    int blockLength = reader.ReadInt32();
                    var data = reader.ReadBytes(blockLength);

                    _ioDataCollection.Enqueue(new DataBlock(blockPosition, data), ct);

                    StatusQueue?.TryEnqueue(
                        new Status(typeof(GZipDataReader),
                            $"Read {_ioStream.Position} bytes from {_ioStream.Length}."));
                }

                // Send completion signal
                _ioDataCollection.Enqueue(new DataBlock(long.MaxValue, new byte[0]), ct);
                StatusQueue?.TryEnqueue(new Status(typeof(GZipDataReader), $"Reading completed."));
            }
        }
    }
}
