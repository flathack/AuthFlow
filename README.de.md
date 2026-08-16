# AuthFlow

[English](README.md)

<img src="assets/authflow-icon.png" alt="AuthFlow-Icon" width="160">

AuthFlow ist eine Windows-Desktop-App für wiederholbare Web- und Citrix-Anmeldeabläufe. Sie verbindet einen lokalen verschlüsselten Tresor, bearbeitbare Automationsprofile und einen eingebetteten WebView2-Browser.

## Funktionen

- Lokaler SQLite-Tresor für Login-Metadaten
- Schutz von Passwörtern und optionalen TOTP-Secrets mit Windows DPAPI
- Eingebetteter WebView2-Browser
- Bearbeitbare Automationsprofile
- Automatisierte Passwort- und Zwei-Faktor-Anmeldungen
- Geprüfte automatische Updates aus stabilen GitHub-Releases
- Windows-Installer pro Benutzer ohne Administratorrechte
- Eigenständige Windows-Pakete für x64 und ARM64

## Download

Die aktuelle Version ist **v1.0.3**. Der Installer ist die empfohlene Variante:

- [Windows x64 Installer](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-x64-setup.exe)
- [Windows ARM64 Installer](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-arm64-setup.exe)
- [Portables Windows x64 ZIP](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-x64.zip)
- [Portables Windows ARM64 ZIP](https://github.com/flathack/AuthFlow/releases/download/v1.0.3/AuthFlow-v1.0.3-windows-arm64.zip)
- [Release Notes und SHA-256-Prüfsummen](https://github.com/flathack/AuthFlow/releases/tag/v1.0.3)

Der Installer bewahrt den lokalen verschlüsselten Tresor und die Einstellungen bei Updates. AuthFlow prüft stabile GitHub-Releases automatisch, verifiziert den heruntergeladenen Installer und fragt vor der Installation nach. Unter Windows 10 oder Windows 11 wird die Microsoft Edge WebView2 Runtime benötigt.

## Sicherheit

Zugangsdaten bleiben lokal. Passwörter und optionale TOTP-Secrets werden mit DPAPI für den aktuellen Windows-Benutzer verschlüsselt. AuthFlow prüft HTTPS-Ziele, bevor sensible Werte eingegeben werden.

Dieses öffentliche Repository ist die Produkt- und Release-Seite von AuthFlow. Die Release-Pakete werden aus dem privat gepflegten Quell-Repository erstellt.

## Lizenz

MIT-Lizenz. Siehe [LICENSE](LICENSE).
