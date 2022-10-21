using Serilog.Configuration;
using Serilog.Events;

namespace Serilog.Sinks.Intercepter;

public static class LoggerConfigurationIntercepterExtensions
{
    public static LoggerConfiguration Intercept(
       this LoggerSinkConfiguration loggerSinkConfiguration,
       Action<LoggerSinkConfiguration> configure) => 
        Intercept(loggerSinkConfiguration, configure, IntercepterContext.Default);

    public static LoggerConfiguration Intercept(
      this LoggerSinkConfiguration loggerSinkConfiguration,
      Action<LoggerSinkConfiguration> configure,
      IntercepterContext context) =>
        LoggerSinkConfiguration.Wrap(
            loggerSinkConfiguration,
            wrappedSink => new IntercepterSink(context, wrappedSink),
            configure,
            LevelAlias.Minimum,
            null);
}