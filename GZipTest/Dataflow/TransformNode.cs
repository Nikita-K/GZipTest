using System;
using System.Threading;
using GZipTest.Utilities;

namespace GZipTest.Dataflow
{
    public interface IDataTransformer<TInput, TOutput>
    {
        TOutput Process(TInput input); 
    }

    public class TransformNode<TInput, TOutput> : NodeBase, ITargetNode<TInput>
    {
        private ITargetNode<TOutput> _targetNode;
        private readonly BlockingQueue<TInput> _inputQueue;
        private readonly IDataTransformer<TInput, TOutput> _dataTransformer;

        public TransformNode(
            IDataTransformer<TInput, TOutput> dataTransformer, NodeOptions nodeOptions) 
            : base(nodeOptions)
        {
            _ = dataTransformer ?? throw new ArgumentNullException($@"Parameter {nameof(dataTransformer)} cannot be null.");

            _dataTransformer = dataTransformer;
            _inputQueue = _nodeOptions.BoundedCapacity < 0 ? 
                new BlockingQueue<TInput>() : new BlockingQueue<TInput>(_nodeOptions.BoundedCapacity);
        }

        public TransformNode(
            IDataTransformer<TInput, TOutput> dataTransformer) 
            : this(dataTransformer, new NodeOptions())
        {
        }

        public bool Post(TInput data)
        {
            if (State != NodeState.Running)
            {
                return false;
            }

            return _inputQueue.TryEnqueue(data);
        }

        public void LinkTo(ITargetNode<TOutput> targetNode)
        {
            _ = targetNode ?? throw new ArgumentNullException($@"Parameter {nameof(targetNode)} cannot be null.");

            _targetNode = targetNode;
        }

        protected override void Process(CancellationToken cancellationToken)
        {
            _ = _targetNode ?? throw new InvalidOperationException("The target node must be defined.");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_inputQueue.TryDequeue(out TInput sourceData))
                {
                    if (_completionTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    continue;
                }

                var processedData = _dataTransformer.Process(sourceData);
                while (!_targetNode.Post(processedData))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
    }
}
