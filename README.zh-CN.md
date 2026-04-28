# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote 是一个面向 Windows 的轻量桌面便签 / 待办工具。
它以贴近桌面的方式常驻使用，支持托盘控制、全局快捷键和本地数据持久化，
不依赖 WebView2。

## 预览

<img src="docs/images/easynote-preview.png" alt="EasyNote 预览图" width="340" />

## 安装

请到发布页面下载对应的安装包：[Release page](https://github.com/je44/easynote/releases)

支持 Windows x64 和 x86。

### 我应该选择哪个版本？

| 版本 | 特点 | 链接 |
| --- | --- | --- |
| Stable | 正式版，适合日常使用。 | [Release](https://github.com/je44/easynote/releases/latest) |

## 功能

- 桌面贴靠式便签界面
- 待办 / 已办视图切换
- 新增、置顶、完成、恢复、删除待办
- 托盘图标快捷操作
- `Ctrl + Alt + N` 全局显示 / 隐藏快捷键
- 待办自动保存
- 窗口位置、尺寸、透明度持久化
- 可选便携模式，本地 `data\` 目录存储

## 从源码运行

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

## 内建自测

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test
```

可选自定义报告路径：

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test --self-test-output=.\.omx\self-test-report.json
```

自测会验证：

- 启动与托盘初始化
- 新增 / 置顶 / 完成 / 恢复 / 删除待办流程
- 托盘显示、快捷键显示/隐藏、右上角停靠
- 自动保存、自启动切换、窗口状态持久化

## 打包

生成安装包和便携版：

```powershell
.\build-windows-installer.ps1
```

默认产物：

- 发布目录：`publish\app`
- 便携版目录：`publish\portable`
- 便携版压缩包：`publish\EasyNote-portable-win-x64.zip`
- 安装包：`installer\EasyNoteSetup.exe`

只生成便携版：

```powershell
.\build-windows-installer.ps1 -SkipInstaller
```

便携版行为：

- 解压 `publish\EasyNote-portable-win-x64.zip`
- 运行 `EasyNote.exe`
- 数据、窗口状态、日志会保存到程序目录下的 `data\`
- 不需要 WebView2 Runtime

## 数据目录

- 普通模式：`%AppData%\easy-note`
- 便携模式：程序目录下的 `data\`

## 项目结构

- `EasyNote/`：WPF 主程序源码
- `easy-note-wpf.sln`：解决方案入口
- `build-windows-installer.ps1`：发布和打包脚本
- `installer.iss`：Inno Setup 安装器配置

## 技术栈

- WPF
- .NET 10
- `Hardcodet.NotifyIcon.Wpf`
