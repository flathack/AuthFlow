namespace AutoLogin.App.Models;

public sealed class AutomationStep
{
    public string Action { get; set; } = string.Empty;

    public string? Selector { get; set; }

    public string? Value { get; set; }

    public int? DelayMs { get; set; }

    public string? UrlPattern { get; set; }

    public int? TimeoutMs { get; set; }
}
