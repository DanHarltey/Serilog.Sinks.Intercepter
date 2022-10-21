namespace Serilog.Sinks.Intercepter.Internal;

internal sealed class ContextBookmark : IDisposable
{
    private readonly IntercepterContext _context;
    private readonly IIntercepter[] _moderators;

    public ContextBookmark(IntercepterContext context, IIntercepter[] moderators)
    {
        _context = context;
        _moderators = moderators;
    }

    public void Dispose() => _context.Intercepters = _moderators;
}
