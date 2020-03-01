using GZipTest.Pipeline.Factory;
using GZipTest.Utilities;
using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest.Pipeline
{
    public class GZipPipeline : NodeBase
    {
        private readonly GZipOperationFactory _opFactory = new GZipOperationFactory();
        private readonly IONodeFactory _ioNodeFactory = new IONodeFactory();
        private readonly CompressionMode _mode;
        private readonly Stream _inputStream;
        private readonly Stream _outputStream;

        public GZipPipeline(CompressionMode mode, Stream inputStream, Stream outputStream)
        {
            _ = inputStream ?? throw new ArgumentNullException($@"Parameter {nameof(inputStream)} cannot be null.");
            _ = outputStream ?? throw new ArgumentNullException($@"Parameter {nameof(outputStream)} cannot be null.");

            _mode = mode;
            _inputStream = inputStream;
            _outputStream = outputStream;
        }

        // Theoretically, we can reuse nodes, 
        // but in the current task this is not necessary
        protected override void Process(CancellationToken ct)
        {
            // Set the capacity of the srcDataBlocksQueue CustomThreadPool.DataProcessQueueCapacity
            // to avoid running out of available memory.
            // At the same time, this is enough to buffer data, 
            // because a custom ThreadPool contains a maximum of Environment.ProcessorCount workers
            var srcDataBlocksQueue = new BlockingQueue<DataBlock>(CustomThreadPool.DataProcessingQueueCapacity);
            var processedDataBlocksQueue = new BlockingQueue<DataBlock>();

            var dataProducer = _ioNodeFactory.CreateDataReader(_mode, _inputStream, srcDataBlocksQueue);
            var gzipNode = new GZipNode(_opFactory.GetGZipOperation(_mode), srcDataBlocksQueue, processedDataBlocksQueue);
            var dataConsumer = _ioNodeFactory.CreateDataWriter(_mode, _outputStream, processedDataBlocksQueue);

            dataProducer.ExceptionEvent += OnNodeFailed;
            gzipNode.ExceptionEvent += OnNodeFailed;
            dataConsumer.ExceptionEvent += OnNodeFailed;

            StatusNode statusNode = null;
            var statusOption = ConfigurationManager.AppSettings["EnableStatus"];
            if (bool.TryParse(statusOption, out bool enableStatus) 
                    && enableStatus)
            {
                var statusQueue = new BlockingQueue<Status>();
                statusNode = new StatusNode(statusQueue);
                dataProducer.StatusQueue = statusQueue;
                dataConsumer.StatusQueue = statusQueue;
            }

            dataProducer.Start(ct);
            gzipNode.Start(ct);
            dataConsumer.Start(ct);
            statusNode?.Start(ct);

            dataConsumer.Completion.Join();
            statusNode?.Complete();
            statusNode?.Completion.Join();
        }

        private void OnNodeFailed(object sender, Exception ex)
        {
            _exception = ex;
            _internalTokenSource.Cancel();
        }
    }
}
