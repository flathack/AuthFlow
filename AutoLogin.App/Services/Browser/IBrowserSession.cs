namespace AutoLogin.App.Services.Browser;

public interface IBrowserSession
{
    Task InitializeAsync();

    Task NavigateAsync(string url, CancellationToken cancellationToken = default);

    Task<string> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default);

    Task WaitForDocumentReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    Task<string> GetCurrentUrlAsync(CancellationToken cancellationToken = default);
}
