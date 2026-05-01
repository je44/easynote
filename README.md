# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote is a lightweight desktop sticky-note and todo app for Windows.
It is designed to stay close to the desktop and provide a quick place to
capture tasks with tray controls, a global hotkey, and local persistence.

## Preview

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## About This Version

This `release` branch introduces EasyNote for everyday users. It focuses on
what the app does, what changed recently, and how to download and install it.

If you want to read or customize the original source code, use the `main`
branch. If you just want to use EasyNote, download the release package below.

## Features

- Desktop-attached note board UI
- Pending / completed views
- Add, pin, complete, restore, and delete todos
- Tray icon with quick actions
- `Ctrl + Alt + N` global show / hide hotkey
- Auto-save for todos
- Window position, size, and opacity persistence
- Optional portable mode with local `data/` storage

## Recent Updates

- Improved the daily note and todo experience.
- Refined add, edit, delete, archive, and restore flows.
- Improved window display, saved position, and size behavior.
- Refined theme display and interface details.
- Improved local data saving so notes and todos remain available next time.
- Added Windows x64 and x86 downloads, with installer and portable options.

## Install

Download EasyNote for Windows directly from GitHub Releases:

- [Windows x64 installer](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x64.zip)
- [Windows x86 installer](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x86-setup.exe)
- [Windows x86 portable ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x86.zip)

For the installer version, run the downloaded setup EXE. For the portable
version, extract the ZIP and run `EasyNote.exe`.

## Data Location

- standard mode: `%AppData%\easy-note`
- portable mode: `data\` under the app folder
