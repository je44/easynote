# EasyNote

`EasyNote` 是一个面向 Windows 的桌面便签 / 待办工具，适合用来快速记录待办事项，并以轻量常驻的方式放在桌面上使用。

## 软件预览

<img src="docs/images/easynote-preview.png" alt="EasyNote 软件预览图" width="340" />

## 当前支持功能

- 桌面便签式窗口显示
- 待办 / 已完成双视图切换
- 新建、完成、恢复待办
- 待办置顶
- 长按删除与二次确认
- 系统托盘驻留
- 开机自启动
- `Ctrl + Alt + N` 全局快捷键显示 / 隐藏窗口
- 自动保存待办数据
- 自动记住窗口位置、尺寸与透明度

## 安装方式

### 直接运行源码

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

### 生成 Windows 安装包

```powershell
.\build-windows-installer.ps1
```

默认产物：

- 发布目录：`publish\app`
- 安装包：`installer\EasyNoteSetup.exe`

当前安装包行为：

- 默认安装目录：`C:\Program Files\EasyNote`
- 安装时请求管理员权限
- 支持在安装向导中手动修改安装目录
- 发布形态为 `win-x64` 单文件自包含

## 项目架构

- `EasyNote/`：WPF 主程序源码
- `EasyNote/wwwroot/`：前端静态资源
- `easy-note-wpf.sln`：解决方案入口
- `build-windows-installer.ps1`：发布与安装包构建脚本
- `installer.iss`：Inno Setup 安装器配置

技术结构：

- 桌面端：WPF
- 运行时：.NET 10
- Web 容器：WebView2
- 托盘能力：Hardcodet.NotifyIcon.Wpf
