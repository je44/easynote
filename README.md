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

### Current Release Update

This release refresh syncs the stable source that was validated through the
local experimental workflow back into the `release` branch.

- split the oversized `MainWindow` code-behind into focused partial classes for
  automation, desktop hosting, helper routines, resize dragging, theme handling,
  todo operations, UI events, and Win32 interop
- kept the experimental workspace local-only through `.gitignore`, so future UI
  and feature work can be verified in `EasyNote-Experiment` before source
  backport
- refined todo-item behavior and XAML wiring after the source split
- added `PROJECT_ANALYSIS_REPORT.md` as the release-side source structure
  reference for future maintainers
- refreshed the downloadable Windows packages for x64 and x86, including both
  installer EXE and portable ZIP variants

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

- [Windows x64 installer](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x64.zip)
- [Windows x86 installer](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x86-setup.exe)
- [Windows x86 portable ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x86.zip)

For installer builds, run the downloaded setup EXE. For portable builds, extract
the ZIP and run `EasyNote.exe`. This release build is published from the
`release` branch.

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
- portable folders: `publish\portable-x64`, `publish\portable-x86`
- portable zips: `publish\EasyNote-v1.0-portable-win-x64.zip`,
  `publish\EasyNote-v1.0-portable-win-x86.zip`
- installers: `installer\EasyNote-v1.0-win-x64-setup.exe`,
  `installer\EasyNote-v1.0-win-x86-setup.exe`

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
