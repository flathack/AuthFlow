using System.Security.Cryptography;
using System.Text;

namespace AutoLogin.App.Services.Security;

public sealed class DpapiCredentialProtector : ICredentialProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AutoLogin-App/v1");

    public string Protect(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedValue);

        var encryptedBytes = Convert.FromBase64String(protectedValue);
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
