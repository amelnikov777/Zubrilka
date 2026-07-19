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
| 2 | Import from `.xlsx` (ClosedXML) | ⬜ planned |
| 3 | Start screen + switch-box (language selection) | ⬜ planned |
| 4 | Playback engine + Android TextToSpeech | ⬜ planned |
| 5 | Settings screens (font, speed, pauses) | ⬜ planned |
| 6 | Improvements | ⬜ planned |

## Project structure

```
Models/     Data classes: Block, Card, AppSettings
Data/       SQLite: AppDatabase + repositories (Block/Card/Settings)
Platforms/  Android-specific host code
Resources/  Fonts, images, styles, icons
```

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

## Working across two machines

1. `git pull` before you start, `git push` when you finish.
2. Each machine needs the prerequisites above installed once (they are **not**
   stored in git — only source code is).
3. Local settings and build output are intentionally git-ignored
   (`.gitignore`, `.gitattributes` keep line endings consistent).
