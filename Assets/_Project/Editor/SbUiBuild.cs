using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Artti.SignBridge.UI;
using Artti.SignBridge.App;
using Artti.SignBridge.Speech;

namespace Artti.SignBridge.EditorTools
{
    /// <summary>
    /// 화면 빌더 공용 유틸 — 색 토큰·폰트·Canvas/부트스트랩·UI 프리미티브·SerializeField 배선.
    /// AAC의 SceneBuilderUtils에 대응. 각 *SceneBuilder가 이걸 사용해 일관된 화면을 생성한다.
    /// 폰트는 기존 NotoSansKR SDF 에셋 로드(재베이킹 금지). 둥근 모서리는 빌트인 UISprite 사용.
    /// </summary>
    public static class SbUiBuild
    {
        // ── 색 토큰(목업 추출 근사값, 디자인 토큰 확정 시 교체) ──
        public static readonly Color Ink       = Hex("#2A2F3A"); // 본문 진한 글씨
        public static readonly Color Brand      = Hex("#3E4E6E"); // 로고/타이틀
        public static readonly Color SubInk     = Hex("#7E8795");
        public static readonly Color Muted      = Hex("#9AA2AE");
        public static readonly Color Primary    = Hex("#4C8DE0"); // 주요 버튼/배지
        public static readonly Color CardBg     = Color.white;
        public static readonly Color CardLine   = Hex("#E4E7EC");
        public static readonly Color ScreenBg   = Hex("#FFFFFF");
        public static readonly Color Good        = Hex("#22C55E");
        public static readonly Color Pending     = Hex("#C7CDD6");
        public static readonly Color PillBg      = Hex("#EAF1FB");
        public static readonly Color Secondary   = Hex("#F4F6F8"); // 보조(아웃라인풍) 버튼

        const string BoldPath    = "Assets/_Project/Fonts/NotoSansKR-Bold SDF.asset";
        const string RegularPath = "Assets/_Project/Fonts/NotoSansKR-Regular SDF.asset";

