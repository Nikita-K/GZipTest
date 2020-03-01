using GZipTest.Utilities;
using System;
using System.IO;
using System.Threading;

namespace GZipTest.Dataflow
{
    public interface IDataConsumer<T>
    {
        bool IsCompleted { get; }
        void Process(T data);
    }

    public class ActionNode<T> : NodeBase, ITargetNode<T>
    {
        private readonly IDataConsumer<T> _dataConsumer;
        private readonly BlockingQueue<T> _inputQueue;

        public ActionNode(IDataConsumer<T> dataConsumer, NodeOptions nodeOptions) : base(nodeOptions)
        {
            _ = dataConsumer ?? throw new ArgumentNullException($@"Parameter {nameof(dataConsumer)} cannot be null.");

            _dataConsumer = dataConsumer;
            _inputQueue = _nodeOptions.BoundedCapacity <= 0 ?
                new BlockingQueue<T>() : new BlockingQueue<T>(_nodeOptions.BoundedCapacity);
        }

        public ActionNode(IDataConsumer<T> dataConsumer) : this (dataConsumer, new NodeOptions())
        {
        }

        public bool Post(T data)
        {
            if (State != NodeState.Running)
            {
                return false;
            }

            return _inputQueue.TryEnqueue(data);
        }

        protected override void Process(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if(_inputQueue.TryDequeue(out T sourceData))
                {
                    _dataConsumer.Process(sourceData);
                    continue;
                }

                if (_completionTokenSource.IsCancellationRequested)
                {
                    if (!_dataConsumer.IsCompleted)
                    {
                        throw new InvalidDataException("Missing data packets");
                    }

                    return;
                }
            }
        }
    }
}
