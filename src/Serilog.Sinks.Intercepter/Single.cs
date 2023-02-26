using Serilog.Events;
using System.Collections;

namespace Serilog.Sinks.Intercepter;

internal class Single : IEnumerable<LogEvent>
{
    private readonly LogEvent _logEvent;

    public Single(LogEvent logEvent) =>
        _logEvent = logEvent;

    public IEnumerator<LogEvent> GetEnumerator() => new SingleEnumerator(_logEvent);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class SingleEnumerator : IEnumerator<LogEvent>
    {
        private readonly LogEvent _logEvent;
        private ulong _index;


        public SingleEnumerator(LogEvent logEvent)
        {
            _logEvent = logEvent;
            Reset();
        }

        public LogEvent Current => _index == 1 ? _logEvent : default!;

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
        }

        public bool MoveNext() => _index++ == 0;

        public void Reset() => _index =0;
    }
}
