using Prometheus;

namespace UniFiStoreWatcher.Web.Metrics;

public static class UniFiStoreWatcherMetrics
{
    // Counters
    public static readonly Counter StockChecksTotal = Prometheus.Metrics.CreateCounter(
        "UniFiStoreWatcher_checks_total",
        "Total number of stock check HTTP requests",
        ["result"] // success, error
    );

    public static readonly Counter StockTransitionsTotal = Prometheus.Metrics.CreateCounter(
        "UniFiStoreWatcher_transitions_total",
        "Total number of stock state transitions",
        ["from_state", "to_state"]
    );

    public static readonly Counter NotificationsSentTotal = Prometheus.Metrics.CreateCounter(
        "UniFiStoreWatcher_notifications_sent_total",
        "Total number of notifications sent",
        ["provider", "success"]
    );

    // Gauges
    public static readonly Gauge ActiveProducts = Prometheus.Metrics.CreateGauge(
        "UniFiStoreWatcher_active_products",
        "Number of currently active (polling) products"
    );

    public static readonly Gauge MonitoredProductsTotal = Prometheus.Metrics.CreateGauge(
        "UniFiStoreWatcher_monitored_products_total",
        "Total number of products (active + paused)"
    );

    // Histograms
    public static readonly Histogram PollDurationSeconds = Prometheus.Metrics.CreateHistogram(
        "UniFiStoreWatcher_poll_duration_seconds",
        "Duration of individual product poll HTTP requests in seconds",
        new HistogramConfiguration
        {
            Buckets = [0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0],
        }
    );

    public static readonly Histogram NotificationLatencySeconds = Prometheus.Metrics.CreateHistogram(
        "UniFiStoreWatcher_notification_latency_seconds",
        "Duration of notification dispatch in seconds",
        new HistogramConfiguration
        {
            Buckets = [0.05, 0.1, 0.25, 0.5, 1.0, 5.0],
        }
    );

    // Rate-limit observability ───────────────────────────────────────────────

    /// <summary>Incremented each time the Ubiquiti store returns HTTP 429.</summary>
    public static readonly Counter RateLimitServer429Total = Prometheus.Metrics.CreateCounter(
        "UniFiStoreWatcher_rate_limit_server_429_total",
        "Total number of HTTP 429 responses received from the target store"
    );

    /// <summary>Last observed jittered inter-request gap in milliseconds.</summary>
    public static readonly Gauge PollRequestGapMs = Prometheus.Metrics.CreateGauge(
        "UniFiStoreWatcher_poll_request_gap_ms",
        "Last jittered inter-request gap applied by the client-side rate limiter (ms)"
    );
}
