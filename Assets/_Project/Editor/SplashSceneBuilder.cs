using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Artti.SignBridge.UI;
using Artti.SignBridge.App;

namespace Artti.SignBridge.EditorTools
{
    /// <summary>
    /// 스플래시 화면(61.png)을 현재 씬에 자동 구성하는 에디터 빌더.
    /// docs/ui-skeleton-setup.md "스플래시 화면 구성 스펙" 그대로 생성하며,
    /// Canvas·부트스트랩(_App)·SplashScreen 패널을 만들고 SerializeField를 배선한다.
    /// 이 .cs가 원본 — 레이아웃 수정은 코드에서 하고 메뉴를 재실행한다(idempotent).
    /// 메뉴: Artti > Build Splash Screen
    /// </summary>
    public static class SplashSceneBuilder
    {
        // 61.png 추출 근사 색상 (디자인 토큰 확정 시 교체)
        static readonly Color Ink      = Hex("#3E4E6E"); // 타이틀/team ARTTI
        static readonly Color SubInk   = Hex("#7E8795"); // 부제
        static readonly Color Muted    = Hex("#9AA2AE"); // 상태 문구
        static readonly Color Track    = Hex("#E4E7EC"); // 진행바 트랙
        static readonly Color Fill     = Hex("#4C8DE0"); // 진행바 채움
        static readonly Color Bg       = Color.white;

        const string FontBoldPath    = "Assets/_Project/Fonts/NotoSansKR-Bold SDF.asset";
        const string FontRegularPath = "Assets/_Project/Fonts/NotoSansKR-Regular SDF.asset";

        [MenuItem("Artti/Build Splash Screen")]
        public static void Build()
        {
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontBoldPath);
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontRegularPath);
            if (bold == null || regular == null)
            {
                EditorUtility.DisplayDialog("Splash Builder",
                    "NotoSansKR SDF 폰트를 찾지 못했습니다.\n" + FontBoldPath, "확인");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            Canvas canvas = EnsureCanvas();
            EnsureEventSystem();
            EnsureBootstrap();

            // 기존 스플래시 패널이 있으면 제거 후 재생성(재실행 안전).
            // 비활성 패널까지 포함해야 중복이 안 쌓인다(Include).
            foreach (var existing in Object.FindObjectsByType<SplashScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);

            // ── SplashScreen 루트(흰 배경 풀스크린 패널) ──────────────────────
            var panel = NewUI("SplashScreen", canvas.transform);
            Stretch(panel.rect);
            var bgImg = panel.go.AddComponent<Image>();
            bgImg.color = Bg;
            var splash = panel.go.AddComponent<SplashScreen>();

            // ── TitleGroup (화면 중앙보다 위 — 61.png 구도: 타이틀이 상단 1/3 지점) ──
            var title = Text("Title", panel.rect, "SIGNBRIDGE", bold, 64, Ink);
            Center(title.rect, new Vector2(900, 90), new Vector2(0, 300));
            title.tmp.characterSpacing = 6f;

            var subtitle = Text("Subtitle", panel.rect, "실시간 수어 통역 도우미", regular, 22, SubInk);
            Center(subtitle.rect, new Vector2(900, 40), new Vector2(0, 205));

            // ── BottomGroup (하단 엣지 기준 — 가로/세로·해상도 무관하게 보이도록) ──
            var status = Text("Status", panel.rect, "수어 인식 모델 준비 중", regular, 15, Muted);
            Bottom(status.rect, new Vector2(900, 30), 360);

            var rounded = SbUiBuild.Pill(); // 끝이 완전히 둥근 캡슐형 스프라이트(빌트인보다 둥글기 큼)

            var trackGo = NewUI("ProgressTrack", panel.rect);
            Bottom(trackGo.rect, new Vector2(960, 26), 300); // 61.png처럼 풀폭·두툼하게
            var trackImg = trackGo.go.AddComponent<Image>();
            trackImg.sprite = rounded;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = Track;

            // 채움: Type.Filled는 둥근 스프라이트를 얇은 바에 압축할 때 좌측 모서리가
            // 검게 뭉치는 아티팩트가 있어, 좌측 앵커 폭(anchorMax.x)으로 채운다.
            // 트랙과 동일한 둥근 Sliced 스프라이트 → 둥근 채움, 아티팩트 없음.
            var fillGo = NewUI("ProgressFill", trackGo.rect);
            var fillRect = fillGo.rect;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0.6f, 1f); // 목업 기준 기본값(런타임에 AppBootstrap이 갱신)
            fillRect.pivot     = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.go.AddComponent<Image>();
            fillImg.sprite = rounded;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = Fill;

