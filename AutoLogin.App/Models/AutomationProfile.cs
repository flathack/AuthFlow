namespace AutoLogin.App.Models;

public sealed class AutomationProfile
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TargetUrlPattern { get; set; } = "*";

    public List<AutomationStep> Steps { get; set; } = [];

    public int TimeoutMs { get; set; } = 15000;

    public bool WaitForNavigation { get; set; } = true;
}
