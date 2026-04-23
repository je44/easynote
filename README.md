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

## 最近修复更新

- 修复了手动拖拽底部 `resize grip` 向下调整窗口高度后，窗口长期停留在置顶层的问题。现在在手动底部 resize 结束后，窗口会按原有桌面模式逻辑正确回到桌面层级，不再需要先隐藏再显示才能恢复。
- 修复了待办录入区域正文首行显示不全的问题，输入文字现在可以完整显示。
- 优化了待办录入区域占位提示的显示策略：当输入框为空且未聚焦时显示提示词；一旦输入框获得焦点或开始输入，提示词立即隐藏，以避免提示词与光标轻微错位对使用造成干扰。

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

## 项目结构

- `EasyNote/`：WPF 主程序源码
- `EasyNote/wwwroot/`：前端静态资源
- `easy-note-wpf.sln`：解决方案入口
- `build-windows-installer.ps1`：发布与安装包构建脚本
- `installer.iss`：Inno Setup 安装器配置

技术栈：

- 桌面端：WPF
- 运行时：.NET 10
- Web 容器：WebView2
- 托盘能力：Hardcodet.NotifyIcon.Wpf
