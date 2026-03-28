using Prometheus;

namespace UnifiStoreWatcher.Web.Metrics;

public static class UnifiStoreWatcherMetrics
{
    // Counters
    public static readonly Counter StockChecksTotal = Prometheus.Metrics.CreateCounter(
        "UnifiStoreWatcher_checks_total",
        "Total number of stock check HTTP requests",
        ["result"] // success, error
    );

    public static readonly Counter StockTransitionsTotal = Prometheus.Metrics.CreateCounter(
        "UnifiStoreWatcher_transitions_total",
        "Total number of stock state transitions",
        ["from_state", "to_state"]
    );

    public static readonly Counter NotificationsSentTotal = Prometheus.Metrics.CreateCounter(
        "UnifiStoreWatcher_notifications_sent_total",
        "Total number of notifications sent",
        ["provider", "success"]
    );

    // Gauges
    public static readonly Gauge ActiveProducts = Prometheus.Metrics.CreateGauge(
        "UnifiStoreWatcher_active_products",
        "Number of currently active (polling) products"
    );

    public static readonly Gauge MonitoredProductsTotal = Prometheus.Metrics.CreateGauge(
        "UnifiStoreWatcher_monitored_products_total",
        "Total number of products (active + paused)"
    );

    // Histograms
    public static readonly Histogram PollDurationSeconds = Prometheus.Metrics.CreateHistogram(
        "UnifiStoreWatcher_poll_duration_seconds",
        "Duration of individual product poll HTTP requests in seconds",
        new HistogramConfiguration
        {
            Buckets = [0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0],
        }
    );

    public static readonly Histogram NotificationLatencySeconds = Prometheus.Metrics.CreateHistogram(
        "UnifiStoreWatcher_notification_latency_seconds",
        "Duration of notification dispatch in seconds",
        new HistogramConfiguration
        {
            Buckets = [0.05, 0.1, 0.25, 0.5, 1.0, 5.0],
        }
    );

}
