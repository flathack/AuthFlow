using System.Reflection;

namespace AutoLogin.App;

internal static class AppInfo
{
    public const string Name = "AuthFlow";

    public static string Version { get; } =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppInfo).Assembly.GetName().Version?.ToString(3)
        ?? "0.1.0";

    public static string Title => $"{Name} v{Version}";
}
