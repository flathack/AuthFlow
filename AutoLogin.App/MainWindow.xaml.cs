using System.IO;
using System.Windows;
using AutoLogin.App.Models;
using AutoLogin.App.Services.Automation;
using AutoLogin.App.Services.Browser;
using AutoLogin.App.Services.Profiles;
using AutoLogin.App.Services.Security;
using AutoLogin.App.Services.Storage;
using Microsoft.Web.WebView2.Core;
using FormsSendKeys = System.Windows.Forms.SendKeys;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace AutoLogin.App;

public partial class MainWindow : Window
{
    private readonly ILoginEntryRepository _entryRepository;
    private readonly IAutomationProfileCatalog _profileCatalog;
    private readonly ICredentialProtector _credentialProtector;
    private readonly IAutomationEngine _automationEngine;
    private readonly IBrowserSession _browserSession;

    private List<LoginEntry> _entries = [];
    private List<AutomationProfile> _profiles = [];

    public MainWindow(
        ILoginEntryRepository entryRepository,
        IAutomationProfileCatalog profileCatalog,
        ICredentialProtector credentialProtector,
        IAutomationEngine automationEngine)
    {
        InitializeComponent();
        Title = AppInfo.Title;
        _entryRepository = entryRepository;
        _profileCatalog = profileCatalog;
        _credentialProtector = credentialProtector;
        _automationEngine = automationEngine;
        _browserSession = new WebView2BrowserSession(BrowserView);
    }

    private LoginEntry? SelectedEntry => EntriesComboBox.SelectedItem as LoginEntry;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await ReloadAsync();
        await InitializeBrowserAsync();
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

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await _browserSession.InitializeAsync();

            if (BrowserView.CoreWebView2 is not null)
            {
                BrowserView.CoreWebView2.LaunchingExternalUriScheme += CoreWebView2_LaunchingExternalUriScheme;
                BrowserView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            }
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Browser konnte nicht initialisiert werden: {exception.Message}";
        }
    }

    private async Task ReloadAsync()
    {
        try
        {
            _profiles = (await _profileCatalog.GetProfilesAsync()).OrderBy(profile => profile.Name).ToList();
            _entries = (await _entryRepository.GetAllAsync()).OrderBy(entry => entry.DisplayName).ToList();

            EntriesComboBox.ItemsSource = null;
            EntriesComboBox.ItemsSource = _entries;

            if (_entries.Count > 0)
            {
                EntriesComboBox.SelectedIndex = 0;
                StatusTextBlock.Text = $"Bereit: {_entries.Count} Einträge geladen.";
            }
            else
            {
                AddressTextBlock.Text = "Kein Ziel ausgewählt";
                StatusTextBlock.Text = "Noch keine Einträge vorhanden. Bitte zuerst einen Eintrag anlegen.";
            }
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Fehler beim Laden: {exception.Message}";
        }
    }

    private void EntriesComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SelectedEntry is null)
        {
            AddressTextBlock.Text = "Kein Ziel ausgewählt";
            return;
        }

        AddressTextBlock.Text = SelectedEntry.StartUrl;
        StatusTextBlock.Text = $"Ausgewählt: {SelectedEntry.DisplayName}";
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var editor = new EntryEditorWindow(_profiles, _credentialProtector)
        {
            Owner = this
        };

        if (editor.ShowDialog() != true || editor.EditedEntry is null || string.IsNullOrWhiteSpace(editor.Password))
        {
            return;
        }

        var entry = editor.EditedEntry;
        entry.EncryptedPassword = _credentialProtector.Protect(editor.Password);
        entry.EncryptedTotpSecret = string.IsNullOrWhiteSpace(editor.TotpSecret)
            ? null
            : _credentialProtector.Protect(editor.TotpSecret);
        entry.CreatedAt = DateTimeOffset.UtcNow;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await _entryRepository.SaveAsync(entry);
        await ReloadAsync();
        EntriesComboBox.SelectedItem = _entries.FirstOrDefault(candidate => candidate.Id == entry.Id);
        StatusTextBlock.Text = $"Eintrag '{entry.DisplayName}' wurde angelegt.";
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEntry is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Eintrag auswählen.";
            return;
        }

        var clone = SelectedEntry.Clone();
        var editor = new EntryEditorWindow(_profiles, _credentialProtector, clone)
        {
            Owner = this
        };

        if (editor.ShowDialog() != true || editor.EditedEntry is null)
        {
            return;
        }

        var updatedEntry = editor.EditedEntry;
        updatedEntry.CreatedAt = SelectedEntry.CreatedAt;
        updatedEntry.UpdatedAt = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(editor.Password))
        {
            updatedEntry.EncryptedPassword = SelectedEntry.EncryptedPassword;
        }
        else
        {
            updatedEntry.EncryptedPassword = _credentialProtector.Protect(editor.Password);
        }

        updatedEntry.EncryptedTotpSecret = string.IsNullOrWhiteSpace(editor.TotpSecret)
            ? SelectedEntry.EncryptedTotpSecret
            : _credentialProtector.Protect(editor.TotpSecret);

        await _entryRepository.SaveAsync(updatedEntry);
        await ReloadAsync();
        EntriesComboBox.SelectedItem = _entries.FirstOrDefault(candidate => candidate.Id == updatedEntry.Id);
        StatusTextBlock.Text = $"Eintrag '{updatedEntry.DisplayName}' wurde aktualisiert.";
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await NavigateSelectedEntryAsync(runAutomation: false);
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await NavigateSelectedEntryAsync(runAutomation: true);
    }

    private async Task NavigateSelectedEntryAsync(bool runAutomation)
    {
        if (SelectedEntry is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Eintrag auswählen.";
            return;
        }

        try
        {
            AddressTextBlock.Text = SelectedEntry.StartUrl;
            StatusTextBlock.Text = $"Lade '{SelectedEntry.DisplayName}'...";
            await _browserSession.NavigateAsync(SelectedEntry.StartUrl);
            await _browserSession.WaitForDocumentReadyAsync(TimeSpan.FromSeconds(20));
            StatusTextBlock.Text = "Seite geladen.";

            if (runAutomation)
            {
                await RunAutomationAsync();
            }
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text = $"Navigation fehlgeschlagen: {exception.Message}";
        }
    }

    private async Task RunAutomationAsync()
    {
        if (SelectedEntry is null)
        {
            StatusTextBlock.Text = "Bitte zuerst einen Eintrag auswählen.";
            return;
        }

        var profile = _profiles.FirstOrDefault(candidate => candidate.Id == SelectedEntry.AutomationProfileId);
        if (profile is null)
        {
            StatusTextBlock.Text = $"Profil '{SelectedEntry.AutomationProfileId}' wurde nicht gefunden.";
            return;
        }

        StatusTextBlock.Text = "Automation läuft...";
        var result = await _automationEngine.ExecuteAsync(_browserSession, SelectedEntry, profile);
        StatusTextBlock.Text = result.IsSuccess
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
                StatusTextBlock.Text = $"Externer Start '{uri.Scheme}://' wurde blockiert, Detection wird übersprungen.";
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
                    StatusTextBlock.Text = "Citrix-Protokoll blockiert, Skip Detection wurde ausgelöst.";
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
                    StatusTextBlock.Text = $"Download abgeschlossen, öffne {Path.GetFileName(finalPath)} ...";
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
                        StatusTextBlock.Text = $"ICA-Datei wurde geladen, konnte aber nicht geöffnet werden: {exception.Message}";
                    });
                }
            }
        };
    }
}
