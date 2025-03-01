using Serilog.Events;

namespace Serilog.Sinks.Intercepter;

/// <summary>
/// Intercepts log events for modifying, adding, filtering, or buffering.
/// </summary>
public interface IIntercepter
{
    /// <summary>
    /// Tests the <paramref name="logEvent"/> to determine if it should be rejected.
    /// </summary>
    /// <param name="logEvent">The item to be tested for rejection.</param>
    /// <returns>false to <see cref="IIntercepter.Intercept(LogEvent)"/> the <paramref name="logEvent"/>; otherwise, true.</returns>
    bool Reject(LogEvent logEvent);

    /// <summary>
    /// Intercepts the <paramref name="logEvent"/> for modifying, adding, filtering, or buffering.
    /// </summary>
    /// <param name="logEvent"></param>
    /// <returns></returns>
    IEnumerable<LogEvent> Intercept(LogEvent logEvent);
}
