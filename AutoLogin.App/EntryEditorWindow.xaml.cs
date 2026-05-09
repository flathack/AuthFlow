using System.Windows;
using AutoLogin.App.Models;
using AutoLogin.App.Services.Security;

namespace AutoLogin.App;

public partial class EntryEditorWindow : Window
{
    private readonly LoginEntry? _existingEntry;

    public EntryEditorWindow(IReadOnlyList<AutomationProfile> profiles, ICredentialProtector? credentialProtector = null, LoginEntry? entry = null)
    {
        InitializeComponent();
        Title = $"{AppInfo.Title} - Login-Ziel anlegen";
        _existingEntry = entry;

        ProfileComboBox.ItemsSource = profiles;

        if (entry is null)
        {
            if (profiles.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }

            return;
        }

        Title = $"{AppInfo.Title} - Login-Ziel bearbeiten";
        DisplayNameTextBox.Text = entry.DisplayName;
        StartUrlTextBox.Text = entry.StartUrl;
        UsernameTextBox.Text = entry.Username;
        ProfileComboBox.SelectedValue = entry.AutomationProfileId;
        AutoSubmitCheckBox.IsChecked = entry.AutoSubmit;

        if (credentialProtector is not null && !string.IsNullOrWhiteSpace(entry.EncryptedTotpSecret))
        {
            try
            {
                TotpSecretTextBox.Text = credentialProtector.Unprotect(entry.EncryptedTotpSecret);
            }
            catch
            {
                TotpSecretTextBox.Text = string.Empty;
            }
        }
    }

    public LoginEntry? EditedEntry { get; private set; }

    public string Password => PasswordBox.Password;

    public string TotpSecret => TotpSecretTextBox.Text.Trim();

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplayNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(StartUrlTextBox.Text) ||
            string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
            ProfileComboBox.SelectedValue is not string profileId)
        {
            System.Windows.MessageBox.Show(
                "Bitte Name, URL, Benutzer und Profil ausfüllen.",
                "Validierung",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_existingEntry is null && string.IsNullOrWhiteSpace(Password))
        {
            System.Windows.MessageBox.Show(
                "Für neue Einträge ist ein Passwort erforderlich.",
                "Validierung",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!Uri.TryCreate(StartUrlTextBox.Text, UriKind.Absolute, out _))
        {
            System.Windows.MessageBox.Show(
                "Bitte eine gültige absolute URL angeben.",
                "Validierung",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        EditedEntry = new LoginEntry
        {
            Id = _existingEntry?.Id ?? Guid.NewGuid(),
            DisplayName = DisplayNameTextBox.Text.Trim(),
            StartUrl = StartUrlTextBox.Text.Trim(),
            Username = UsernameTextBox.Text.Trim(),
            AutomationProfileId = profileId,
            AutoSubmit = AutoSubmitCheckBox.IsChecked == true,
            EncryptedPassword = _existingEntry?.EncryptedPassword ?? string.Empty,
            EncryptedTotpSecret = _existingEntry?.EncryptedTotpSecret,
            CreatedAt = _existingEntry?.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
