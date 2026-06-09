# UI 골격 씬 배선 가이드

`feat/ui-skeleton` 브랜치에서 추가한 화면 골격 코드를 Unity 씬에 연결하는 절차다.
**비주얼(배경 PNG·버튼 스프라이트·레이아웃)은 디자인 수령 후** 각 패널 내부에 채운다.
이 문서는 코드 ↔ 씬 연결만 다룬다.

## 스크립트 구성 (`Assets/_Project/Scripts/`)

```
App/
  AppBootstrap.cs       - 진입점: 스플래시 → 권한 확인 → 홈
  SessionController.cs  - 세션 시작/종료/발화 누적, 파이프라인 기동·정지 이벤트
Data/
  AppSettings.cs        - PlayerPrefs 설정(임계값·TTS·Gemini·환각통제·동의)
  ClassSession.cs       - 수업 세션 모델
  UtteranceEntry.cs     - 발화(원본 수어단어 + Gemini 문장 + 미인식 플래그)
  SessionStore.cs       - 세션 JSON 영속화(persistentDataPath/sessions.json)
UI/
  AppScreen.cs          - 화면 enum
  UIScreen.cs           - 화면 베이스(OnShow/OnHide)
  ScreenManager.cs      - 패널 전환·백스택
  Screens/              - 7개 화면 컨트롤러
```

## 씬 배선 절차

> MCP Unity로 조작하기 전 **에디터에서 수동 `Ctrl+R`로 컴파일 동기화**(Auto Refresh 꺼짐, CLAUDE.md).

1. **부트스트랩 오브젝트** 생성 (빈 GameObject, 예: `_App`)
   - `ScreenManager`, `SessionController`, `AppBootstrap` 컴포넌트 부착
2. **Canvas** 생성 → 그 아래 화면별 빈 패널 7개 생성
   - 각 패널에 대응 컨트롤러 부착(`SplashScreen`, `CameraPermissionScreen`,
     `HomeScreen`, `RecognitionScreen`, `SessionLogScreen`, `AllLogsScreen`, `SettingsScreen`)
   - 각 컨트롤러의 `Screen` 필드를 해당 enum 값으로 설정
3. **ScreenManager.`_screens`** 리스트에 패널 7개를 모두 등록
4. 버튼/슬라이더/토글은 **PNG 디자인대로 배치한 뒤** 각 컨트롤러의 `[SerializeField]` 필드에 연결
   - 미연결 상태에서도 null 가드로 컴파일·실행은 됨(해당 버튼만 동작 안 함)

## 화면 전환 흐름 (plan §7)

```
스플래시 → (카메라 권한) → 홈 ─┬─ [수업 시작] → 수업 인식 ─┬─ 세션 로그
                              ├─ 전체 수업 로그            └─ 종료 → 홈
                              └─ 설정
```

## 다음 단계 (이 골격 위에 쌓을 것)

- **PNG 디자인 수령 후**: 각 패널 내부 비주얼 구성 + SerializeField 연결
- **파이프라인 단계**: `SessionController.SessionStarted/Ended`를 구독하는
  카메라→MediaPipe(137점)→27점 매핑→정규화→Sentis 추론→버퍼→Gemini→TTS 흐름
  (전처리 사양은 ML 산출물 메모 참조)
- **AR 단계**: `RecognitionScreen`에 AR Foundation 카메라 피드 + 월드 말풍선 연동

## 스플래시 화면 구성 스펙 (61.png)

> **자동 빌더 있음**: 메뉴 `Artti > Build Splash Screen`(`Assets/_Project/Editor/SplashSceneBuilder.cs`)이
> 아래 스펙대로 Canvas·부트스트랩(_App)·SplashScreen 패널을 생성하고 SerializeField까지 배선한다.
> 재실행 안전(기존 스플래시 패널 제거 후 재생성). 레이아웃 수정은 그 .cs에서 하고 메뉴 재실행.
> 실행 후 씬 저장(Ctrl+S) 필요. 아래 표는 빌더가 따르는 원본 스펙.

> 폰트는 프로젝트에 이미 있는 `Assets/_Project/Fonts/NotoSansKR-Bold SDF`,
> `NotoSansKR-Regular SDF` TMP 에셋 사용. **재베이킹 금지**(전체 네모 깨짐 이력).

**Canvas**: Screen Space - Overlay · CanvasScaler = Scale With Screen Size,
Reference 1080×1920(portrait), Match 0.5

