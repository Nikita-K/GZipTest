using GZipTest.Utilities;
using System;
using System.Threading;

namespace GZipTest.Pipeline
{
    public abstract class NodeBase
    {
        protected CancellationTokenSource _internalTokenSource;
        protected CancellationTokenSource _commonTokenSource;

        protected Thread _nodeThread;
        public Thread Completion => _nodeThread;

        protected Exception _exception;
        public Exception Exception => _exception;

        public event EventHandler<Exception> ExceptionEvent;

        public BlockingQueue<Status> StatusQueue { get; set; }

        public void Start(CancellationToken ct = default(CancellationToken))
        {
            _internalTokenSource = new CancellationTokenSource();
            _commonTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct, _internalTokenSource.Token);

            _nodeThread = new Thread(() => {
                try
                {
                    Process(_commonTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _exception = ex;
                    OnExceptionEvent(this, ex);
                }
            });
            _nodeThread.Start();
        }

        public void Complete()
        {
            _internalTokenSource.Cancel();
        }

        protected void OnExceptionEvent(object sender, Exception ex)
        {
            ExceptionEvent?.Invoke(this, ex);
        }

        protected abstract void Process(CancellationToken ct);
    }
}
