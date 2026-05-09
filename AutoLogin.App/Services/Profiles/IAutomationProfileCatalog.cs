using AutoLogin.App.Models;

namespace AutoLogin.App.Services.Profiles;

public interface IAutomationProfileCatalog
{
    Task<IReadOnlyList<AutomationProfile>> GetProfilesAsync();
}
