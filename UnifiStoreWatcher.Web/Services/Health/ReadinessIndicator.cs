namespace UniFiStoreWatcher.Web.Services.Health;

/// <summary>
/// Thread-safe readiness indicator backed by a volatile bool.
/// Once <see cref="MarkReady"/> is called it stays ready for the lifetime of the process.
/// </summary>
public sealed class ReadinessIndicator : IReadinessIndicator
{
    private volatile bool _ready;

    public bool IsReady => _ready;

    public void MarkReady() => _ready = true;
}
