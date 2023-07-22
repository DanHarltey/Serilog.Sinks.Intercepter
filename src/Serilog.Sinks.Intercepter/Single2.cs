using Serilog.Events;
using System.Collections;

namespace Serilog.Sinks.Intercepter;

internal sealed class Single2 : IEnumerable<LogEvent>, IEnumerator<LogEvent>
{
    private readonly LogEvent _logEvent;

    public Single2(LogEvent logEvent)
    {
        _logEvent = logEvent;
        _state = State.Initial;
    }

    public IEnumerator<LogEvent> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private enum State
    {
        Initial,
        Active,
        Complete
    }

    private State _state;

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
