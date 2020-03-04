using GZipTest.Dataflow;
using GZipTest.Pipeline.Factory;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest.Pipeline
{
    public sealed class GZipPipeline : IDisposable
    {
        private bool _disposed = false;
        private IDataProducer<DataBlock> _dataProducer;
        private TransformNode<DataBlock, DataBlock> _transformNode;
        private ActionNode<DataBlock> _consumerNode;

        private CancellationTokenSource _internalTokenSource;
        private CancellationTokenSource _commonTokenSource;

        private readonly BinaryReader _inputStream;
        private readonly BinaryWriter _outputStream;
        private readonly Stopwatch _stopWatch;

        private ConsoleWriter _consoleWriter;
        private ActionNode<string> _statusNode;

        public Exception Exception { get; private set; }

        public GZipPipeline(CompressionMode mode, Stream inputStream, Stream outputStream)
        {
            _ = inputStream ?? throw new ArgumentNullException($@"Parameter {nameof(inputStream)} cannot be null.");
            _ = outputStream ?? throw new ArgumentNullException($@"Parameter {nameof(outputStream)} cannot be null.");

            _inputStream = new BinaryReader(inputStream);
            _outputStream = new BinaryWriter(outputStream);
            _stopWatch = new Stopwatch();
            _internalTokenSource = new CancellationTokenSource();

            Init(mode);
        }

        public void Process(CancellationToken ct)
        {
            _commonTokenSource = 
                CancellationTokenSource.CreateLinkedTokenSource(ct, _internalTokenSource.Token);
            var commonToken = _commonTokenSource.Token;

            _transformNode.Start(commonToken);
            _consumerNode.Start(commonToken);
            _statusNode?.Start(commonToken);

            _stopWatch.Start();

            while (true)
            {
                commonToken.ThrowIfCancellationRequested();

                try
                {
                    var dataBlock = _dataProducer.GetData();

                    _statusNode?.Post(
                        $@"Read {_inputStream.BaseStream.Position} bytes from {_inputStream.BaseStream.Length}.
Elapsed time: {_stopWatch.Elapsed}");

                    if (dataBlock == null)
                    {
                        _transformNode.Complete();
                        _statusNode?.Complete();
                        break;
                    }

                    while (!_transformNode.Post(dataBlock))
                    {
                        commonToken.ThrowIfCancellationRequested();
                    }
                }
                catch (Exception ex)
                {
                    PipelineFailed(this, ex);
                    break;
                }
            }

            _stopWatch.Stop();
            _consoleWriter?.Print(
                        $@"Reading completed.
Elapsed time: {_stopWatch.Elapsed}");

            _consumerNode.Wait();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #region Private region

        private void Init(CompressionMode mode)
        {
            var transformNodeOptions = new NodeOptions
            {
                MaxDegreeOfParallelism = (uint)Environment.ProcessorCount,
                BoundedCapacity = Environment.ProcessorCount * 2
            };

            var actionNodeOptions = new NodeOptions
            {
                BoundedCapacity = Environment.ProcessorCount * 2
            };

            var producerFactory = new DataProducerFactory();
            var transformerFactory = new DataTransformerFactory();
            var consumerFactory = new DataConsumerFactory();

            _dataProducer = producerFactory.CreateDataReader(mode, _inputStream);

            _transformNode = new TransformNode<DataBlock, DataBlock>(
                transformerFactory.CreateDataTransformer(mode), transformNodeOptions);
            _consumerNode = new ActionNode<DataBlock>(
                consumerFactory.CreateDataWriter(mode, _outputStream), actionNodeOptions);

            _transformNode.ExceptionEvent += PipelineFailed;
            _consumerNode.ExceptionEvent += PipelineFailed;

            _transformNode.LinkTo(_consumerNode);
            _transformNode.CompletionAction = () => { _consumerNode.Complete(); };

            var statusOption = ConfigurationManager.AppSettings["EnableStatus"];
            if (bool.TryParse(statusOption, out bool enableStatus)
                    && enableStatus)
            {
                _consoleWriter = new ConsoleWriter();
                _statusNode = new ActionNode<string>(_consoleWriter, new NodeOptions { BoundedCapacity = 1 });
            }
        }

        private void PipelineFailed(object sender, Exception ex)
        {
            if(Exception != null)
            {
                return;
            }

            Exception = ex;
            _internalTokenSource.Cancel();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _inputStream?.Dispose();
                _outputStream?.Dispose();

                _internalTokenSource?.Dispose();
                _internalTokenSource?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
