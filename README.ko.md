# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote는 Windows용 경량 데스크톱 메모 / 할 일 앱입니다.
데스크톱에 붙여 두고 빠르게 쓰는 흐름에 맞춰 설계되었으며,
트레이 제어, 전역 단축키, 로컬 저장을 지원합니다.
WebView2는 필요하지 않습니다.

## 미리보기

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## 이 버전 안내

이 브랜치는 일상 사용에 권장되는 버전입니다. 개인 개발 없이 EasyNote를 바로 사용하려면 아래
[설치](#설치) 섹션에서 설치 버전 또는 포터블 버전을 다운로드하세요.

`main` 브랜치는 EasyNote의 원본 오픈 소스 코드로 유지됩니다. 코드를 읽고, 수정하고, 자신만의 버전을 만들고 싶을 때 사용하세요. 일반 사용에는 release 다운로드를 권장합니다.

## 주요 기능

- 데스크톱 밀착형 노트 UI
- 할 일 / 완료 보기 전환
- 항목 추가, 고정, 완료, 복원, 삭제
- 트레이 아이콘 빠른 동작
- `Ctrl + Alt + N` 전역 표시 / 숨기기
- 자동 저장
- 창 위치, 크기, 투명도 유지
- `data\` 폴더 기반 포터블 모드

## 설치

Windows 버전은 GitHub Releases에서 바로 다운로드할 수 있습니다.

- [Windows x64 설치 버전](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x64-setup.exe)
- [Windows x64 포터블 ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x64.zip)
- [Windows x86 설치 버전](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-win-x86-setup.exe)
- [Windows x86 포터블 ZIP](https://github.com/je44/easynote/releases/download/v1.0/EasyNote-v1.0-portable-win-x86.zip)

설치 버전은 다운로드한 설치 파일을 실행하세요. 포터블 버전은 ZIP을 압축 해제한 뒤 `EasyNote.exe`를 실행하세요.

## 데이터 위치

- 일반 모드: `%AppData%\easy-note`
- 포터블 모드: 앱 폴더 아래 `data\`
