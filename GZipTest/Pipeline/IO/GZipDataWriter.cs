using System.IO;
using System.Text;
using System.Threading;
using GZipTest.Utilities;

namespace GZipTest.Pipeline.IO
{
    public class GZipDataWriter : IONodeBase
    {
        public static readonly byte[] CustomHeader = Encoding.ASCII.GetBytes("magic string");

        public GZipDataWriter(Stream ioStream, BlockingQueue<DataBlock> ioDataQueue) 
            : base(ioStream, ioDataQueue)
        {
        }

        protected override void Process(CancellationToken ct)
        {
            using (var writer = new BinaryWriter(_ioStream))
            {
                writer.Write(CustomHeader);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!_ioDataCollection.TryDequeue(out DataBlock dataBlock))
                    {
                        continue;
                    }

                    // Received packet signifying completion of input
                    if (dataBlock.Data.Length == 0)
                    {
                        StatusQueue?.TryEnqueue(new Status(typeof(GZipDataWriter), $"Writing completed."));
                        return;
                    }

                    writer.Write(dataBlock.Position);
                    writer.Write(dataBlock.Data.Length);
                    writer.Write(dataBlock.Data);

                    StatusQueue?.TryEnqueue(
                        new Status(typeof(GZipDataWriter), $"{_ioStream.Position} bytes written."));
                }
            }
        }
    }
}
