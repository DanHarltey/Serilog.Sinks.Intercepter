using Microsoft.Extensions.Configuration;
using Serilog.Sinks.Intercepter.Tests.Mocks;

namespace Serilog.Sinks.Intercepter.Tests;

public class JsonConfigTests
{
    [Fact]
    public void CanConfigureIntercepterWithJson()
    {
        // Arrange
        var testSink = new TestSink();
        TestSinkExtensions.Instance = testSink;

        // Act
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("SerilogConfig.json")
            .Build();

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration);

        var logger = loggerConfiguration.CreateLogger();

        // Assert
        logger.Information("Hello, world!");
        Assert.Single(testSink.LogEvents);

        using (IntercepterContext.PushLogLevelBuffer())
        {
            logger.Information("Hello, world!");
        }

        Assert.Single(testSink.LogEvents);

        using (IntercepterContext.PushLogLevelBuffer())
        {
            logger.Information("Hello, world!");
            logger.Error("ERROR!");
        }

        Assert.Equal(3, testSink.LogEvents.Count());
    }

    [Fact]
    public void WrappedSinkIsDisposed()
    {
        // Arrange
        var testSink = new TestSink();
        TestSinkExtensions.Instance = testSink;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("SerilogConfig.json")
            .Build();

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration);

        // Act
        var logger = loggerConfiguration.CreateLogger();
        logger.Dispose();

        // Assert
        Assert.True(testSink.IsDisposed);
    }
}
