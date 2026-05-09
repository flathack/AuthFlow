using System.Windows;
using AutoLogin.App.Models;
using AutoLogin.App.Services.Automation;
using AutoLogin.App.Services.Profiles;
using AutoLogin.App.Services.Security;
using AutoLogin.App.Services.Storage;

namespace AutoLogin.App;

public partial class MainWindow : Window
{
    private readonly ILoginEntryRepository _entryRepository;
    private readonly IAutomationProfileCatalog _profileCatalog;
    private readonly ICredentialProtector _credentialProtector;
    private readonly IAutomationEngine _automationEngine;

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
    }

    private LoginEntry? SelectedEntry => EntriesComboBox.SelectedItem as LoginEntry;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await ReloadAsync();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
            return;
        }

        StatusTextBlock.Text = $"Ausgewählt: {SelectedEntry.DisplayName}";
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var editor = new EntryEditorWindow(_profiles, _credentialProtector);
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
        var editor = new EntryEditorWindow(_profiles, _credentialProtector, clone);
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
            ? null
            : _credentialProtector.Protect(editor.TotpSecret);

        await _entryRepository.SaveAsync(updatedEntry);
        await ReloadAsync();
        EntriesComboBox.SelectedItem = _entries.FirstOrDefault(candidate => candidate.Id == updatedEntry.Id);
        StatusTextBlock.Text = $"Eintrag '{updatedEntry.DisplayName}' wurde aktualisiert.";
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        OpenBrowser(runAutomationOnLaunch: true);
    }

    private void OpenBrowser(bool runAutomationOnLaunch)
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

        var browserWindow = new BrowserWindow(SelectedEntry.Clone(), profile, _automationEngine, runAutomationOnLaunch)
        {
            Owner = this
        };

        browserWindow.Show();
        StatusTextBlock.Text = $"Anmeldung für '{SelectedEntry.DisplayName}' gestartet.";
    }
}
