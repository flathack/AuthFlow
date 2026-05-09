using System.IO;
using System.Windows;
using AutoLogin.App.Models;
using AutoLogin.App.Services.Automation;
using AutoLogin.App.Services.Browser;
using Microsoft.Web.WebView2.Core;
using FormsSendKeys = System.Windows.Forms.SendKeys;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace AutoLogin.App;

public partial class BrowserWindow : Window
{
    private readonly LoginEntry _entry;
    private readonly AutomationProfile _profile;
    private readonly IAutomationEngine _automationEngine;
    private readonly bool _runAutomationOnLaunch;
    private readonly IBrowserSession _browserSession;

    public BrowserWindow(
        LoginEntry entry,
        AutomationProfile profile,
        IAutomationEngine automationEngine,
        bool runAutomationOnLaunch)
    {
        InitializeComponent();
        Title = $"{AppInfo.Title} Browser";
        _entry = entry;
        _profile = profile;
        _automationEngine = automationEngine;
        _runAutomationOnLaunch = runAutomationOnLaunch;
        _browserSession = new WebView2BrowserSession(BrowserView);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        HeaderTextBlock.Text = _entry.DisplayName;
        SubHeaderTextBlock.Text = $"{_entry.StartUrl} | Profil: {_profile.Name}";

        try
        {
            await _browserSession.InitializeAsync();

            if (BrowserView.CoreWebView2 is not null)
            {
                BrowserView.CoreWebView2.LaunchingExternalUriScheme += CoreWebView2_LaunchingExternalUriScheme;
                BrowserView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            }

            await _browserSession.NavigateAsync(_entry.StartUrl);
            await _browserSession.WaitForDocumentReadyAsync(TimeSpan.FromSeconds(20));
            ExecutionStatusTextBlock.Text = "Seite geladen.";

            if (_runAutomationOnLaunch)
            {
                await RunAutomationAsync();
            }
        }
        catch (Exception exception)
        {
            ExecutionStatusTextBlock.Text = $"Browser konnte nicht initialisiert werden: {exception.Message}";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _browserSession.NavigateAsync(_entry.StartUrl);
            await _browserSession.WaitForDocumentReadyAsync(TimeSpan.FromSeconds(20));
            ExecutionStatusTextBlock.Text = "Seite neu geladen.";
        }
        catch (Exception exception)
        {
            ExecutionStatusTextBlock.Text = $"Neu laden fehlgeschlagen: {exception.Message}";
        }
    }

    private async void RunAutomationButton_Click(object sender, RoutedEventArgs e)
    {
        await RunAutomationAsync();
    }

    private async Task RunAutomationAsync()
    {
        ExecutionStatusTextBlock.Text = "Automation läuft...";

        var result = await _automationEngine.ExecuteAsync(_browserSession, _entry, _profile);
        ExecutionStatusTextBlock.Text = result.IsSuccess
            ? $"Erfolg in {result.Elapsed.TotalSeconds:0.0}s: {result.Message}"
            : $"Fehlgeschlagen in {result.Elapsed.TotalSeconds:0.0}s: {result.Message}";
    }

    private void CoreWebView2_LaunchingExternalUriScheme(object? sender, CoreWebView2LaunchingExternalUriSchemeEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
        {
            return;
        }

        var blockedSchemes = new[] { "citrix", "citrixsso", "receiver", "ica" };
        if (blockedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            Dispatcher.Invoke(() =>
            {
                ExecutionStatusTextBlock.Text = $"Externer Start '{uri.Scheme}://' wurde blockiert, Detection wird übersprungen.";
            });

            _ = Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(150);
                BrowserView.Focus();
                FormsSendKeys.SendWait("{ESC}");

                await Task.Delay(500);

                if (BrowserView.CoreWebView2 is null)
                {
                    return;
                }

                await BrowserView.CoreWebView2.ExecuteScriptAsync(
                    """
                    (() => {
                        const candidates = Array.from(document.querySelectorAll('a, button, div[role="button"], span[role="button"]'));
                        const skipLink = candidates.find(candidate => {
                            const text = (candidate.innerText || candidate.textContent || candidate.value || '').trim().toLowerCase();
                            return text.includes('skip detection') || text.includes('erkennung ueberspringen') || text.includes('erkennung überspringen');
                        });

                        if (!skipLink) {
                            return false;
                        }

                        if (skipLink.scrollIntoView) {
                            skipLink.scrollIntoView({ block: 'center', inline: 'center' });
                        }

                        skipLink.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
                        skipLink.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
                        if (typeof skipLink.click === 'function') {
                            skipLink.click();
                        }

                        return true;
                    })()
                    """);

                Dispatcher.Invoke(() =>
                {
                    ExecutionStatusTextBlock.Text = "Citrix-Protokoll blockiert, Skip Detection wurde ausgelöst.";
                });
            });
        }
    }

    private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Handled = true;

        var downloadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        Directory.CreateDirectory(downloadDirectory);

        var fileName = Path.GetFileName(e.ResultFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"download-{DateTime.Now:yyyyMMdd-HHmmss}.bin";
        }

        var finalPath = Path.Combine(downloadDirectory, fileName);
        e.ResultFilePath = finalPath;

        var operation = e.DownloadOperation;
        operation.StateChanged += (_, _) =>
        {
            if (operation.State == CoreWebView2DownloadState.Completed &&
                File.Exists(finalPath) &&
                string.Equals(Path.GetExtension(finalPath), ".ica", StringComparison.OrdinalIgnoreCase))
            {
                Dispatcher.Invoke(() =>
                {
                    ExecutionStatusTextBlock.Text = $"Download abgeschlossen, öffne {Path.GetFileName(finalPath)} ...";
                });

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = finalPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception exception)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ExecutionStatusTextBlock.Text = $"ICA-Datei wurde geladen, konnte aber nicht geöffnet werden: {exception.Message}";
                    });
                }
            }
        };
    }
}
