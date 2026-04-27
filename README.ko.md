# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote는 Windows용 경량 데스크톱 메모 / 할 일 앱입니다.
데스크톱에 붙여 두고 빠르게 쓰는 흐름에 맞춰 설계되었으며,
트레이 제어, 전역 단축키, 로컬 저장을 지원합니다.
WebView2는 필요하지 않습니다.

## 미리보기

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="409" />

## Release 브랜치 안내

이 `release` 브랜치는 EasyNote의 다운로드 가능한 Windows 빌드입니다.
`main` 브랜치는 다른 사용자와 개발자가 사용할 공개 기준선으로 유지합니다.

`main`과 비교해 현재 이 브랜치에는 다음 변경이 포함되어 있습니다.

- 더 넓은 개인 맞춤 WPF UI와 부드러운 베이지 계열 스타일
- 미리보기와 맞춘 Todo / Done 전환 및 빈 상태 레이아웃
- 할 일 추가를 위한 인라인 초안 입력 흐름
- 고정 항목 스타일과 세부 항목 동작 상태 개선
- 주간 / 야간 눈 보호 테마 전환 및 창 상태와 함께 저장
- 포터블 모드에서 기존 AppData 할 일과 창 상태를 옮기는 마이그레이션
- 이 브랜치 전용 README 미리보기 이미지

공개 협업과 기준 개발에는 `main`을 사용하고, 배포용 개인 맞춤 UI와 작업 흐름은 `release`를 사용하세요.

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

release 버전 Windows 실행 파일은 GitHub Releases에서 바로 다운로드할 수 있습니다.

- release 버전: [EasyNote.exe](https://github.com/je44/easynote/releases/download/release-2026-04-26/EasyNote.exe)

`main` 브랜치는 오픈 소스 코드 기준선으로만 유지하며 EXE 다운로드는 제공하지 않습니다.
`main`을 사용할 때는 아래 소스 실행 방법에 따라 빌드하세요.

## 소스에서 실행

```powershell
dotnet restore .\easy-note-wpf.sln
dotnet run --project .\EasyNote\EasyNote.csproj
```

## 내장 셀프 테스트

```powershell
dotnet run --project .\EasyNote\EasyNote.csproj -- --self-test
```

## 패키징

설치 파일과 포터블 패키지 생성:

```powershell
.\build-windows-installer.ps1
```

포터블 패키지만 생성:

```powershell
.\build-windows-installer.ps1 -SkipInstaller
```

## 데이터 위치

- 일반 모드: `%AppData%\easy-note`
- 포터블 모드: 앱 폴더 아래 `data\`

## 기술 스택

- WPF
- .NET 10
- `Hardcodet.NotifyIcon.Wpf`
