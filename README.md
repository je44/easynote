# EasyNote

`EasyNote` 是一个面向 Windows 的轻量级桌面便签 / 待办应用。

当前仓库直接以现有最稳定、功能最完整的主线版本作为正式发布版本。

项目现已开源，欢迎继续完善 UI、交互、安装体验和稳定性问题。

## 当前状态

- 当前正式版：基于当前主线发布
- 技术栈：WPF + .NET 10 + WebView2 + 托盘菜单
- 运行平台：Windows
- 维护策略：先公开当前最好用的版本，再逐步收敛并优化正式版工程质量

## 功能简介

- 桌面便签式窗口，可嵌入桌面层显示
- 待办 / 已完成双视图切换
- 新建待办、完成待办、恢复待办
- 待办置顶
- 长按后删除，并带二次确认
- 系统托盘驻留
- 开机自启动
- `Ctrl + Alt + N` 全局快捷键显示 / 隐藏窗口
- 自动保存待办数据
- 自动记住窗口位置、尺寸与透明度
- 提供运行日志，便于继续排查桌面嵌入相关问题

## 安装方式

### 方式一：直接运行当前正式版源码

适合开发者和希望直接体验最新主线版本的用户。

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

### 方式二：生成可分发的正式版目录

```powershell
.\build-windows-installer.ps1
```

脚本会自动生成发布目录，并在可用的打包环境下输出 Windows EXE。

### 方式三：打包 Windows EXE

使用项目自带脚本可以直接打包 Windows EXE：

```powershell
.\build-windows-installer.ps1
```

默认会构建当前正式版，并输出：

- 发布目录：`publish\`
- 可执行安装文件：`installer\EasyNoteSetup.exe`

如果你想手动构建基于 `Release` 配置的产物，也可以执行：

```powershell
.\build-windows-installer.ps1 -Configuration Release
```

## 开发环境

- Windows 10 / 11
- .NET SDK 10

## 仓库结构

- `EasyNote/`：应用源码
- `EasyNote/wwwroot/`：当前正式版使用的前端静态资源
- `easy-note-wpf.sln`：Visual Studio 解决方案
- `build-windows-installer.ps1`：发布与 EXE 打包脚本
- `installer.iss`：EXE 打包配置文件

## 本地数据位置

应用运行后会在本机保存以下数据：

- 待办数据：`%AppData%\easy-note\todos.json`
- 窗口状态：`%AppData%\easy-note\window-state.json`

开机自启注册表位置：

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- 键名：`DesktopMemoBoard`

## 贡献方式

欢迎提 Issue 和 Pull Request，一起把这个项目继续打磨下去。

提交前建议至少完成以下检查：

```powershell
dotnet build .\easy-note-wpf.sln
```

如果改动涉及安装包脚本，再额外运行：

```powershell
.\build-windows-installer.ps1
```

更详细的协作约定请见 [CONTRIBUTING.md](./CONTRIBUTING.md)。

## 开源协议

本项目采用 [MIT License](./LICENSE)。

## 资源与说明

- 托盘功能依赖 `Hardcodet.NotifyIcon.Wpf`
- Web 内容承载依赖 `Microsoft.Web.WebView2`
- `EasyNote/Assets/Fonts/Newsreader.ttf` 附带 OFL 许可文本
- `EasyNote/wwwroot/fonts/` 中保留了当前正式版所使用的字体资源，后续如要做更大范围二次分发，建议再次核对对应字体授权状态
