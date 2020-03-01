using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Utilities
{
    public class CustomThreadPool
    {
        private readonly CancellationToken _ct;
        private readonly object _lockObj = new object();
        private readonly List<Thread> _threads = new List<Thread>();
        protected CancellationTokenSource _endWorkTokenSource = new CancellationTokenSource();

        private static readonly int _workerThreadsMax = Environment.ProcessorCount;
        private readonly BlockingQueue<Action> _workItemsQueue 
                                = new BlockingQueue<Action>(DataProcessingQueueCapacity);

        // Let's try to create a stock of tasks: _workerThreadsMax * 2
        public static readonly int DataProcessingQueueCapacity = _workerThreadsMax * 2;

        public Exception LastException { get; private set; }

        public CustomThreadPool(CancellationToken ct)
        {
            _ct = ct;
        }

        public void QueueWorkItem(Action workItem, CancellationToken ct)
        {
            // Trying to use a simple rule. 
            // If the task queue has fewer elements than threads, 
            // then there are enough threads. Just add a task.
            if (_workItemsQueue.Count < _threads.Count
                    || _threads.Count == _workerThreadsMax)
            {
                _workItemsQueue.Enqueue(workItem, ct);
                return;
            }

            lock (_lockObj)
            {
                // Double checking
                if (_threads.Count != _workerThreadsMax)
                {
                    AddWorker();
                }
            }

            // In any case, the task must be queued, even if it is blocked
            _workItemsQueue.Enqueue(workItem, ct);
        }

        public void WaitAll()
        {
            _endWorkTokenSource.Cancel();
            lock (_lockObj)
            {
                foreach (var thr in _threads)
                {
                    thr.Join();
                }
            }

            _endWorkTokenSource = new CancellationTokenSource();
            _threads.Clear();
        }

        #region Private methods

        private void AddWorker()
        {
            var endWorkToken = _endWorkTokenSource.Token;
            var thr = new Thread(() => {
                while (!_ct.IsCancellationRequested)
                {
                    if (!_workItemsQueue.TryDequeue(out Action work))
                    {
                        if (endWorkToken.IsCancellationRequested)
                        {
                            return;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    try
                    {
                        work();
                    }
                    catch (Exception ex)
                    {
                        LastException = ex;
                    }
                }
            });

            thr.Start();
            _threads.Add(thr);
        }

        #endregion
    }
}
