using GZipTest.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Pipeline
{
    public class Status
    {
        public Type NodeType { get; }
        public string Message { get; }

        public Status(Type nodeType, string message)
        {
            NodeType = nodeType;
            Message = message;
        }
    }

    public class StatusNode : NodeBase
    {
        private const int ReportIntervalSec = 1;
        private const int SleepIntervalMs = 500;

        private readonly TimeSpan _reportTimeInterval = new TimeSpan(0, 0, ReportIntervalSec);
        private readonly Dictionary<Type, string> _summaryStatus = new Dictionary<Type, string>();
        private BlockingQueue<Status> _statusQueue;

        public StatusNode(BlockingQueue<Status> statusQueue)
        {
            _ = statusQueue ?? throw new ArgumentNullException($@"Parameter {nameof(statusQueue)} cannot be null");

            _statusQueue = statusQueue;
        }

        protected override void Process(CancellationToken ct)
        {
            var timeStart = DateTime.Now;
            var lastReportTime = new DateTime();
            var cursorPosition = new Tuple<int, int>(Console.CursorLeft, Console.CursorTop);
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                while(_statusQueue.TryDequeue(out Status status))
                {
                    _summaryStatus[status.NodeType] = status.Message;
                }

                var currentTime = DateTime.Now;
                if(currentTime - lastReportTime > _reportTimeInterval)
                {
                    lastReportTime = currentTime;

                    Console.SetCursorPosition(cursorPosition.Item1, cursorPosition.Item2);
                    foreach (var statusItem in _summaryStatus)
                    {
                        Console.Write(new string(' ', Console.BufferWidth));
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.WriteLine($"{statusItem.Key.Name}: {statusItem.Value}");
                    }
                    Console.WriteLine($"Elapsed time: {currentTime - timeStart}");

                    Thread.Sleep(SleepIntervalMs);
                }
            }
        }
    }
}
