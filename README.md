# EasyNote

`EasyNote` 是一个面向 Windows 的轻量桌面便签 / 待办工具，适合快速记录待办事项，并以常驻桌面的方式随手使用。

## 软件预览

<img src="docs/images/easynote-preview.png" alt="EasyNote 软件预览图" width="340" />

## 当前支持功能

- 桌面便签式窗口显示
- 待办 / 已办双视图切换
- 新建、完成、恢复待办
- 待办置顶
- 长按删除与二次确认
- 系统托盘常驻
- 开机自启动
- `Ctrl + Alt + N` 全局快捷键显示 / 隐藏窗口
- 自动保存待办数据
- 自动记住窗口位置、尺寸与透明度
- 不依赖 WebView2，目标机器无需额外安装浏览器运行时

## 最近修复更新

- 修复了手动拖拽底部 resize grip 向下调整窗口高度后，窗口长期停留在置顶层的问题。
- 修复了待办录入区域正文首行显示不全的问题，输入文字现在可以完整显示。
- 优化了待办录入区域占位提示的显示体验：当输入框为空且未聚焦时显示提示词；一旦输入框获得焦点或开始输入，提示词立即隐藏。

## 安装方式

### 直接运行源码

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

### 运行内建自测

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test
```

可选地输出到指定文件：

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test --self-test-output=.\.omx\self-test-report.json
```

自测会覆盖并恢复运行期数据文件，用于验证以下功能链路：

- 启动与托盘初始化
- 新增、置顶、完成、恢复、删除待办
- 托盘显示、快捷键显示/隐藏、右上角停靠
- 自动保存、自启动切换、窗口位置/尺寸/透明度持久化

### 生成 Windows 安装包和免安装便携版

```powershell
.\build-windows-installer.ps1
```

默认产物：

- 发布目录：`publish\app`
- 便携版目录：`publish\portable`
- 便携版压缩包：`publish\EasyNote-portable-win-x64.zip`
- 安装包：`installer\EasyNoteSetup.exe`

### 只生成免安装便携版

```powershell
.\build-windows-installer.ps1 -SkipInstaller
```

便携版使用方式：

- 解压 `publish\EasyNote-portable-win-x64.zip`
- 直接运行其中的 `EasyNote.exe`
- 便携版会把待办数据、窗口状态和日志保存到程序目录下的 `data\` 文件夹
- 目标机器不需要预装 WebView2 Runtime

数据目录说明：

- 普通运行：`%AppData%\easy-note`
- 便携版运行：程序目录下的 `data\`

当前安装包行为：

- 默认安装目录：`C:\Program Files\EasyNote`
- 安装时请求管理员权限
- 支持在安装向导中手动修改安装目录
- 发布形态为 `win-x64` 单文件自包含

## 项目结构

- `EasyNote/`：WPF 主程序源码
- `easy-note-wpf.sln`：解决方案入口
- `build-windows-installer.ps1`：发布与安装包构建脚本
- `installer.iss`：Inno Setup 安装器配置

技术栈：

- 桌面端：WPF
- 运行时：.NET 10
- 托盘能力：Hardcodet.NotifyIcon.Wpf
