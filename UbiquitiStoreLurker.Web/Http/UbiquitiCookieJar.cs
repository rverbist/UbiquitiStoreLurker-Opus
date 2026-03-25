using System.Diagnostics;
using System.Net;
using System.Text.Json;
using UbiquitiStoreLurker.Web.Telemetry;

namespace UbiquitiStoreLurker.Web.Http;

// LoggerMessage source generation requires a partial class.
// ReSharper disable once PartialTypeWithSinglePart
/// <summary>
/// Singleton service that manages a shared cookie jar for Ubiquiti EU store requests.
///
/// Responsibilities:
/// - Seeds the three cookies required for the store to serve product pages directly
///   (curr_language, curr_store, store_modal_shown).
/// - Captures any Set-Cookie headers from responses so the server's session cookies are
///   replayed on subsequent requests.
/// - Persists the live cookie jar to a JSON file alongside the database on each change,
///   and reloads it on startup to survive container restarts.
/// - Invalidates a persisted jar older than 24 hours (cookies are short-lived).
/// </summary>
public sealed partial class UbiquitiCookieJar : IDisposable
{
    private static readonly Uri StoreBaseUri = new("https://eu.store.ui.com/");

    // Cookies required before the store will serve product content without redirecting
    // to a geo-selector / region modal.
    private static readonly (string Name, string Value)[] SeedCookies =
    [
        ("curr_language", "en"),
        ("curr_store", "eu"),
        ("store_modal_shown", "true"),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly CookieContainer _container = new();
    private readonly string? _persistPath;
    private readonly ILogger<UbiquitiCookieJar> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public UbiquitiCookieJar(IConfiguration configuration, ILogger<UbiquitiCookieJar> logger)
    {
        _logger = logger;

        // Derive persist path: same directory as the SQLite database file.
        var connStr = configuration.GetConnectionString("Default");
        if (!string.IsNullOrEmpty(connStr))
        {
            // "Data Source=/data/ubiquitistorelurker.db" → /data/http-cookies.json
            var match = System.Text.RegularExpressions.Regex.Match(
                connStr, @"Data\s+Source=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var dbPath = match.Groups[1].Value.Trim();
                var dir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dir))
                    _persistPath = Path.Combine(dir, "http-cookies.json");
            }
        }

        // Seed known-good store cookies immediately.
        SeedRequired();

