# AuthFlow

AuthFlow is a Windows desktop app for repeatable web and Citrix-style login workflows.
It combines a local encrypted vault, editable automation profiles, and an embedded WebView2 browser so recurring login steps can be executed in a controlled way.

## Features

- WPF desktop app built on .NET 8
- local SQLite vault for login metadata
- DPAPI-based password protection tied to the current Windows user
- optional encrypted TOTP secrets for two-factor login flows
- embedded WebView2 browser
- editable JSON automation profiles in `AutoLogin.App/Profiles`
- CRUD UI for login targets
- optional `Open & Login` browser flow

## Project Structure

- `AutoLogin.App/Models`: login entries, profiles, and execution results
- `AutoLogin.App/Services/Storage`: SQLite vault access
- `AutoLogin.App/Services/Security`: DPAPI protection
- `AutoLogin.App/Services/Profiles`: JSON profile loading
- `AutoLogin.App/Services/Automation`: browser automation execution

## Run

Install the .NET 8 SDK and start the app from PowerShell:

```powershell
dotnet run --project AutoLogin.App
```

On Windows you can also use:

```powershell
.\launch.cmd
```

## Demo Profiles

- `generic-form.json`
- `the-internet-demo.json`
- `totp-sequence-demo.json`
- `toyota-citrix-login.json`

## Security Notes

Credentials are intended to stay local. Passwords and optional TOTP secrets are protected with Windows DPAPI for the current user account.
Do not commit real profiles, exported credentials, screenshots, or logs that contain account-specific information.

## Release

Windows release ZIPs can be built with:

```powershell
.\scripts\release_windows.ps1 -SkipUpload
```

Remove `-SkipUpload` to create the Git tag and GitHub Release after checking the generated assets.

## License

MIT License. See [LICENSE](LICENSE).
