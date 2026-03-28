using System.Threading.Channels;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using System.Net;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Endpoints;
using UnifiStoreWatcher.Web.Http;
using UnifiStoreWatcher.Web.Metrics;
using UnifiStoreWatcher.Web.Services;
using UnifiStoreWatcher.Web.Services.Health;
using UnifiStoreWatcher.Web.Services.Parsing;
using UnifiStoreWatcher.Web.Services.Polling;
using UnifiStoreWatcher.Web.Services.StateMachine;
using UnifiStoreWatcher.Web.Services.Notifications;
using UnifiStoreWatcher.Web.Hubs;

// Bootstrap logger (captures startup errors before full config loads)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

// Load .env and .env.{environment} files from the project root (or any ancestor directory).
// Env vars set in the OS environment take precedence over file values.
// In Docker, env vars come from Docker Compose env_file, so this is a no-op there.
DotNetEnv.Env.TraversePath().NoClobber().Load();
var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
DotNetEnv.Env.TraversePath().NoClobber().Load($".env.{aspnetEnv}");

try
{
    Log.Information("Starting UniFiStoreWatch");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning);

        // Forward logs to any configured OTLP collector.
        // UseSerilog() replaces the default ILogger pipeline, so the Serilog
        // OpenTelemetry sink is the bridge for exported logs.
        var otlpEndpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            var isHttp = context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"] is "http/protobuf";
            configuration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = isHttp
                    ? otlpEndpoint.TrimEnd('/') + "/v1/logs"
                    : otlpEndpoint;
                options.Protocol = isHttp
                    ? Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf
                    : Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;

                var rawHeaders = context.Configuration["OTEL_EXPORTER_OTLP_HEADERS"] ?? "";
                foreach (var pair in rawHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = pair.IndexOf('=');
                    if (idx > 0)
                        options.Headers[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
                }

                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = context.HostingEnvironment.ApplicationName,
                };
            });
        }

        if (context.HostingEnvironment.IsProduction())
        {
            configuration.WriteTo.File(
                new CompactJsonFormatter(),
                path: "/logs/stock-monitor-.clef",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 50 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 14);
        }
    });

    builder.Services.AddOpenApi();
    builder.Services.AddSignalR();
    builder.Services.AddProblemDetails();

    // Add DbContext
    builder.Services.AddDbContext<UnifiStoreWatcherDbContext>(options =>
    {
        var connStr = builder.Configuration.GetConnectionString("UniFiStoreWatch-db")
            ?? throw new InvalidOperationException(
                "Connection string 'UniFiStoreWatch-db' is not configured. " +
                "Set it via an env file or the ConnectionStrings__UniFiStoreWatch-db environment variable.");
        if (connStr.StartsWith("InMemory:", StringComparison.OrdinalIgnoreCase))
            options.UseInMemoryDatabase(connStr["InMemory:".Length..]);
        else
            options.UseSqlServer(connStr);
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<UnifiStoreWatcherDbContext>("db")
        .AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"])
        .AddCheck<PollerReadinessCheck>("poller", tags: ["ready"]);

    // Readiness indicator — toggled by PollSchedulerService after its first scan loop
    builder.Services.AddSingleton<IReadinessIndicator, ReadinessIndicator>();
    builder.Services.AddSingleton<DatabaseReadinessCheck>();
    builder.Services.AddSingleton<PollerReadinessCheck>();

    // Polling options
    builder.Services.Configure<PollOptions>(
        builder.Configuration.GetSection(PollOptions.SectionName));

    // Bounded channel for poll work items
    var pollChannel = Channel.CreateBounded<PollWorkItem>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false,
    });
    builder.Services.AddSingleton(pollChannel.Reader);
    builder.Services.AddSingleton(pollChannel.Writer);

    // HTTP client for polling

    // Singleton cookie jar — seeds required EU store cookies and persists server-set
    // cookies across handler-chain rotations and container restarts.
    builder.Services.AddSingleton<UbiquitiCookieJar>();

    // BrowserFingerprintHandler and UbiquitiCookieHandler are transient; they share
    // state via the singleton UbiquitiCookieJar (injected by DI).
    builder.Services.AddTransient<BrowserFingerprintHandler>();
    builder.Services.AddTransient<UbiquitiCookieHandler>();

    builder.Services.AddHttpClient("UniFiStoreWatchPoller", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    // Disable the primary handler's built-in cookie management; UbiquitiCookieHandler
    // manages the cookie jar manually so the shared singleton jar is always current.
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        UseCookies = false,
        AutomaticDecompression =
            System.Net.DecompressionMethods.GZip |
            System.Net.DecompressionMethods.Brotli |
            System.Net.DecompressionMethods.Deflate,
    })
    // Handler chain (outermost first):
    //   UbiquitiCookieHandler → BrowserFingerprintHandler → primary
    .AddHttpMessageHandler<UbiquitiCookieHandler>()
    .AddHttpMessageHandler<BrowserFingerprintHandler>()
    .AddStandardResilienceHandler(resilienceOptions =>
    {
        resilienceOptions.Retry.MaxRetryAttempts = 3;
        resilienceOptions.Retry.Delay = TimeSpan.FromSeconds(2);
        resilienceOptions.Retry.UseJitter = true;
        resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        resilienceOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    });

    // Background services
    builder.Services.AddHostedService<PollSchedulerService>();
    builder.Services.AddHostedService<PollWorkerService>();
    builder.Services.AddHostedService<ProductGaugeUpdater>();

    // Parsing services (order matters: JSON-LD → Button → Text)
    builder.Services.AddTransient<JsonLdStockParser>();
    builder.Services.AddTransient<ButtonStateParser>();
    builder.Services.AddTransient<TextContentParser>();
    builder.Services.AddTransient<IStockParser, JsonLdStockParser>(sp => sp.GetRequiredService<JsonLdStockParser>());
    builder.Services.AddTransient<IStockParser, ButtonStateParser>(sp => sp.GetRequiredService<ButtonStateParser>());
    builder.Services.AddTransient<IStockParser, TextContentParser>(sp => sp.GetRequiredService<TextContentParser>());
    builder.Services.AddTransient<CompositeStockParser>();
    builder.Services.AddTransient<ProductInfoExtractor>();
    builder.Services.AddTransient<ProductImageService>();
    builder.Services.AddTransient<StockStateMachine>();

    // Notification providers
    builder.Services.AddSingleton<BrowserPushProvider>();
    builder.Services.AddSingleton<INotificationProvider>(sp => sp.GetRequiredService<BrowserPushProvider>());
    builder.Services.AddSingleton<INotificationProvider, EmailProvider>();
    builder.Services.AddSingleton<INotificationProvider, SmsProvider>();
    builder.Services.AddSingleton<INotificationProvider, TeamsWebhookProvider>();
    builder.Services.AddSingleton<INotificationProvider, DiscordWebhookProvider>();
    builder.Services.AddScoped<NotificationDispatcher>();
    builder.Services.AddSingleton<StockHubBroadcaster>();

    var app = builder.Build();

    // Trust X-Forwarded-For and X-Forwarded-Proto from the Traefik proxy at 172.18.2.4
    // on the Docker 'proxy' network (172.18.0.0/16). ForwardLimit = 1 prevents header spoofing.
    var fwdOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1,
    };
    fwdOptions.KnownProxies.Add(IPAddress.Parse("172.18.2.4"));
    app.UseForwardedHeaders(fwdOptions);

    app.UseExceptionHandler();

    // Auto-migrate on startup; use EnsureCreated for InMemory (tests)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();
        if (db.Database.IsInMemory())
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();
    }

    app.UseHttpMetrics();

    app.UseStaticFiles();

    app.UseSerilogRequestLogging();

    // OpenAPI
    app.MapOpenApi();

    // VAPID key generation if not present
    using (var vapidScope = app.Services.CreateScope())
    {
        var db = vapidScope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();
        var settings = await db.AppSettings.FindAsync(1);
        if (settings != null && string.IsNullOrEmpty(settings.VapidPublicKey))
        {
            var (publicKey, privateKey) = Program.GenerateVapidKeys();
            settings.VapidPublicKey = publicKey;
            settings.VapidPrivateKey = privateKey;
            await db.SaveChangesAsync();
        }
    }

    // API endpoint groups
    app.MapProductEndpoints();
    app.MapSettingsEndpoints();
    app.MapNotificationEndpoints();
    app.MapPushEndpoints();

    app.MapHub<UnifiStoreWatcherHub>("/UnifiStoreWatcher-hub");

    // Health endpoints
    // /api/health/live — liveness: always 200 if the process is running (no checks executed)
    app.MapHealthChecks("/api/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy]   = StatusCodes.Status200OK,
            [HealthStatus.Degraded]  = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status200OK,
        },
    });
    // /api/health/ready — readiness: 200 when DB + poller are ready, 503 otherwise
    app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
    {
        Predicate = c => c.Tags.Contains("ready"),
    });
    // /api/health — backward-compatible aggregate endpoint (all checks)
    app.MapHealthChecks("/api/health");

    app.MapMetrics("/api/metrics");

    // SPA fallback — serves the built Vue app for any unmatched non-file routes
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory in tests
public partial class Program
{
    internal static (string publicKey, string privateKey) GenerateVapidKeys()
    {
        using var ecdh = System.Security.Cryptography.ECDiffieHellman.Create(
            System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var parameters = ecdh.ExportParameters(includePrivateParameters: true);

        // Uncompressed EC point format: 0x04 || X (32 bytes) || Y (32 bytes) = 65 bytes
        var publicKeyBytes = new byte[65];
        publicKeyBytes[0] = 0x04;
        parameters.Q.X!.CopyTo(publicKeyBytes, 1);
        parameters.Q.Y!.CopyTo(publicKeyBytes, 33);

        var publicKey = Convert.ToBase64String(publicKeyBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var privateKey = Convert.ToBase64String(parameters.D!).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (publicKey, privateKey);
    }
}
