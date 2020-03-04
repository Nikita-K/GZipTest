using GZipTest.Dataflow;
using System;

namespace GZipTest.Pipeline
{
    public class ConsoleWriter : IDataConsumer<string>
    {
        private DateTime _lastReportTime = new DateTime();
        private readonly TimeSpan _reportTimeInterval = new TimeSpan(0, 0, 1);
        private readonly Tuple<int, int> _cursorPosition = 
            new Tuple<int, int>(Console.CursorLeft, Console.CursorTop);

        public void Print(string message)
        {
            _lastReportTime = DateTime.Now;

            Console.SetCursorPosition(_cursorPosition.Item1, _cursorPosition.Item2);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.WriteLine(message);
        }

        public void PrintIfNeeded(string message)
        {
            if (DateTime.Now - _lastReportTime > _reportTimeInterval)
            {
                Print(message);
            }
        }

        #region IDataConsumer

        public bool IsCompleted => true;

        public void Process(string message)
        {
            PrintIfNeeded(message);
        }

        #endregion
    }
}
