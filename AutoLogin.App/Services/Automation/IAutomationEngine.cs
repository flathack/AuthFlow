using AutoLogin.App.Models;
using AutoLogin.App.Services.Browser;

namespace AutoLogin.App.Services.Automation;

public interface IAutomationEngine
{
    Task<AutomationExecutionResult> ExecuteAsync(
        IBrowserSession browserSession,
        LoginEntry entry,
        AutomationProfile profile,
        CancellationToken cancellationToken = default);
}
