using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Dataflow
{
    public interface ITargetNode<T>
    {
        bool Post(T data);
    }

    public interface IDataProducer<T>
    {
        T GetData();
    }

    public class NodeOptions
    {
        public uint MaxDegreeOfParallelism { get; set; } = 1;
        public int BoundedCapacity { get; set; } = -1; // negative or zero means unlimited size
    }

    public enum NodeState
    {
        Created,
        Running,
        Сompleted,
        Faulted
    }

    public abstract class NodeBase
    {
        protected NodeOptions _nodeOptions;

        private CancellationTokenSource _commonTokenSource;
        private CancellationTokenSource _workerFailTokenSource = new CancellationTokenSource();
        protected CancellationTokenSource _completionTokenSource = new CancellationTokenSource();

        private uint _completedThreads = 0;
        private readonly object _lockObject = new object();
        private readonly List<Thread> _threads = new List<Thread>();

        #region Properties

        public Exception Exception { get; private set; }
        public NodeState State { get; private set; } = NodeState.Created;
        public Action CompletionAction { get; set; }
        
        #endregion

        public event EventHandler<Exception> ExceptionEvent;

        public NodeBase(NodeOptions nodeOptions)
        {
            _ = nodeOptions ?? throw new ArgumentNullException($@"Parameter {nameof(nodeOptions)} cannot be null.");

            _nodeOptions = nodeOptions;
        }

        public void Start(CancellationToken userToken = default(CancellationToken))
        {
            if (State != NodeState.Created)
            {
                throw new InvalidOperationException("Node is already running");
            }

            AddWorkers(userToken);

            foreach (var thread in _threads)
            {
                thread.Start();
            }

            State = NodeState.Running;
        }

        public void Complete()
        {
            if (State == NodeState.Created)
            {
                throw new InvalidOperationException("Node is not running yet");
            }

            _completionTokenSource.Cancel();
        }

        public void Wait()
        {
            if(State == NodeState.Created)
            {
                throw new InvalidOperationException("Node is not running yet");
            }

            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }

        #region Protected methods

        protected abstract void Process(CancellationToken ct);

        protected void OnExceptionEvent(object sender, Exception ex)
        {
            if (State == NodeState.Faulted)
            {
                return;
            }

            lock (_lockObject)
            {
                // Let's keep only first exception
                if (State != NodeState.Faulted)
                {
                    Exception = ex;
                    State = NodeState.Faulted;

                    _workerFailTokenSource.Cancel();
                    ExceptionEvent?.Invoke(this, ex);
                }
            }
        }

        #endregion

        #region Privete methods

        private void AddWorkers(CancellationToken userToken)
        {
            _commonTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                userToken, _workerFailTokenSource.Token);

            Thread CreateWorker()
            {
                return new Thread(() =>
                {
                    try
                    {
                        Process(_commonTokenSource.Token);
                        lock (_lockObject)
                        {
                            _completedThreads++;
                            if (_completedThreads == _nodeOptions.MaxDegreeOfParallelism)
                            {
                                State = NodeState.Сompleted;
                                CompletionAction?.Invoke();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnExceptionEvent(this, ex);
                    }
                });
            }

            for (int i = 0; i < _nodeOptions.MaxDegreeOfParallelism; i++)
            {
                _threads.Add(CreateWorker());
            }
        }

        #endregion
    }
}
