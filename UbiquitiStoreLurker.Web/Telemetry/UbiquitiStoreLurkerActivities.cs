using System.Diagnostics;

namespace UbiquitiStoreLurker.Web.Telemetry;

public static class UbiquitiStoreLurkerActivities
{
    public static readonly ActivitySource Source = new("UbiquitiStoreLurker.Web", "1.0.0");
}
