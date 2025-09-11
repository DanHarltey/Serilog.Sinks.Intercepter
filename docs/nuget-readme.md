Modify, filter, buffer event logs. Buffer log messages and conditionally output them based on later events.

A wrapper for other [Serilog](https://serilog.net) sinks. This sink allows you to "intercept" log events just as they are written to a wrapped sink. This is used to modify, filter, buffer event logs. This is especially suited to reducing log volume, for example only writing logs when an error has occurred.

## Getting started

Assuming you have already installed the target sink, such as the console sink, move the wrapped sink's configuration within a `WriteTo.Intercepter()` statement:

```csharp
Log.Logger = new LoggerConfiguration()
  .WriteTo.Intercepter(x => x.Console())
  // Other logger configuration
  .CreateLogger()

Log.Information("Continue to use the log as normal");

// At application shutdown
Log.CloseAndFlush();
```

or if using [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) JSON:

```json
{
  "Serilog": {
    "WriteTo": [{
    "Name": "Intercepter",
    "Args": {
      "configure": [{
        "Name": "Console"
        }]
      }
    }]
  }
}
```

Whilst no Intercepter has been set, all log messages will be sent onwards to the wrapped sink (`Console` in this case).

## LogLevelBuffer Intercepter

This Intercepter is designed to reduce log volume by storing log events and only writing when an error level log event is received.

```csharp
// add the Intercepter
using (IntercepterContext.PushLogLevelBuffer(Serilog.Events.LogEventLevel.Error))
{
    Log.Information("This log is stored by the intercepter.");
    Log.Error("On this error, this and all previous log events are sent to the wrapped sink");
    Log.Information("As there has already been an error, this is sent to");
}
```

Can be used per application request to only write the logs of requests where there was an error.

### Log event ordering

In the event of any buffering, the log events sent to the output are in the original order and with the original timestamps.

### ASP.NET Core integration
Please see [here](https://github.com/DanHarltey/Serilog.Sinks.Intercepter?tab=readme-ov-file#aspnet-core-integration) for details.