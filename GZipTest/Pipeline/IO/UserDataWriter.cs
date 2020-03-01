using GZipTest.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTest.Pipeline.IO
{
    public class UserDataWriter : IONodeBase
    {
        public UserDataWriter(Stream ioStream, BlockingQueue<DataBlock> ioDataQueue) 
            : base(ioStream, ioDataQueue)
        {
        }

        protected override void Process(CancellationToken ct)
        {
            using (var writer = new BinaryWriter(_ioStream))
            {
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
                        StatusQueue?.TryEnqueue(new Status(typeof(UserDataWriter), $"Writing completed."));
                        return;
                    }

                    writer.BaseStream.Seek(dataBlock.Position, SeekOrigin.Begin);
                    writer.Write(dataBlock.Data);
                    StatusQueue?.TryEnqueue(
                        new Status(typeof(UserDataWriter), $"{_ioStream.Position} bytes written."));
                }
            }
        }
    }
}
