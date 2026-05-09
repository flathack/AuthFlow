# AutoLogin-App

Windows-Desktop-App auf Basis von `WPF + .NET 8` mit lokalem Vault fuer Web-Logins.

## Umgesetzte Kernbereiche

- `SQLite` fuer Login-Metadaten und verschluesselte Passwort-Referenzen
- `DPAPI` fuer benutzergebundene Passwortverschluesselung
- optionales, ebenfalls verschluesseltes `TOTP Secret` fuer 2FA-Logins
- `WebView2` als eingebetteter Browser
- JSON-basierte, bearbeitbare Automationsprofile in `AutoLogin.App/Profiles`
- CRUD-Oberflaeche fuer Login-Ziele
- Browserfenster mit optionalem `Open & Login`-Flow
- TOTP-faehige Automationsschritte fuer 2FA-Sequenzen
- eigenes App-Icon in Blau mit rundem Badge und offenem Schloss unter `AutoLogin.App/Assets`

## Projektstruktur

- `AutoLogin.App/Models`: Datenmodelle fuer Entries, Profile und Ausfuehrungsresultate
- `AutoLogin.App/Services/Storage`: SQLite-Vault
- `AutoLogin.App/Services/Security`: DPAPI-Schutz
- `AutoLogin.App/Services/Profiles`: Laden der JSON-Profile
- `AutoLogin.App/Services/Automation`: Ausfuehrung der Login-Schritte im Browser

## Demo-Profile

- `generic-form.json`
- `the-internet-demo.json`
- `totp-sequence-demo.json`
- `toyota-citrix-login.json`
