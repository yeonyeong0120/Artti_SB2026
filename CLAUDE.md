# Artti_SB2026 - SignBridge

통합학급 농인 학생을 위한 실시간 수어 통역 AR 앱. 팀 아르띠(Artti) 3인 프로젝트.

자세한 기획은 저장소 루트의 기획서 Artti_SB2026\docs\plan.md 참조. 본 문서는 Claude Code가 매 세션 자동으로 읽는 운영 가이드라인이다.

---

## 개발 환경 (절대 변경 금지)

| 항목 | 값 |
| --- | --- |
| Unity 에디터 | **6000.3.14f1 (Unity 6.3 LTS)** |
| 템플릿 | AR Mobile v2.1.2 (URP 포함) |
| 타깃 플랫폼 | **Android** (태블릿 우선) |
| Bundle Identifier | `com.artti.signbridge` |
| 렌더 파이프라인 | URP |

---

## 핵심 패키지

| 패키지 | 버전 | 비고 |
| --- | --- | --- |
| AR Foundation / ARCore / ARKit | 6.x.x | 템플릿 포함 |
| Universal RP | 17.x.x | 템플릿 포함 |
| **Sentis (Inference Engine)** | **2.6.x** | SL-GCN ONNX 추론 |
| **com.unity.ugui** | **1.0.0** | MediaPipe 호환성 위해 강제 다운그레이드 |
| **MediaPipe Unity Plugin (homuler)** | **v0.16.3** | 본격 코드 작성 직전 unitypackage 임포트 |
| Newtonsoft Json | 내장 | Gemini API 응답 파싱 |

### 패키지 주의사항

- **UGUI 1.0.0 고정**: Unity 6.3 기본 UGUI 2.0이 MediaPipe와 컴파일 충돌 (`WorldDocumentRaycaster` 에러). 1.0.0으로 다운그레이드해야 동작. `Packages/manifest.json` 직접 명시됨.
- **MediaPipe는 추후 설치**: 매우 무거워 셋업 단계 컴파일 부담 회피.

---

## 필수 운영 설정

| 항목 | 값 | 위치 |
| --- | --- | --- |
| **Active Input Handling** | **Both** | Player Settings → Other Settings (MediaPipe 호환성 필수) |
| **Auto Refresh** | **Disabled** | Preferences → Asset Pipeline (MCP 사용 필수) |
| **When entering Play Mode** | **Do not reload Domain or Scene** | Editor Settings (MCP 사용 필수) |
| Scripting Backend | IL2CPP | Player Settings |
| Target Architecture (Android) | ARM64 only | Player Settings |
| Color Space | Linear | Player Settings |

### Reload Domain 비활성화 부작용

`static` 변수가 Play 종료 후 초기화되지 않음. 모든 `static`은 `Awake()`에서 명시적 초기화 필수.

---

## 폴더 구조 규칙

우리 자산은 모두 `Assets/_Project/` 아래 격리. 외부 패키지(`Assets/MediaPipeUnity/`, `Assets/Samples/`)와 분리.

```
Assets/_Project/
├── Scenes/       - 우리 씬
├── Scripts/      - C# 스크립트
│   ├── Pipeline/ - 카메라→키포인트→추론→문장변환→TTS 메인 흐름
│   ├── UI/       - 화면 UI, AR 말풍선
│   └── Utils/    - 유틸
├── Models/       - ONNX 파일
├── Prefabs/
├── Materials/
├── Textures/
└── Resources/
```

- 새 스크립트는 절대 `Assets/` 루트에 두지 말 것
- `_Project` 앞 언더스코어로 Project 창 최상단 정렬

---

## 코드 컨벤션

### 네임스페이스
모든 코드는 `Artti.SignBridge.{서브도메인}` 아래.
```csharp
namespace Artti.SignBridge.Pipeline { ... }
namespace Artti.SignBridge.UI { ... }
namespace Artti.SignBridge.Utils { ... }
```

### 언어
- 코드 주석/XML doc: 한국어 OK
- 변수명/함수명: 영어 (PascalCase / camelCase)
- 커밋 메시지: 한국어 OK, prefix는 영어 (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`)

### MonoBehaviour 규칙
- `Update()`에서 매 프레임 할당 금지 (GetComponent, FindObjectOfType, LINQ, string + 등)
- 참조는 `Awake()`/`Start()`에서 캐시
- ML 추론은 절대 `Update()`에서 동기 호출 금지 → async 파이프라인
- 코루틴보다 **UniTask** 우선

### 비동기
- 표준: **UniTask (`Cysharp.Threading.Tasks`)**
- 표준 `Task`는 GC 오버헤드로 지양
- 모든 async 함수는 `CancellationToken` 인자 필수
- 씬 전환/앱 백그라운드 시 명시적 cancel

### 메모리
- 카메라 프레임(Texture2D/RenderTexture/NativeArray) 재사용, 매 프레임 재할당 금지
- Sentis Tensor 풀링/재사용
- 자주 생성/소멸되는 UI는 `UnityEngine.Pool.ObjectPool<T>`
- 성능 주장은 측정값 기반 (Profiler ms, 할당 KB, 추론 ms)

---

## MCP Unity 사용 규칙

Claude Code의 Unity 조작은 다음 규칙 준수.

- **Play 모드 중 mcp-unity 도구 호출 금지** (충돌, 데이터 손실)
- **미커밋 손배치 변경이 있는 씬에 자동 빌더 호출 금지** (덮어쓰기 위험)
- **씬 조작 도구 호출 직전 사용자가 수동 `Ctrl+R`로 컴파일 동기화** (Auto Refresh 꺼짐)
- **Tools → MCP Unity → Server Window의 Start Server 상태 확인** (꺼져있으면 도구 무응답)

---

## 보안 / 민감 정보

### 절대 커밋 금지
- Gemini API 키, 기타 외부 서비스 토큰
- 개인 식별 정보가 담긴 테스트 데이터

### API 키 패턴
`Assets/_Project/Scripts/Secrets.cs`에 저장 (`.gitignore`로 제외됨):

```csharp
namespace Artti.SignBridge
{
    public static class Secrets
    {
        public const string GEMINI_API_KEY = "여기에 키";
    }
}
```

`.gitignore` 확인 항목: `**/Secrets.cs`, `**/*.secret`, `api_keys.txt`

### 빌드 배포 시 주의
Release 빌드에 API 키가 IL2CPP 컴파일되어도 정적 분석으로 추출 가능. 외부 배포 시 서버 프록시 등 별도 키 관리 전략 고려.

---

## 데이터 / 모델

- ONNX 파일(`*.onnx`)은 커밋 (변환 결과물)
- AI Hub KSL 원본 영상, PyTorch 가중치(`.pth`, `.pt`), 학습 중간 산출물은 `.gitignore`로 제외
- 학습 데이터는 별도 위치(예: `D:\KSL_Dataset\`)에 보관

---

## 협업 규칙

- main 브랜치 직접 푸시 금지 (작업 브랜치 → PR)
- 작업 브랜치명: `feat/기능명`, `fix/이슈명`, `chore/작업명`
- 큰 변경(머지)은 단톡 사전 공유