        public static TMP_FontAsset Bold()    => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldPath);
        public static TMP_FontAsset Regular() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularPath);
        public static bool FontsOk()          => Bold() != null && Regular() != null;

        public static Sprite Rounded() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        public static Sprite Circle()  => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        const string AppIconPath = "Assets/_Project/Textures/AppIcon.png";

        /// <summary>대표 앱 아이콘(AAC_SBIMAGE) 스프라이트. Textures/AppIcon.png를 Sprite로 임포트해 반환.
        /// 파일이 없으면 null(호출부가 기존 원형 로고로 폴백).</summary>
        public static Sprite AppIcon()
        {
            if (!System.IO.File.Exists(AppIconPath)) return null;
            var imp = AssetImporter.GetAtPath(AppIconPath) as TextureImporter;
            if (imp == null) { AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceUpdate); imp = AssetImporter.GetAtPath(AppIconPath) as TextureImporter; }
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 256; // UI 아이콘이라 원본 큰 해상도 불필요
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(AppIconPath);
        }

        const string GuideIllustPath = "Assets/_Project/Textures/GuideIllustration.png";

        /// <summary>가이드 모달 일러스트(수어 교실) 스프라이트. 없으면 null(호출부가 회색 자리표시로 폴백).</summary>
        public static Sprite GuideIllustration()
        {
            if (!System.IO.File.Exists(GuideIllustPath)) return null;
            var imp = AssetImporter.GetAtPath(GuideIllustPath) as TextureImporter;
            if (imp == null) { AssetDatabase.ImportAsset(GuideIllustPath, ImportAssetOptions.ForceUpdate); imp = AssetImporter.GetAtPath(GuideIllustPath) as TextureImporter; }
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(GuideIllustPath);
        }

        const string PillPath = "Assets/_Project/Textures/Sb_Pill.png";

        /// <summary>코너 반지름이 큰 둥근(캡슐형) 스프라이트. 빌트인 UISprite보다 둥글기가 커서
        /// 얇은 진행바를 Sliced로 그려도 끝이 반원(알약 모양)이 된다. 없으면 1회 생성.</summary>
        public static Sprite Pill()
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(PillPath);
            if (sp != null) return sp;

            // 64×64 둥근 사각형(반지름 24, 안쪽 16px 솔리드) 텍스처를 만들어 9-slice 보더 24로 임포트.
            const int s = 64, r = 24;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color32[s * s];
            float c = (s - 1) * 0.5f, half = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - c) - (half - r), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - c) - (half - r), 0f);
                    float a = Mathf.Clamp01(0.5f - (Mathf.Sqrt(dx * dx + dy * dy) - r)); // 1px AA
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Textures"))
                AssetDatabase.CreateFolder("Assets/_Project", "Textures");
            System.IO.File.WriteAllBytes(PillPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PillPath, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(PillPath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = new Vector4(r, r, r, r); // 9-slice: 끝이 둥글게 유지
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(PillPath);
        }

        // ── 인프라(없으면 생성) ─────────────────────────────────────────────

        /// <summary>AR 카메라/월드 캔버스를 가로채지 않도록 Overlay 루트 캔버스만 재사용·생성.
        /// 기존 캔버스가 있으면 스케일러 설정을 절대 건드리지 않는다(사용자 손배치 보존).</summary>
        public static Canvas EnsureCanvas()
        {
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

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static void EnsureBootstrap()
        {
            var sm = Object.FindFirstObjectByType<ScreenManager>();
            GameObject app = sm != null ? sm.gameObject : new GameObject("_App");
            if (app.GetComponent<ScreenManager>() == null)     app.AddComponent<ScreenManager>();
            if (app.GetComponent<SessionController>() == null)  app.AddComponent<SessionController>();
            if (app.GetComponent<TtsService>() == null)         app.AddComponent<TtsService>();
            if (app.GetComponent<AppBootstrap>() == null)       app.AddComponent<AppBootstrap>();
        }

        /// <summary>해당 화면 패널을 재생성(기존 동일 타입 패널 제거 후). 풀스크린 배경 패널 + 컨트롤러 반환.</summary>
        public static T ReplacePanel<T>(Canvas canvas, string name, AppScreen screen, Color bg, out RectTransform root)
            where T : UIScreen
        {
            // 비활성 패널(다른 화면 빌드 때 IsolateInEditor가 꺼둠)까지 찾아 제거.
            // Include 안 하면 옛 패널을 못 지우고 새로 하나 더 생겨 중복·가려짐 발생.
            foreach (var ex in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(ex.gameObject);

            root = NewRect(name, canvas.transform);
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = bg;
            var comp = root.gameObject.AddComponent<T>();
            WireEnum(comp, "_screen", (int)screen);
            Register(comp);
            IsolateInEditor(comp);
            return comp;
        }

        /// <summary>편집 편의: 단일 씬에서 방금 만든 패널만 켜고 나머지 화면 패널은 끈다(겹침 방지).
        /// 런타임 표시는 ScreenManager가 따로 제어하므로 영향 없음.</summary>
        public static void IsolateInEditor(UIScreen keep)
        {
            foreach (var p in Object.FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.gameObject.SetActive(p == keep);
        }

        public static void Register(UIScreen screen)
        {
            var sm = Object.FindFirstObjectByType<ScreenManager>();
            if (sm == null) return;
            var so = new SerializedObject(sm);
            var list = so.FindProperty("_screens");
            // 재빌드로 파괴된(missing) 참조 정리 후 중복 등록 방지
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    list.DeleteArrayElementAtIndex(i);
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == screen) return; // 이미 등록
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = screen;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── UI 프리미티브 ──────────────────────────────────────────────────

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image AddImage(Transform parent, string name, Color color,
            Sprite sprite = null, Image.Type type = Image.Type.Sliced)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = type; }
            return img;
        }

        public static TextMeshProUGUI AddText(Transform parent, string name, string text,
            TMP_FontAsset font, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = NewRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font; tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
            return tmp;
        }

        /// <summary>둥근 배경 + 중앙 라벨 버튼. 라벨 TMP는 out으로 반환(좌측정렬 등 추가 조정용).</summary>
        public static Button AddButton(Transform parent, string name, Color bg, string text,
            TMP_FontAsset font, float size, Color textColor, out TextMeshProUGUI label)
        {
            var img = AddImage(parent, name, bg, Rounded(), Image.Type.Sliced);
            var btn = img.gameObject.AddComponent<Button>();
            label = AddText(img.transform, "Label", text, font, size, textColor);
            Stretch(label.rectTransform);
            return btn;
        }

        // ── 앵커 헬퍼 ──────────────────────────────────────────────────────

        public static void Stretch(RectTransform r)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero; }

        public static void Center(RectTransform r, Vector2 size, Vector2 pos)
        { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f); r.sizeDelta = size; r.anchoredPosition = pos; }

        public static void Bottom(RectTransform r, Vector2 size, float yFromBottom)
        { r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f); r.pivot = new Vector2(0.5f, 0f); r.sizeDelta = size; r.anchoredPosition = new Vector2(0, yFromBottom); }

        public static void TopCenter(RectTransform r, Vector2 size, float yFromTop)
        { r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f); r.sizeDelta = size; r.anchoredPosition = new Vector2(0, -yFromTop); }

        public static void TopLeft(RectTransform r, Vector2 size, Vector2 pad)
        { r.anchorMin = r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 1f); r.sizeDelta = size; r.anchoredPosition = new Vector2(pad.x, -pad.y); }

        /// <summary>좌측 엣지 기준 배치(아이콘 등). anchor (0,0.5).</summary>
        public static void LeftMid(RectTransform r, Vector2 size, float xFromLeft)
        { r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f); r.pivot = new Vector2(0f, 0.5f); r.sizeDelta = size; r.anchoredPosition = new Vector2(xFromLeft, 0); }

        // ── 배선 ───────────────────────────────────────────────────────────

        public static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireEnum(Object target, string field, int v)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).enumValueIndex = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }
}