        // Try to restore from persisted jar (may overlay additional server-set cookies).
        TryRestoreFromDisk();
    }

    /// <summary>Returns the Cookie header value for a given URI from the shared container.</summary>
    public string GetCookieHeader(Uri uri) => _container.GetCookieHeader(uri);

    /// <summary>
    /// Processes Set-Cookie headers from an HTTP response and updates the shared container.
    /// Call this after every response from eu.store.ui.com.
    /// </summary>
    public void UpdateFromResponse(Uri requestUri, HttpResponseMessage response)
    {
        bool changed = false;

        // HttpResponseHeaders stores Set-Cookie as a collection of header values.
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
        {
            foreach (var value in setCookieValues)
            {
                try
                {
                    _container.SetCookies(requestUri, value);
                    changed = true;
                }
                catch (CookieException ex)
                {
                    LogIgnoredSetCookie(_logger, requestUri, ex.Message);
                }
            }
        }

        if (changed)
            _ = PersistToDiskAsync();
    }

    private void SeedRequired()
    {
        using var activity = UbiquitiStoreLurkerActivities.Source.StartActivity("cookie.refresh", ActivityKind.Internal);
        activity?.SetTag("cookie.action", "seed");

        foreach (var (name, value) in SeedCookies)
        {
            _container.Add(StoreBaseUri, new Cookie(name, value)
            {
                Secure = true,
            });
        }
    }

    private void TryRestoreFromDisk()
    {
        if (_persistPath is null || !File.Exists(_persistPath))
            return;

        using var activity = UbiquitiStoreLurkerActivities.Source.StartActivity("cookie.refresh", ActivityKind.Internal);
        activity?.SetTag("cookie.action", "reload");

        try
        {
            var json = File.ReadAllText(_persistPath);
            var doc = JsonSerializer.Deserialize<PersistedCookieJar>(json);
            if (doc is null) return;

            // Discard if older than 24 hours.
            if (DateTimeOffset.UtcNow - doc.SavedAtUtc > TimeSpan.FromHours(24))
            {
                LogStaleJarDiscarded(_logger);
                return;
            }

            int restored = 0;
            foreach (var c in doc.Cookies)
            {
                try
                {
                    var expires = c.Expires == DateTimeOffset.MinValue
                        ? DateTime.MinValue
                        : c.Expires.UtcDateTime;
                    _container.Add(new Cookie(c.Name, c.Value, c.Path, c.Domain)
                    {
                        Secure = c.Secure,
                        HttpOnly = c.HttpOnly,
                        Expires = expires,
                    });
                    restored++;
                }
                catch { /* ignore individual bad entries */ }
            }

            LogRestoredCookies(_logger, restored, _persistPath ?? string.Empty);
        }
        catch (Exception ex)
        {
            LogRestoreFailed(_logger, ex, _persistPath ?? string.Empty);
        }
    }

    private async Task PersistToDiskAsync()
    {
        if (_persistPath is null) return;

        using var activity = UbiquitiStoreLurkerActivities.Source.StartActivity("cookie.refresh", ActivityKind.Internal);
        activity?.SetTag("cookie.action", "persist");

        await _lock.WaitAsync();
        try
        {
            var allCookies = _container.GetAllCookies();
            var entries = allCookies
                .Cast<Cookie>()
                .Select(c => new PersistedCookie
                {
                    Name = c.Name,
                    Value = c.Value,
                    Domain = c.Domain,
                    Path = c.Path,
                    Secure = c.Secure,
                    HttpOnly = c.HttpOnly,
                    Expires = c.Expired ? DateTimeOffset.MinValue : new DateTimeOffset(c.Expires, TimeSpan.Zero),
                })
                .ToList();

            var jar = new PersistedCookieJar { SavedAtUtc = DateTimeOffset.UtcNow, Cookies = entries };
            var json = JsonSerializer.Serialize(jar, JsonOptions);

            var dir = Path.GetDirectoryName(_persistPath);
            if (dir is not null) Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(_persistPath, json);
        }
        catch (Exception ex)
        {
            LogPersistFailed(_logger, ex, _persistPath ?? string.Empty);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    // ─── Persistence model ───────────────────────────────────────────────────

    private sealed record PersistedCookieJar
    {
        public DateTimeOffset SavedAtUtc { get; init; }
        public List<PersistedCookie> Cookies { get; init; } = [];
    }

    private sealed record PersistedCookie
    {
        public string Name { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Domain { get; init; } = string.Empty;
        public string Path { get; init; } = "/";
        public bool Secure { get; init; }
        public bool HttpOnly { get; init; }
        public DateTimeOffset Expires { get; init; }
    }

    // ─── LoggerMessage source-generated helpers ───────────────────────────────

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Ignoring unparseable Set-Cookie from {Uri}: {Error}")]
    private static partial void LogIgnoredSetCookie(ILogger logger, Uri uri, string error);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Discarding stale cookie jar (age > 24 h), re-seeding.")]
    private static partial void LogStaleJarDiscarded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Restored {Count} cookies from disk ({Path}).")]
    private static partial void LogRestoredCookies(ILogger logger, int count, string path);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to restore cookie jar from {Path}; starting fresh.")]
    private static partial void LogRestoreFailed(ILogger logger, Exception ex, string path);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Failed to persist cookie jar to {Path}.")]
    private static partial void LogPersistFailed(ILogger logger, Exception ex, string path);
}