            var team = Text("TeamArtti", panel.rect, "team ARTTI", bold, 20, Ink);
            Bottom(team.rect, new Vector2(600, 36), 120);

            // ── SerializeField 배선 ─────────────────────────────────────────
            var so = new SerializedObject(splash);
            so.FindProperty("_screen").enumValueIndex = (int)AppScreen.Splash;
            so.FindProperty("_progressFill").objectReferenceValue = fillImg;
            so.FindProperty("_statusText").objectReferenceValue = status.tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            RegisterInScreenManager(splash);

            panel.go.SetActive(true);
            SbUiBuild.IsolateInEditor(splash); // 단일 씬 겹침 방지(방금 만든 화면만 표시)
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = panel.go;
            Debug.Log($"[SplashBuilder] '{scene.name}' 씬에 스플래시 화면 생성 완료. 저장(Ctrl+S) 필요.");
        }

        // ── 인프라(없으면 생성) ─────────────────────────────────────────────

        static Canvas EnsureCanvas()
        {
            // AR 카메라/월드 캔버스를 가로채지 않도록 Overlay 루트 캔버스만 재사용
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.transform.parent == null)
                    return c;

            var go = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // portrait
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static void EnsureBootstrap()
        {
            var sm = Object.FindFirstObjectByType<ScreenManager>();
            GameObject app = sm != null ? sm.gameObject : new GameObject("_App");
            if (app.GetComponent<ScreenManager>() == null) app.AddComponent<ScreenManager>();
            if (app.GetComponent<SessionController>() == null) app.AddComponent<SessionController>();
            if (app.GetComponent<AppBootstrap>() == null) app.AddComponent<AppBootstrap>();
        }

        static void RegisterInScreenManager(SplashScreen splash)
        {
            var sm = Object.FindFirstObjectByType<ScreenManager>();
            if (sm == null) return;
            var so = new SerializedObject(sm);
            var list = so.FindProperty("_screens");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == splash) return; // 이미 등록
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = splash;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── UI 생성 헬퍼 ───────────────────────────────────────────────────

        struct UINode { public GameObject go; public RectTransform rect; }
        struct TextNode { public GameObject go; public RectTransform rect; public TextMeshProUGUI tmp; }

        static UINode NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return new UINode { go = go, rect = go.GetComponent<RectTransform>() };
        }

        static TextNode Text(string name, Transform parent, string content,
            TMP_FontAsset font, float size, Color color)
        {
            var n = NewUI(name, parent);
            var tmp = n.go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return new TextNode { go = n.go, rect = n.rect, tmp = tmp };
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        /// <summary>화면 중앙 기준 앵커로 크기·오프셋 배치(portrait 1080×1920 기준).</summary>
        static void Center(RectTransform r, Vector2 size, Vector2 anchoredPos)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = size;
            r.anchoredPosition = anchoredPos;
        }

        /// <summary>하단 엣지(0.5,0) 기준으로 위로 yFromBottom 만큼 띄워 배치.</summary>
        static void Bottom(RectTransform r, Vector2 size, float yFromBottom)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = size;
            r.anchoredPosition = new Vector2(0f, yFromBottom);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
