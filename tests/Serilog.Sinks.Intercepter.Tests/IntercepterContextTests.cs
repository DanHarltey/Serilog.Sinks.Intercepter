using Serilog.Events;
using Serilog.Sinks.Intercepter.Intercepters;
using Serilog.Sinks.Intercepter.Tests.Mocks;

namespace Serilog.Sinks.Intercepter.Tests;

public sealed class IntercepterContextTests
{

    [Fact]
    public void DefaultContextIsNotNull()
    {
        // Arrange

        // Act
        var actual = IntercepterContext.Default;

        // Assert
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task DefaultContextReturnsSameInstance()
    {
        // Arrange
        var intercepter = new TestIntercepter(true, x => throw new NotImplementedException());
        IntercepterContext? instance1 = null;
        IntercepterContext? instance2 = null;
        IntercepterContext? instance3 = null;
        IntercepterContext? instance4 = null;

        // Act
        instance1 = IntercepterContext.Default;

        using (IntercepterContext.Push(intercepter))
        {
            instance2 = IntercepterContext.Default;
        }
        await Task.Run(() => instance3 = IntercepterContext.Default);

        var thread = new Thread(() => instance4 = IntercepterContext.Default);
        thread.Start();
        thread.Join();

        // Assert
        Assert.Same(instance1, instance2);
        Assert.Same(instance2, instance3);
        Assert.Same(instance3, instance4);
    }

    [Fact]
    public void IntercepterDefaultIsNulll()
    {
        // Arrange
        var context = new IntercepterContext();

        // Act
        var actual = context.Intercepter;

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void PushSetsTheIntercepterProperty()
    {
        // Arrange
        var expected = new TestIntercepter(true, x => throw new NotImplementedException());
        IIntercepter? actual;
        var context = new IntercepterContext();

        // Act
        using (IntercepterContext.Push(context, expected))
        {
            actual = context.Intercepter;
        }

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public void PushThrowsArgumentNullException()
    {
        // Arrange
        IntercepterContext? context = null;
        var intercepter = new TestIntercepter(true, x => throw new NotImplementedException());

        // Act, Assert
        Assert.Throws<ArgumentNullException>(() => IntercepterContext.Push(context!, intercepter));
    }

    [Fact]
    public void PushInterceptersDisposesCorrectly()
    {
        // Arrange
        var expected1 = new TestIntercepter(true, x => throw new NotImplementedException());
        var expected2 = new TestIntercepter(true, x => throw new NotImplementedException());

        IIntercepter? actual1;
        IIntercepter? actual2;
        IIntercepter? actual3;

        var context = new IntercepterContext();

        // Act
        using (IntercepterContext.Push(context, expected2))
        {
            using (IntercepterContext.Push(context, expected1))
            {
                actual1 = context.Intercepter;
            }
            actual2 = context.Intercepter;
        }
        actual3 = context.Intercepter;

        // Assert
        Assert.Same(expected1, actual1);
        Assert.Same(expected2, actual2);
        Assert.Null(actual3);
    }

    [Fact]
    public async Task IntercepterFollowsAsyncContext()
    {
        // Arrange
        var expected1 = new TestIntercepter(true, x => throw new NotImplementedException());
        var expected2 = new TestIntercepter(true, x => throw new NotImplementedException());

        IIntercepter? actual1 = null;
        IIntercepter? actual2 = null;
        IIntercepter? actual3 = null;

        var context = new IntercepterContext();

        // Act
        using (IntercepterContext.Push(context, expected1))
        {
            await Task.Run(() => actual1 = context.Intercepter);

            await Task.Run(async () =>
            {
                await Task.Delay(100).ConfigureAwait(false);

                await Task.Run(async () =>
                {
                    using (IntercepterContext.Push(context, expected2))
                    {
                        await Task.Delay(100);
                        actual2 = context.Intercepter;
                    }
                });
                actual3 = context.Intercepter;
            });
        }

        // Assert
        Assert.Same(expected1, actual1);
        Assert.Same(expected2, actual2);
        Assert.Same(expected1, actual3);
    }

    [Fact]
    public void IntercepterDoesFollowThreadContext()
    {
        // Arrange
        var expected = new TestIntercepter(true, x => throw new NotImplementedException());
        var context = new IntercepterContext();
        IIntercepter? actual = null;

        // Act
        using (IntercepterContext.Push(context, expected))
        {
            var thread = new Thread(() => actual = context.Intercepter);
            thread.Start();
            thread.Join();
        }

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public void InterceptersAreOrderedByFIFO()
    {
        // Arrange
        var intercepter = new TestIntercepter(true, x => throw new NotImplementedException());
        var expected = new TestIntercepter(true, x => throw new NotImplementedException());

        IIntercepter? actual;

        var context = new IntercepterContext();

        // Act
        using (IntercepterContext.Push(context, intercepter))
        using (IntercepterContext.Push(context, expected))
        {
            actual = context.Intercepter;
        }

        Assert.Same(expected, actual);
    }

    [Fact]
    public void PushLogLevelBuffer()
    {
        // Arrange
        IIntercepter? actualIntercepter;

        // Act
        using (IntercepterContext.PushLogLevelBuffer())
        {
            actualIntercepter = IntercepterContext.Default.Intercepter;
        }

        var actual = Assert.IsType<LogLevelBufferIntercepter>(actualIntercepter);
        Assert.Equal(LogEventLevel.Error, actual.TriggerLevel);
    }

    [Theory]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Information)]
    public void PushLogLevelBufferWithLevel(LogEventLevel logEventLevel)
    {
        // Arrange
        IIntercepter? actualIntercepter;


        // Act
        using (IntercepterContext.PushLogLevelBuffer(logEventLevel))
        {
            actualIntercepter = IntercepterContext.Default.Intercepter;
        }

        var actual = Assert.IsType<LogLevelBufferIntercepter>(actualIntercepter);
        Assert.Equal(logEventLevel, actual.TriggerLevel);
    }

    [Theory]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Information)]
    public void PushLogLevelBufferWithContextAndLevel(LogEventLevel logEventLevel)
    {
        // Arrange
        IIntercepter? actualIntercepter;

        var context = new IntercepterContext();

        // Act
        using (IntercepterContext.PushLogLevelBuffer(context, logEventLevel))
        {
            actualIntercepter = context.Intercepter;
        }

        var actual = Assert.IsType<LogLevelBufferIntercepter>(actualIntercepter);
        Assert.Equal(logEventLevel, actual.TriggerLevel);
    }
}