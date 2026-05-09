## Allgemeine Verhaltensregeln

Diese Regeln sollen typische LLM-Coding-Fehler reduzieren. Sie priorisieren Vorsicht vor Geschwindigkeit; bei trivialen Aufgaben ist Augenmass erlaubt.

### 1. Vor dem Coden nachdenken

- Keine Annahmen verstecken: Annahmen explizit nennen.
- Bei Unsicherheit oder mehreren plausiblen Interpretationen nachfragen oder die Alternativen benennen.
- Tradeoffs sichtbar machen, besonders wenn eine einfachere Loesung existiert.
- Bei unklarer Aufgabenstellung stoppen, die Unklarheit benennen und klaeren.

### 2. Einfachheit zuerst

- Nur den angefragten Umfang umsetzen, keine spekulativen Features.
- Keine Abstraktionen fuer einmalig genutzten Code.
- Keine Flexibilitaet, Konfigurierbarkeit oder Sonderfallbehandlung einfuehren, die nicht gebraucht wird.
- Unmoegliche Fehlerfaelle nicht mit zusaetzlichem Code absichern.
- Wenn eine Loesung deutlich kuerzer und klarer sein kann, vereinfachen.

### 3. Chirurgische Aenderungen

- Nur Dateien und Zeilen anfassen, die fuer die Aufgabe noetig sind.
- Angrenzenden Code, Kommentare oder Formatierung nicht nebenbei verbessern.
- Bestehenden Stil beibehalten, auch wenn eine andere Loesung persoenlich naheliegt.
- Unrelated Dead Code nur erwaehnen, nicht loeschen.
- Imports, Variablen oder Funktionen entfernen, wenn sie durch die eigene Aenderung unbenutzt wurden.
- Jede geaenderte Zeile soll direkt auf die Nutzeranfrage zurueckfuehrbar sein.

### 4. Zielorientiert arbeiten

- Aufgaben in verifizierbare Ziele uebersetzen.
- Bei Bugfixes nach Moeglichkeit erst einen reproduzierenden Test schreiben oder einen bestehenden Testfall benennen.
- Bei Validierung invaliden Input testen und dann die Implementierung daran ausrichten.
- Bei Refactorings pruefen, dass relevante Tests vor und nach der Aenderung bestehen koennen.
- Fuer mehrstufige Aufgaben einen kurzen Plan mit passenden Verifikationsschritten nennen, zum Beispiel:

```text
1. Kontext lesen -> verify: betroffene Dateien und Tests identifiziert
2. Aenderung umsetzen -> verify: Build des betroffenen Targets
3. Verhalten pruefen -> verify: passender CTest oder manueller Check
```

Diese Regeln sind erfolgreich, wenn Diffs kleiner bleiben, weniger unnoetige Rewrites entstehen und Rueckfragen vor der Implementierung kommen statt nach vermeidbaren Fehlern.
