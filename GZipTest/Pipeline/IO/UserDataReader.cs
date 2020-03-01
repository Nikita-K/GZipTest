using GZipTest.Utilities;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTest.Pipeline.IO
{
    public class UserDataReader : IONodeBase
    {
        public const int DefaultChunkSize = 1048576;

        private int _chunkSize;
        private long _blockPosition = 0;

        public UserDataReader(Stream ioStream, BlockingQueue<DataBlock> ioDataQueue, int chunkSize = DefaultChunkSize)
            : base(ioStream, ioDataQueue)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size cannot be less than 1.");
            }

            _chunkSize = chunkSize;
        }

        protected override void Process(CancellationToken ct)
        {
            using (var reader = new BinaryReader(_ioStream))
            {
                while (_ioStream.Position != _ioStream.Length)
                {
                    ct.ThrowIfCancellationRequested();
                    var data = reader.ReadBytes(_chunkSize);
                    _ioDataCollection.Enqueue(new DataBlock(_blockPosition, data), ct);
                    _blockPosition += data.Length;

                    StatusQueue?.TryEnqueue(
                        new Status(typeof(UserDataReader),
                            $"Read {_ioStream.Position} bytes from {_ioStream.Length}."));
                }

                // Send completion signal
                _ioDataCollection.Enqueue(new DataBlock(long.MaxValue, new byte[0]), ct);
                StatusQueue?.TryEnqueue(new Status(typeof(UserDataReader), $"Reading completed."));
            }
        }
    }
}
