# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote is a lightweight desktop sticky-note and todo app for Windows.
It is designed to stay close to the desktop and provide a quick place to
capture tasks with tray controls, a global hotkey, and local persistence.
It does not require WebView2.

## Source Code Notice

This `main` branch is the original open-source code for EasyNote. It is meant
for people who want to read, change, and continuously build their own version
of EasyNote.

Because this branch follows the source code directly, it may include unfinished
or unstable changes. It is not recommended as the daily-use version.

If you only want to use EasyNote without doing personal development, go to the
[Install](#install) section and download the latest release version.

## 项目说明

`main` 分支是 EasyNote 的原始开源代码，适合查看源码、学习实现方式，或持续构建成适合你自己的个人版本。

由于这里跟随原始代码更新，可能包含不稳定内容，不建议直接作为日常使用版本。

如果你只是希望日常使用 EasyNote，不打算进行个人开发，建议阅读下方
[Install](#install) 部分并下载 release 版本。

## Preview

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="340" />

## Install

请到发布页面下载对应的安装包：[Release page](https://github.com/je44/easynote/releases)

Go to the [Release page](https://github.com/je44/easynote/releases) to download the corresponding package.

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
