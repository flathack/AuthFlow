using System.IO;
using System.Windows;
using AutoLogin.App.Services.Automation;
using AutoLogin.App.Services.Profiles;
using AutoLogin.App.Services.Security;
using AutoLogin.App.Services.Storage;

namespace AutoLogin.App;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoLoginApp");

            Directory.CreateDirectory(appDataDirectory);

            var entryRepository = new SqliteLoginEntryRepository(Path.Combine(appDataDirectory, "autologin.db"));
            await entryRepository.InitializeAsync();

            var profileCatalog = new JsonAutomationProfileCatalog(Path.Combine(AppContext.BaseDirectory, "Profiles"));
            var protector = new DpapiCredentialProtector();
            var automationEngine = new BrowserAutomationEngine(protector);

            var mainWindow = new MainWindow(entryRepository, profileCatalog, protector, automationEngine);
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"Die Anwendung konnte nicht gestartet werden.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                AppInfo.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }
}
