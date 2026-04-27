# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote is a lightweight desktop sticky-note and todo app for Windows.
It is designed to stay close to the desktop and provide a quick place to
capture tasks with tray controls, a global hotkey, and local persistence.
It does not require WebView2.

## Preview

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## Release Branch Notes

This `release` branch carries the packaged Windows build for EasyNote. The
`main` branch remains the public baseline for shared development.

Compared with `main`, this branch currently includes:

- a wider personalized WPF interface with a softer beige visual style
- updated Todo / Done navigation and an empty-state layout matching the preview
- an inline draft flow for adding todos
- pinned-item styling and refined item action states
- day / night eye-care theme switching, saved with the window state
- best-effort migration of existing AppData todos and window state into portable mode
- release README preview imagery for the customized interface

Use this branch when you want the packaged Windows app with the personalized
UI and workflow variant.

## Features

- Desktop-attached note board UI
- Pending / completed views
- Add, pin, complete, restore, and delete todos
- Tray icon with quick actions
- `Ctrl + Alt + N` global show / hide hotkey
- Auto-save for todos
- Window position, size, and opacity persistence
- Optional portable mode with local `data/` storage

## Install

Download the release build for Windows directly from GitHub Releases:

- [Download EasyNote.exe](https://github.com/je44/easynote/releases/download/release-2026-04-26/EasyNote.exe)

After downloading, run `EasyNote.exe` to start the app. This release build is
published from the `release` branch.

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
- WebView2 runtime is not required

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
