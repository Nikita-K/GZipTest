using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Utilities
{
    public class BlockingQueue<T>
    {
        public const int WaitTimeMs = 10;

        private readonly int _sizeMax;
        private readonly Queue<T> _queue;
        private readonly T _defaultObj = default(T);

        private readonly object _lockQueueObj = new object();
        private readonly SemaphoreSlim _pushEventSemaphore;
        private readonly SemaphoreSlim _popEventSemaphore;

        public BlockingQueue()
        {
            // Negative maxSize means unlimited size
            _sizeMax = -1;
            _pushEventSemaphore = new SemaphoreSlim(0);
            _popEventSemaphore = null;
            _queue = new Queue<T>();
        }

        public BlockingQueue(int sizeMax)
        {
            if (sizeMax <= 0)
            {
                throw new ArgumentOutOfRangeException($@"Parameter {nameof(sizeMax)} should be greater than zero");
            }

            _sizeMax = sizeMax;
            _pushEventSemaphore = new SemaphoreSlim(0, _sizeMax);
            _popEventSemaphore = new SemaphoreSlim(_sizeMax, _sizeMax);
            _queue = new Queue<T>(_sizeMax);
        }

        public BlockingQueue(ICollection<T> collection) : this()
        {
            _ = collection ?? throw new ArgumentNullException(
                    $@"Parameter {nameof(collection)} cannot be null");

            foreach (var item in collection)
            {
                DoEnqueue(item);
            }
        }

        public int Count => _queue.Count;

        public bool TryEnqueue(T item)
        {
            var canEnqueue = _popEventSemaphore?.Wait(WaitTimeMs);
            if (!canEnqueue.HasValue || canEnqueue.Value)
            {
                DoEnqueue(item);
                return true;
            }

            return false;
        }

        public void Enqueue(T item, CancellationToken ct = default(CancellationToken))
        {
            _popEventSemaphore?.Wait(Timeout.Infinite, ct);

            DoEnqueue(item);
        }

        public bool TryDequeue(out T item)
        {
            if (_pushEventSemaphore.Wait(WaitTimeMs))
            {
                DoDequeue(out item);
                return true;
            }

            item = _defaultObj;
            return false;
        }

        public void Dequeue(out T item, CancellationToken ct = default(CancellationToken))
        {
            _pushEventSemaphore.Wait(Timeout.Infinite, ct);

            DoDequeue(out item);
        }

        public T[] ToArray()
        {
            lock (_lockQueueObj)
            {
                return _queue.ToArray();
            }
        }

        #region Private methods

        private void DoEnqueue(T item)
        {
            lock (_lockQueueObj)
            {
                _queue.Enqueue(item);
                _pushEventSemaphore.Release();
            }
        }

        private void DoDequeue(out T item)
        {
            lock (_lockQueueObj)
            {
                item = _queue.Dequeue();
                _popEventSemaphore?.Release();
            }
        }

        #endregion
    }
}