```
SplashScreen (Image 배경 #FFFFFF, +SplashScreen.cs, Screen=Splash)
├─ TitleGroup (화면 중앙 약간 위)
│  ├─ "SIGNBRIDGE"        TMP Bold,    #3E4E6E, ~64, 자간 약간
│  └─ "실시간 수어 통역 도우미"  TMP Regular, #7E8795, ~22
└─ BottomGroup (하단)
   ├─ "수어 인식 모델 준비 중"  TMP Regular, #9AA2AE, ~15   → _statusText 에 연결
   ├─ ProgressTrack (Image, #E4E7EC, 둥근 스프라이트, H~10, W~600)
   │  └─ ProgressFill (Image Type=Filled·Horizontal·Left, #4C8DE0, 둥근) → _progressFill 에 연결
   └─ "team ARTTI"        TMP Bold,    #3E4E6E, ~20
```

색상은 PNG에서 추출한 근사값 — 실제 디자인 토큰 있으면 교체.

**인스펙터 연결**
- `SplashScreen._progressFill` = ProgressFill
- `SplashScreen._statusText` = Status 텍스트
- `SplashScreen._screen` = Splash
- `ScreenManager._screens` 리스트에 SplashScreen 추가
- 진행바·문구 갱신은 `AppBootstrap`이 구동(시간 기반 시뮬 → 추후 실제 모델 로드 진행도로 교체)

> 화면 방향: 스플래시가 portrait이므로 Player Settings → Resolution and Presentation의
> Default Orientation을 Portrait(또는 Auto Rotation 제한)로 맞출 것.

## 환경 설정 화면 구성 스펙 (90·91.png)

> **자동 빌더 있음**: 메뉴 `Artti > Build Settings Screen`(`Assets/_Project/Editor/SettingsSceneBuilder.cs`)이
> 아래 화면을 생성하고 SerializeField까지 배선한다. 재실행 안전. 실행 후 씬 저장(Ctrl+S) 필요.

**단일 화면** — 각 설정마다 페이지 전환 없음. 연회색 배경(#F5F6F8) 위 흰 카드.

```
SettingsScreen (Screen=Settings, +SettingsScreen.cs)
├─ BackButton (원형 흰 버튼 + arrow_back)     → _backButton (백스택 Back)
├─ "환경 설정"  TMP Bold, Brand
├─ [음성 설정]
│  └─ VoiceCard
│     ├─ "음성 출력 속도"
│     ├─ TtsRateSlider (정수 5 tick)           → _ttsRateSlider  (AppSettings.TtsRate)
│     └─ TtsTestButton: volume_up + 샘플문장   → _ttsTestButton / _ttsSpeakerIcon
│        (테스트 중 스피커 아이콘 primary 점등)
├─ [데이터 · 개인정보]
│  └─ DataCard: 설명 + "전체 기록 삭제"(앰버)  → _deleteAllButton → 삭제 확인 모달
├─ InquiryButton "팀 ARTTI에 문의하기"(mailto) → _inquiryButton
└─ DeleteModal (숨김, 91.png)
   └─ 카드: 메시지 + 취소/확인 + X            → _deleteModal/_deleteConfirmButton/_deleteCancelButton/_deleteCloseButton
```

- **슬라이더 tick**: `SettingsScreen.TtsRateStops = {0.75, 1.0, 1.25, 1.5, 2.0}` 배율(임의값, 코드에서 조정).
- **TTS 테스트**: `TtsService`(네이티브 Android `TextToSpeech` 래핑)로 샘플 문장을 실제 발화.
  발화 시작/종료 이벤트로 스피커 아이콘이 primary 점등. 속도/볼륨은 `AppSettings.TtsRate/TtsVolume` 반영.
  에디터/비안드로이드에서는 음성 없이 점등 시뮬로 폴백.
  - `TtsService`는 `_App`(부트스트랩)에 컴포넌트로 부착(`SbUiBuild.EnsureBootstrap`이 보장).
    파이프라인의 Gemini 변환 문장도 `TtsService.Instance.Speak(...)`로 동일하게 출력하면 됨.
- **전체 기록 삭제**: 확인 모달 "확인" → `SessionStore.ClearAll()`(로컬 sessions.json 제거, 되돌릴 수 없음).
- **문의하기**: `mailto:` URL(수신자·제목·본문 자동 입력)로 OS 기본 메일 작성 화면 호출. 앱이 직접 전송하지 않음.
  수신처 `SettingsScreen.InquiryEmail` 상수는 팀 공용 주소로 교체할 것.

## 메모

- **UniTask 미설치** — CLAUDE.md 표준은 UniTask이나 현재 manifest에 없음.
  골격은 UniTask 없이 작성(코루틴 사용). 파이프라인 async 도입 시 설치 여부 결정 필요.
- asmdef 없음 → 전부 `Assembly-CSharp`로 컴파일.
