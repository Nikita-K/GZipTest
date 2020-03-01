using System;
using System.Threading;
using GZipTest.Utilities;

namespace GZipTest.Pipeline
{
    public class GZipNode : NodeBase
    {
        private readonly Func<byte[], byte[]> _gzipOperation;
        private readonly BlockingQueue<DataBlock> _inputDataQueue;
        private readonly BlockingQueue<DataBlock> _outputDataQueue;

        public GZipNode(Func<byte[], byte[]> operation,
            BlockingQueue<DataBlock> inputDataQueue,
            BlockingQueue<DataBlock> outputDataQueue)
        {
            _ = operation ?? throw new ArgumentNullException($@"Parameter {nameof(operation)} cannot be null.");
            _ = inputDataQueue ?? throw new ArgumentNullException($@"Parameter {nameof(inputDataQueue)} cannot be null.");
            _ = outputDataQueue ?? throw new ArgumentNullException($@"Parameter {nameof(outputDataQueue)} cannot be null.");

            _gzipOperation = operation;
            _inputDataQueue = inputDataQueue;
            _outputDataQueue = outputDataQueue;
        }

        protected override void Process(CancellationToken ct)
        {
            var customThreadPool = new CustomThreadPool(ct);
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (!_inputDataQueue.TryDequeue(out DataBlock dataBlock))
                {
                    continue;
                }

                if (dataBlock.Data.Length == 0)
                {
                    // Received packet signifying completion of input
                    customThreadPool.WaitAll();
                    if(customThreadPool.LastException != null)
                    {
                        OnExceptionEvent(this, customThreadPool.LastException);
                    }

                    _outputDataQueue.Enqueue(dataBlock, ct);
                    return;
                }

                customThreadPool.QueueWorkItem(() =>
                {
                    try
                    {
                        var processedData = _gzipOperation.Invoke(dataBlock.Data);
                        _outputDataQueue.Enqueue(new DataBlock(dataBlock.Position, processedData), ct);
                    }
                    catch (Exception ex)
                    {
                        OnExceptionEvent(this, ex);
                    }
                }, ct);
            }
        }
    }
}
