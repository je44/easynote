# EasyNote

`EasyNote` 是一个面向 Windows 的轻量桌面便签 / 待办工具，适合快速记录待办事项，并以常驻桌面的方式随手使用。

## Languages

### English

EasyNote is a lightweight desktop sticky-note and todo app for Windows.
It stays close to the desktop, supports tray control, global hotkeys,
portable mode, and local data persistence without requiring WebView2.

Key features:

- Pending / completed views
- Add, pin, complete, restore, and delete todos
- Tray icon with quick actions
- `Ctrl + Alt + N` global show / hide hotkey
- Auto-save for todos
- Window position, size, and opacity persistence
- Optional portable mode with local `data/` storage

Run from source:

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

Run the built-in self-test:

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test
```

### 日本語

EasyNote は Windows 向けの軽量なデスクトップ付箋 / ToDo アプリです。
デスクトップ常駐型の使い方を想定しており、トレイ操作、グローバル
ショートカット、ポータブル実行、ローカル保存に対応しています。
WebView2 は不要です。

主な機能:

- 未完了 / 完了ビューの切り替え
- ToDo の追加、ピン留め、完了、復元、削除
- トレイアイコンからの操作
- `Ctrl + Alt + N` による表示 / 非表示
- ToDo の自動保存
- ウィンドウ位置、サイズ、透明度の保存
- `data/` フォルダを使うポータブル版

### 한국어

EasyNote는 Windows용 경량 데스크톱 메모 / 할 일 앱입니다.
데스크톱에 붙여 두고 빠르게 사용하는 흐름에 맞춰져 있으며,
트레이 제어, 전역 단축키, 포터블 실행, 로컬 데이터 저장을 지원합니다.
WebView2는 필요하지 않습니다.

주요 기능:

- 할 일 / 완료 보기 전환
- 항목 추가, 고정, 완료, 복원, 삭제
- 트레이 아이콘 빠른 동작
- `Ctrl + Alt + N` 전역 표시 / 숨기기
- 할 일 자동 저장
- 창 위치, 크기, 투명도 유지
- `data/` 폴더를 사용하는 포터블 모드

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
