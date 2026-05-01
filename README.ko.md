# EasyNote

[English](README.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

EasyNote는 Windows용 경량 데스크톱 메모 / 할 일 앱입니다.
데스크톱에 붙여 두고 빠르게 쓰는 흐름에 맞춰 설계되었으며,
트레이 제어, 전역 단축키, 로컬 저장을 지원합니다.
WebView2는 필요하지 않습니다.

## 프로젝트 안내

`main` 브랜치는 EasyNote의 원본 오픈 소스 코드입니다. 코드를 읽고,
수정하며, 자신에게 맞는 EasyNote 버전을 계속 만들어 가려는 사람에게 적합합니다.

이 브랜치는 소스 코드 변경을 그대로 따라가므로, 완성되지 않았거나 불안정한 내용이 포함될 수 있습니다. 일상 사용 버전으로는 권장하지 않습니다.

개인 개발 없이 EasyNote를 일상적으로 사용하려면 아래
[설치](#설치) 섹션에서 release 버전을 다운로드하세요.

## 미리보기

<img src="docs/images/easynote-preview.png" alt="EasyNote preview" width="340" />

## 설치

릴리스 페이지에서 알맞은 패키지를 다운로드하세요: [Release page](https://github.com/je44/easynote/releases)

Windows x64 및 x86을 지원합니다.

### 어떤 릴리스를 선택해야 하나요?

| 버전 | 특징 | 링크 |
| --- | --- | --- |
| Stable | 공식 릴리스로, 일상적인 사용에 적합합니다. | [Release](https://github.com/je44/easynote/releases/latest) |

## 주요 기능

- 데스크톱 밀착형 노트 UI
- 할 일 / 완료 보기 전환
- 항목 추가, 고정, 완료, 복원, 삭제
- 트레이 아이콘 빠른 동작
- `Ctrl + Alt + N` 전역 표시 / 숨기기
- 자동 저장
- 창 위치, 크기, 투명도 유지
- `data\` 폴더 기반 포터블 모드

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
