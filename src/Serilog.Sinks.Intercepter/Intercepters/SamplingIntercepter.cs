using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Intercepters;

public class SamplingIntercepter : IIntercepter
{
    private readonly bool _intercept;

    public SamplingIntercepter(int samplePercentage)
    {
        if (samplePercentage < 0 || samplePercentage > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(samplePercentage), "Must be between 0 and 100");
        }
        var random = Random.Shared.Next(100);
        _intercept = random < samplePercentage;
    }

    public bool CanHandle(LogEvent logEvent) => _intercept;

    public IEnumerable<LogEvent> Process(LogEvent logEvent) => new[] { logEvent };
}
