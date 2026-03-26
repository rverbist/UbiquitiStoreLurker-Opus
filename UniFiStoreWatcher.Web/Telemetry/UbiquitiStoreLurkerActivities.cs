using System.Diagnostics;

namespace UniFiStoreWatcher.Web.Telemetry;

public static class UniFiStoreWatcherActivities
{
    public static readonly ActivitySource Source = new("UniFiStoreWatcher.Web", "1.0.0");
}
