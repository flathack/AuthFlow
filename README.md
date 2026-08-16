# AuthFlow

[Deutsch](README.de.md)

<img src="assets/authflow-icon.png" alt="AuthFlow icon" width="160">

AuthFlow is a Windows desktop app for repeatable web and Citrix-style login workflows. It combines a local encrypted vault, editable automation profiles, and an embedded WebView2 browser.

## Features

- Local SQLite vault for login metadata
- Password and optional TOTP protection through Windows DPAPI
- Embedded WebView2 browser
- Editable automation profiles
- Automated password and two-factor login sequences
- Verified automatic updates from stable GitHub releases
- Per-user Windows installer without administrator rights
- Self-contained Windows x64 and ARM64 packages

## Download

The current version is **v1.0.3**. The installer is recommended:

- [Windows x64 installer](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-x64-setup.exe)
- [Windows ARM64 installer](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-arm64-setup.exe)
- [Portable Windows x64 ZIP](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-x64.zip)
- [Portable Windows ARM64 ZIP](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-arm64.zip)
- [Release notes and SHA-256 checksums](https://github.com/flathack/AuthFlow/releases/tag/v1.0.3)

The installer preserves the local encrypted vault and settings during upgrades. AuthFlow checks stable GitHub releases automatically, verifies the downloaded installer, and asks before installing it. Microsoft Edge WebView2 Runtime is required on Windows 10 or Windows 11.

## Security

Credentials remain local. Passwords and optional TOTP secrets are encrypted for the current Windows user with DPAPI. AuthFlow validates HTTPS destinations before entering sensitive values.

This public repository is the product and release page for AuthFlow. Release packages are built from the privately maintained source repository.

## License

MIT License. See [LICENSE](LICENSE).
