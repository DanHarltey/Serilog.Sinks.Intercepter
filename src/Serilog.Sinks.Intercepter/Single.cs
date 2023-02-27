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

    enum State
    {
        Initial,
        Active,
        Complete
    }

    private class SingleEnumerator : IEnumerator<LogEvent>
    {
        private readonly LogEvent _logEvent;
        private State _state;


        public SingleEnumerator(LogEvent logEvent)
        {
            _logEvent = logEvent;
        }

        public LogEvent Current => _state == State.Active ? _logEvent : default!;

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_state == State.Initial)
            {
                _state = State.Active;
                return true;
            }

            _state = State.Complete;
            return false;
        }

        public void Reset() => _state = State.Initial;
    }
}
