var builder = DistributedApplication.CreateBuilder(args);

// Secrets — stored in dotnet user-secrets, never in files
// First-run: dotnet user-secrets set "Parameters:smtp-password" "value" --project src/UbiquitiStoreLurker.AppHost
var smtpPassword   = builder.AddParameter("smtp-password",   secret: true);
var twilioToken    = builder.AddParameter("twilio-token",    secret: true);
var discordWebhook = builder.AddParameter("discord-webhook", secret: true);
var teamsWebhook   = builder.AddParameter("teams-webhook",   secret: true);

// SQLite — injects ConnectionStrings__ubiquitistorelurker-db automatically + SQLiteWeb UI
var db = builder.AddSqlite("ubiquitistorelurker-db")
                .WithSqliteWeb();

// App — startup ordering + health probe
// .WaitFor(db)                            → Aspire holds the web container until the SQLite resource reports healthy,
//                                           preventing EF Core migration races on first run.
// .WithHttpHealthCheck("/api/health/live") → liveness: process alive (always 200)
// .WithHttpHealthCheck("/api/health/ready",
//   HttpScheme.Http, ProbeType.Readiness)  → readiness: DB + poller initialised (200 / 503)
builder.AddProject<Projects.UbiquitiStoreLurker_Web>("ubiquitistorelurker")
       .WithReference(db)
       .WithHttpHealthCheck("/api/health/live")
       .WithHttpHealthCheck("/api/health/ready")
       .WaitFor(db)
       .WithEnvironment("Email__Password",     smtpPassword)
       .WithEnvironment("Twilio__AuthToken",   twilioToken)
       .WithEnvironment("Discord__WebhookUrl", discordWebhook)
       .WithEnvironment("Teams__WebhookUrl",   teamsWebhook);

// Local Prometheus + Grafana — Aspire mode only, DO NOT add to docker-compose.yml
// Proxmox production uses the existing lab Prometheus (172.18.1.1) and Grafana (172.18.1.2)
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "latest")
                        .WithBindMount("./prometheus", "/etc/prometheus")
                        .WithHttpEndpoint(targetPort: 9090, name: "http");

builder.AddContainer("grafana", "grafana/grafana", "latest")
       .WithBindMount("./grafana-provisioning", "/etc/grafana/provisioning")
       .WithHttpEndpoint(targetPort: 3000, name: "http")
       .WaitFor(prometheus);

builder.Build().Run();
