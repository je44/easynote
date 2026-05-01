# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote is a lightweight desktop sticky-note and todo app for Windows.
It is designed to stay close to the desktop and provide a quick place to
capture tasks with tray controls, a global hotkey, and local persistence.
It does not require WebView2.

## Preview

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## Release Version

This branch is the recommended version for daily use. Download the installer or
portable package from the [Install](#install) section if you want to use
EasyNote directly without personal development work.

The `main` branch is kept as the original open-source code for people who want
to read, change, or create their own version of EasyNote. For normal use,
choose the release download instead.

## 本版本说明

此分支是推荐日常使用的版本。如果你只是想直接使用 EasyNote，不打算进行个人开发，请在下方
[Install](#install) 部分下载安装版或免安装版。

`main` 分支保留为 EasyNote 的原始开源代码，适合查看源码、修改和构建自己的版本。日常使用建议选择
release 下载。

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

Download EasyNote for Windows directly from GitHub Releases:

- [Windows x64 installer](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x64-setup.exe)
- [Windows x64 portable ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x64.zip)
- [Windows x86 installer](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x86-setup.exe)
- [Windows x86 portable ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x86.zip)

For the installer version, run the downloaded setup EXE. For the portable
version, extract the ZIP and run `EasyNote.exe`. This version is published from
the `release` branch.

## Data Location

- standard mode: `%AppData%\easy-note`
- portable mode: `data\` under the app folder
