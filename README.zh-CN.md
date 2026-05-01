# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote 是一个面向 Windows 的轻量桌面便签 / 待办工具。
它以贴近桌面的方式常驻使用，支持托盘控制、全局快捷键和本地数据持久化，
不依赖 WebView2。

## 预览

<img src="docs/images/easynote-preview.png" alt="EasyNote 预览图" width="409" />

## 本版本说明

此分支是推荐日常使用的版本。如果你只是想直接使用 EasyNote，不打算进行个人开发，请在下方
[安装](#安装) 部分下载安装版或免安装版。

`main` 分支保留为 EasyNote 的原始开源代码，适合查看源码、修改和构建自己的版本。日常使用建议选择
release 下载。

## 功能

- 桌面贴靠式便签界面
- 待办 / 已办视图切换
- 新增、置顶、完成、恢复、删除待办
- 托盘图标快捷操作
- `Ctrl + Alt + N` 全局显示 / 隐藏快捷键
- 待办自动保存
- 窗口位置、尺寸、透明度持久化
- 可选便携模式，本地 `data\` 目录存储

## 安装

可直接从 GitHub Releases 下载 Windows 版本：

- [Windows x64 安装版](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x64-setup.exe)
- [Windows x64 免安装版 ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x64.zip)
- [Windows x86 安装版](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x86-setup.exe)
- [Windows x86 免安装版 ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x86.zip)

安装版请运行下载的安装程序。免安装版请解压 ZIP 后运行 `EasyNote.exe`。

## 数据目录

- 普通模式：`%AppData%\easy-note`
- 便携模式：程序目录下的 `data\`
