using GZipTest.Utilities;
using System;
using System.IO;

namespace GZipTest.Pipeline.IO
{
    public abstract class IONodeBase : NodeBase
    {
        protected readonly Stream _ioStream;
        protected readonly BlockingQueue<DataBlock> _ioDataCollection;

        public IONodeBase(Stream ioStream, BlockingQueue<DataBlock> ioDataQueue)
        {
            _ = ioStream ?? throw new ArgumentNullException($@"Parameter {nameof(ioStream)} cannot be null.");
            _ = ioDataQueue ?? throw new ArgumentNullException($@"Parameter {nameof(ioDataQueue)} cannot be null.");

            _ioStream = ioStream;
            _ioDataCollection = ioDataQueue;
        }
    }
}
