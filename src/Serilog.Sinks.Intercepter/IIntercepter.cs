using Serilog.Events;

namespace Serilog.Sinks.Intercepter;

/// <summary>
/// Intercepts log events allowing for modifcation of log output.
/// </summary>
public interface IIntercepter
{
    /// <summary>
    /// Tests the <paramref name="logEvent"/> to determine if it should be rejected. 
    /// Preventing the <paramref name="logEvent"/> from being logged.
    /// </summary>
    /// <param name="logEvent">The item to be tested for rejection.</param>
    /// <returns>True to preventing any further processing of <paramref name="logEvent"/>;
    /// otherwise False to <see cref="IIntercepter.Intercept(LogEvent)"/> the <paramref name="logEvent"/>
    /// </returns>
    bool Reject(LogEvent logEvent);

    /// <summary>
    /// Intercepts the <paramref name="logEvent"/> to allow customised log output.
    /// Customistion could include reading, modifying, removing, adding, filtering, or buffering of LogEvents.
    /// </summary>
    /// <param name="logEvent">The item to be intercepted</param>
    /// <returns>LogEvents that will be sent to the proxyed Serilog sink</returns>
    IEnumerable<LogEvent> Intercept(LogEvent logEvent);
}
