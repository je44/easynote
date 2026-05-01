# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote is a lightweight desktop sticky-note and todo app for Windows.
It is designed to stay close to the desktop and provide a quick place to
capture tasks with tray controls, a global hotkey, and local persistence.

## Source Code Notice

This `main` branch publishes the original EasyNote source code. It is intended
for personal developers, learners, and anyone who wants to shape EasyNote into
their own preferred version.

This branch is a source-code starting point, not the recommended daily-use
download. It may include unfinished or unstable changes as the project evolves.

If you only want to use EasyNote without modifying it, go to the
[Install](#install) section and download the release version.

## Preview

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="340" />

## Install

Go to the [Release page](https://github.com/je44/easynote/releases) to download
the daily-use version.

Supports Windows x64 and x86.

### How should I choose a release?

| Version | Feature | Link |
| --- | --- | --- |
| Stable | Official release, suitable for daily use. | [Release](https://github.com/je44/easynote/releases/latest) |

## Features

- Desktop-attached note board UI
- Pending / completed views
- Add, pin, complete, restore, and delete todos
- Tray icon with quick actions
- `Ctrl + Alt + N` global show / hide hotkey
- Auto-save for todos
- Window position, size, and opacity persistence
- Optional portable mode with local `data/` storage

## Run From Source

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

## Built-In Self-Test

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test
```

Optional custom report path:

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test --self-test-output=.\.omx\self-test-report.json
```

The self-test validates:

- startup and tray initialization
- add / pin / complete / restore / delete todo flows
- tray show, hotkey show/hide, and top-right docking
- autosave, autostart toggle, and window state persistence

## Packaging

Build installer and portable package:

```powershell
.\build-windows-installer.ps1
```

Default outputs:

- publish folder: `publish\app`
- portable folder: `publish\portable`
- portable zip: `publish\EasyNote-portable-win-x64.zip`
- installer: `installer\EasyNoteSetup.exe`

Build only the portable package:

```powershell
.\build-windows-installer.ps1 -SkipInstaller
```

Portable mode behavior:

- extract `publish\EasyNote-portable-win-x64.zip`
- run `EasyNote.exe`
- data, window state, and logs are stored under the local `data\` folder

## Data Location

- standard mode: `%AppData%\easy-note`
- portable mode: `data\` under the app folder

## Project Structure

- `EasyNote/`: WPF application source
- `easy-note-wpf.sln`: solution entry point
- `build-windows-installer.ps1`: publish and packaging script
- `installer.iss`: Inno Setup installer config

## Tech Stack

- WPF
- .NET 10
- `Hardcodet.NotifyIcon.Wpf`
