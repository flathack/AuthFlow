namespace AutoLogin.App.Models;

public sealed class LoginEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public string StartUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string EncryptedPassword { get; set; } = string.Empty;

    public string? EncryptedTotpSecret { get; set; }

    public string AutomationProfileId { get; set; } = string.Empty;

    public bool AutoSubmit { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public LoginEntry Clone()
    {
        return new LoginEntry
        {
            Id = Id,
            DisplayName = DisplayName,
            StartUrl = StartUrl,
            Username = Username,
            EncryptedPassword = EncryptedPassword,
            EncryptedTotpSecret = EncryptedTotpSecret,
            AutomationProfileId = AutomationProfileId,
            AutoSubmit = AutoSubmit,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}
