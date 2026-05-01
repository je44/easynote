# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote は Windows 向けの軽量なデスクトップ付箋 / ToDo アプリです。
デスクトップに貼り付くような使い方を想定しており、トレイ操作、
グローバルホットキー、ローカル保存に対応しています。

## プロジェクトについて

`main` ブランチは EasyNote の元になるソースコードを公開する場所です。個人開発者、学習者、自分好みの EasyNote を継続的に作っていきたい人向けです。

これはソースコードの出発点であり、日常利用向けの推奨ダウンロード版ではありません。プロジェクトの更新に合わせて、未完成または不安定な内容を含む場合があります。

個人開発をせずに EasyNote を日常利用したい場合は、下の
[インストール](#インストール) セクションから release 版をダウンロードしてください。

## プレビュー

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="340" />

## インストール

リリースページから対応するパッケージをダウンロードしてください：[Release page](https://github.com/je44/easynote/releases)

Windows x64 / x86 に対応しています。

### どのリリースを選べばよいですか？

| バージョン | 特徴 | リンク |
| --- | --- | --- |
| Stable | 正式版。日常利用に適しています。 | [Release](https://github.com/je44/easynote/releases/latest) |

## 主な機能

- デスクトップ常駐型のノート UI
- 未完了 / 完了ビューの切り替え
- ToDo の追加、ピン留め、完了、復元、削除
- トレイアイコンからのクイック操作
- `Ctrl + Alt + N` で表示 / 非表示
- ToDo の自動保存
- ウィンドウ位置、サイズ、透明度の保存
- `data\` フォルダを使うポータブルモード

## ソースから実行

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

## 内蔵セルフテスト

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test
```

## パッケージ作成

インストーラーとポータブル版を作成:

```powershell
.\build-windows-installer.ps1
```

ポータブル版のみを作成:

```powershell
.\build-windows-installer.ps1 -SkipInstaller
```

## データ保存先

- 通常モード: `%AppData%\easy-note`
- ポータブルモード: アプリフォルダ内の `data\`

## 技術スタック

- WPF
- .NET 10
- `Hardcodet.NotifyIcon.Wpf`
