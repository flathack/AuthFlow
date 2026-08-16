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
- Eigenständige Windows-Pakete für x64 und ARM64

## Download

Die aktuelle Version ist **v1.0.2**:

- [Windows x64 ZIP](https://github.com/flathack/AuthFlow/releases/download/v1.0.2/AuthFlow-v1.0.2-windows-x64.zip)
- [Windows ARM64 ZIP](https://github.com/flathack/AuthFlow/releases/download/v1.0.2/AuthFlow-v1.0.2-windows-arm64.zip)
- [Release Notes und SHA-256-Prüfsummen](https://github.com/flathack/AuthFlow/releases/tag/v1.0.2)

Entpacke das vollständige ZIP, bevor du `AuthFlow.exe` startest. Unter Windows 10 oder Windows 11 wird die Microsoft Edge WebView2 Runtime benötigt.

## Sicherheit

Zugangsdaten bleiben lokal. Passwörter und optionale TOTP-Secrets werden mit DPAPI für den aktuellen Windows-Benutzer verschlüsselt. AuthFlow prüft HTTPS-Ziele, bevor sensible Werte eingegeben werden.

Dieses öffentliche Repository ist die Produkt- und Release-Seite von AuthFlow. Die Release-Pakete werden aus dem privat gepflegten Quell-Repository erstellt.

## Lizenz

MIT-Lizenz. Siehe [LICENSE](LICENSE).
