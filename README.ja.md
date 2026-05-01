# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote は Windows 向けの軽量なデスクトップ付箋 / ToDo アプリです。
デスクトップに貼り付くような使い方を想定しており、トレイ操作、
グローバルホットキー、ローカル保存に対応しています。

## プレビュー

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## このバージョンについて

`release` ブランチは、一般ユーザー向けに EasyNote を紹介する場所です。アプリでできること、最近の更新内容、ダウンロードとインストール方法を中心にまとめています。

元のソースコードを読みたい、または自分向けに変更したい場合は `main` ブランチを使ってください。EasyNote をそのまま使いたい場合は、下の release 版をダウンロードしてください。

## 主な機能

- デスクトップ常駐型のノート UI
- 未完了 / 完了ビューの切り替え
- ToDo の追加、ピン留め、完了、復元、削除
- トレイアイコンからのクイック操作
- `Ctrl + Alt + N` で表示 / 非表示
- ToDo の自動保存
- ウィンドウ位置、サイズ、透明度の保存
- `data\` フォルダを使うポータブルモード

## 最近の更新

- デスクトップノートと ToDo の日常利用体験を改善しました。
- ToDo の追加、編集、削除、アーカイブ、復元の流れを整えました。
- ウィンドウ表示、位置保存、サイズ調整の動作を改善しました。
- テーマ表示と画面操作の細部を調整しました。
- ローカル保存を改善し、次回起動時にも内容を続けて使いやすくしました。
- Windows x64 / x86 向けに、インストーラー版とポータブル版を用意しました。

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
