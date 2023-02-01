namespace Serilog.Sinks.Intercepter.Internal;

internal sealed class ContextBookmark : IDisposable
{
    private readonly IntercepterContext _context;
    private readonly IIntercepter? _intercepter;

    public ContextBookmark(IntercepterContext context, IIntercepter? intercepter)
    {
        _context = context;
        _intercepter = intercepter;
    }

    public void Dispose() => _context.Intercepter = _intercepter;
}
