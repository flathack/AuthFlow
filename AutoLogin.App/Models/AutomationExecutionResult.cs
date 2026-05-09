namespace AutoLogin.App.Models;

public sealed class AutomationExecutionResult
{
    public bool IsSuccess { get; set; }

    public int? FailedStepIndex { get; set; }

    public string Message { get; set; } = string.Empty;

    public TimeSpan Elapsed { get; set; }
}
