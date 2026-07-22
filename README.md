# Zubrilka

A personal Android app for memorizing phrases while learning languages:
flashcards with multilingual, on-device text-to-speech (offline).

- **Stack:** .NET MAUI, C#, MVVM
- **Target:** Android only (`net10.0-android`)
- **Storage:** local SQLite via `sqlite-net-pcl`
- **UI language:** English

## Project status (by phase)

| Phase | Scope | Status |
|-------|-------|--------|
| 1 | Foundation: project, models, SQLite, repositories | ✅ done |
| 2 | Import from `.xlsx` (ClosedXML) | ✅ done |
| 3 | Start screen + switch-box (language selection) | ✅ done |
| 4 | Playback engine + Android TextToSpeech | ✅ done |
| 5 | Settings screens (font, speed, pauses) | ✅ done |
| 6 | Improvements | 🔄 in progress |

### Planned improvements

- **Voice selection** — let the user pick among the device's installed voices for each
  language (Android usually offers several per locale) and remember it per block.
- Direct import from Google Sheets, card editing, and export back to `.xlsx`
  (all marked `[FUTURE]` in the code).

## Project structure

```
Models/      Data classes: Block, Card, AppSettings
Data/        SQLite: AppDatabase + repositories (Block/Card/Settings)
Services/    xlsx import, language catalog, TTS abstraction
ViewModels/  MVVM logic for every screen
Views/       XAML screens (blocks list, switch-box, playback, settings)
Behaviors/   Tap/long-press gesture, phrase font auto-shrink
Converters/  Small XAML value converters
Platforms/   Android-specific host code + TextToSpeech implementation
Resources/   Fonts, images, styles, icons, languages.json
```

## Import file format

One `.xlsx` file = one block. Row 1 holds the language codes (column headers),
each following row is one card:

| he | ru | en |
|----|----|----|
| מתי הלו"ז שלנו להיום? | Какое у нас расписание? | What is our schedule? |

Use ISO 639-1 codes as headers (`he`, `ru`, `en`, `de`, `fr`, `bg`, …). They are
mapped to speech locales and text direction in
[`Resources/Raw/languages.json`](Resources/Raw/languages.json); extend that file
to add languages. Playback needs the matching Android voice installed
(Settings → System → Languages & input → Text-to-speech output).

## Prerequisites (per machine)

This project builds from a plain .NET SDK — no Visual Studio required.

1. **.NET 10 SDK** — https://dotnet.microsoft.com/download
2. **MAUI Android workload:**
   ```
   dotnet workload install maui-android
   ```
3. **Android SDK + JDK.** The project expects them under `%LOCALAPPDATA%\Android`
   (see `Directory.Build.props`). Install them once with:
   ```
   dotnet build Zubrilka.csproj -t:InstallAndroidDependencies -f net10.0-android ^
     -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" ^
     -p:JavaSdkDirectory="%LOCALAPPDATA%\Android\jdk" ^
     -p:AcceptAndroidSDKLicenses=True
   ```
   If you already have an Android SDK/JDK elsewhere (e.g. from Visual Studio),
   you can skip this — `Directory.Build.props` only applies its paths when the
   `%LOCALAPPDATA%\Android` folders exist, otherwise normal auto-detection runs.

## Build

```
dotnet build Zubrilka.csproj -c Debug
```

## Background playback on Xiaomi (MIUI / HyperOS)

Playback runs in a foreground service with a wake lock, and the app offers to turn off
battery optimisation the first time you press Play. Xiaomi still needs a few switches that
no app can set for itself — do these once, or playback stops a minute after the screen
turns off:

1. **Settings → Apps → Zubrilka → Battery saver → No restrictions**
2. **Settings → Apps → Zubrilka → Autostart → on**
3. **Recents (task switcher) → swipe down on Zubrilka → padlock**, so cleaning recents
   doesn't kill it
4. Accept the "run without battery restrictions" prompt the app shows on first playback

Wording moves between MIUI versions; the entries are usually under Settings → Apps →
Manage apps → Zubrilka, and Autostart may live in the Security app.

## Working across two machines

1. `git pull` before you start, `git push` when you finish.
2. Each machine needs the prerequisites above installed once (they are **not**
   stored in git — only source code is).
3. Local settings and build output are intentionally git-ignored
   (`.gitignore`, `.gitattributes` keep line endings consistent).
