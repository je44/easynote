# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote は Windows 向けの軽量なデスクトップ付箋 / ToDo アプリです。
デスクトップに貼り付くような使い方を想定しており、トレイ操作、
グローバルホットキー、ローカル保存に対応しています。
WebView2 は不要です。

## プレビュー

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## Release ブランチについて

この `release` ブランチは EasyNote のダウンロード可能な Windows ビルドです。
`main` ブランチは、他のユーザーや開発者が使う公開ベースラインとして残しています。

`main` と比べて、このブランチには現在以下の変更が含まれます。

- より広い個人向け WPF UI と、やわらかいベージュ系の見た目
- プレビューに合わせた Todo / Done ナビゲーションと空状態レイアウト
- ToDo 追加用のインライン下書き入力
- ピン留め項目の表示強化と、項目操作状態の調整
- 日中 / 夜間の目にやさしいテーマ切り替えと、その状態保存
- ポータブルモードで既存 AppData の ToDo とウィンドウ状態を移行する処理
- このブランチ専用の README プレビュー画像

共有開発には `main` を使い、配布版の個人向け UI と操作フローを使う場合は `release` を使ってください。

## 主な機能

- デスクトップ常駐型のノート UI
- 未完了 / 完了ビューの切り替え
- ToDo の追加、ピン留め、完了、復元、削除
- トレイアイコンからのクイック操作
- `Ctrl + Alt + N` で表示 / 非表示
- ToDo の自動保存
- ウィンドウ位置、サイズ、透明度の保存
- `data\` フォルダを使うポータブルモード

## インストール

release 版の Windows 実行ファイルは GitHub Releases から直接ダウンロードできます。

- release 版: [EasyNote.exe](https://github.com/je44/easynote/releases/download/release-2026-04-26/EasyNote.exe)

`main` ブランチはオープンソースのコードベースとして維持し、EXE ダウンロードは提供しません。
`main` を使う場合は、下記のソース実行手順に従ってください。

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
