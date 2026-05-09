using System.IO;
using System.Text.Json;
using AutoLogin.App.Models;

namespace AutoLogin.App.Services.Profiles;

public sealed class JsonAutomationProfileCatalog : IAutomationProfileCatalog
{
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonAutomationProfileCatalog(string profilesDirectory)
    {
        _profilesDirectory = profilesDirectory;
    }

    public async Task<IReadOnlyList<AutomationProfile>> GetProfilesAsync()
    {
        if (!Directory.Exists(_profilesDirectory))
        {
            return [];
        }

        var files = Directory.GetFiles(_profilesDirectory, "*.json", SearchOption.TopDirectoryOnly);
        var profiles = new List<AutomationProfile>();

        foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            await using var stream = File.OpenRead(file);
            var profile = await JsonSerializer.DeserializeAsync<AutomationProfile>(stream, _serializerOptions);
            if (profile is null || string.IsNullOrWhiteSpace(profile.Id))
            {
                continue;
            }

            profiles.Add(profile);
        }

        return profiles;
    }
}
