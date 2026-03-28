using System.Diagnostics;

namespace UnifiStoreWatcher.Web.Telemetry;

public static class UnifiStoreWatcherActivities
{
    public static readonly ActivitySource Source = new("UnifiStoreWatcher.Web", "1.0.0");
}
