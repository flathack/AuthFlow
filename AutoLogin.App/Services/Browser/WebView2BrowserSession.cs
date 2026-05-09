using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;

namespace AutoLogin.App.Services.Browser;

public sealed class WebView2BrowserSession : IBrowserSession
{
    private readonly WebView2 _webView;

    public WebView2BrowserSession(WebView2 webView)
    {
        _webView = webView;
    }

    public async Task InitializeAsync()
    {
        await _webView.EnsureCoreWebView2Async();
    }

    public Task NavigateAsync(string url, CancellationToken cancellationToken = default)
    {
        _webView.Source = new Uri(url);
        return Task.CompletedTask;
    }

    public Task<string> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        return _webView.ExecuteScriptAsync(script);
    }

    public async Task WaitForDocumentReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readyState = await ReadScriptValueAsync<string>("document.readyState", cancellationToken);
            if (string.Equals(readyState, "complete", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(readyState, "interactive", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(200, cancellationToken);
        }

        throw new TimeoutException($"Die Seite wurde nicht innerhalb von {timeout.TotalSeconds:0} Sekunden geladen.");
    }

    public async Task<string> GetCurrentUrlAsync(CancellationToken cancellationToken = default)
    {
        return await ReadScriptValueAsync<string>("window.location.href", cancellationToken) ?? string.Empty;
    }

    private async Task<T?> ReadScriptValueAsync<T>(string expression, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await ExecuteScriptAsync(expression, cancellationToken);
        return JsonSerializer.Deserialize<T>(result);
    }
}
