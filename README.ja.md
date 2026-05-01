# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote は Windows 向けの軽量なデスクトップ付箋 / ToDo アプリです。
デスクトップに貼り付くような使い方を想定しており、トレイ操作、
グローバルホットキー、ローカル保存に対応しています。
WebView2 は不要です。

## プレビュー

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## このバージョンについて

このブランチは日常利用におすすめのバージョンです。個人開発をせずに EasyNote をそのまま使いたい場合は、下の
[インストール](#インストール) セクションからインストーラーまたはポータブル版をダウンロードしてください。

`main` ブランチは EasyNote の元になるオープンソースコードとして残しています。コードを読み、変更し、自分向けのバージョンを作りたい場合に使ってください。通常利用には release 版のダウンロードをおすすめします。

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

Windows 版は GitHub Releases から直接ダウンロードできます。

- [Windows x64 インストーラー](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x64-setup.exe)
- [Windows x64 ポータブル ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x64.zip)
- [Windows x86 インストーラー](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x86-setup.exe)
- [Windows x86 ポータブル ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x86.zip)

インストーラー版は、ダウンロードしたセットアップファイルを実行してください。ポータブル版は ZIP を展開して `EasyNote.exe` を実行してください。

## データ保存先

- 通常モード: `%AppData%\easy-note`
- ポータブルモード: アプリフォルダ内の `data\`
