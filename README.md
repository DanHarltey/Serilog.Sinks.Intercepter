# Serilog.Sinks.Intercepter
A wrapper for other [Serilog](https://serilog.net) sinks. This sink allows you to "intercept" log events just as they are wrote to a wrapped sink. This is used to modifty, filter, buffer event logs. This is especially suited to reducing log volunms, for example only writting logs when an error has cccoured. 

### Getting started

Install from [NuGet](https://nuget.org/packages/serilog.sinks.async):

```powershell
Install-Package Serilog.Sinks.Async
```

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

Whilst no Intercepter has been set the all log messages will be sent onwards to the wrapped sink (`Console` in this case).

### LogLevelBuffer Intercepter
This Intercepter is designed to reduce log volunms by storing log events and only writting when an error level log event is recived.

```csharp
// add the Intercepter
using (IntercepterContext.PushLogLevelBuffer())
{
	Log.Information("This log is stored by the intercepter.");
	Log.Error("On this error, this and all prevouse log events are sent to the wrapped sink");
	Log.Information("As there has already been an error, this is sent to");
}
```

Can be used per appilation request to only write the logs of requests where there was an error. 

## ASP.NET Core integration

See [samples/Sample.WebApp/](https://github.com/DanHarltey/Serilog.Sinks.Intercepter/tree/main/samples) for the full sample of using this with ASP.NET Core.

The configuration used in the sample is similar to the first we saw:
```csharp
builder.Host.UseSerilog((ctx, lc) =>
    lc.WriteTo.Intercepter(x => x.Console()));
```

The sample shows how to add `IntercepterContext.PushLogLevelBuffer` to the ASP.NET Core pipeline as middleware:

```csharp
public async Task Invoke(HttpContext httpContext)
{
	using (IntercepterContext.PushLogLevelBuffer())
	{
		await _next(httpContext);
	}
}
```

### JSON configuration

Using [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) JSON:

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

### Log event ordering

In the event of any buffering, the log events are sent to the output are in the orginal order and with the orginal timestamps.
