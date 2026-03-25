namespace UbiquitiStoreLurker.Web.Services.Health;

/// <summary>
/// Tracks whether the application is ready to serve traffic.
/// Implementations are registered as singletons and toggled by background services
/// once they have completed their initialisation phase.
/// </summary>
public interface IReadinessIndicator
{
    bool IsReady { get; }
    void MarkReady();
}
