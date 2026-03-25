namespace UbiquitiStoreLurker.Web.Services.Polling;

public sealed class PollOptions
{
    public const string SectionName = "Polling";

    public int MinIntervalSeconds { get; set; } = 30;
    public int MaxIntervalSeconds { get; set; } = 90;
    public int SchedulerScanIntervalSeconds { get; set; } = 10;
    public int ChannelCapacity { get; set; } = 100;

    /// <summary>
    /// Full User-Agent string sent with every poll request.
    /// If blank or prefixed with "UbiquitiStoreLurker/", the handler falls back to the
    /// Chrome 124 default so the poller is never identifiable as a bot.
    /// </summary>
    public string BrowserUserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    /// <summary>
    /// Minimum milliseconds between successive HTTP requests to the store.
    /// A random ±<see cref="JitterPercent"/>% is applied per request.
    /// Default: 12 000 ms (12 s).
    /// </summary>
    public int MinRequestGapMs { get; set; } = 12_000;

    /// <summary>
    /// Jitter percentage applied to <see cref="MinRequestGapMs"/>.
    /// E.g., 30 means actual gap is random in [MinRequestGapMs×0.7, MinRequestGapMs×1.3].
    /// </summary>
    public int JitterPercent { get; set; } = 30;
}
