namespace AutoLogin.App.Services.Security;

public interface ICredentialProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedValue);
}
