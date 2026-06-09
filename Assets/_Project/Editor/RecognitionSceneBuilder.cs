using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Artti.SignBridge.UI;
using static Artti.SignBridge.EditorTools.SbUiBuild;

namespace Artti.SignBridge.EditorTools
{
    /// <summary>
    /// 수업 인식(SCR-100, plan §7) 골격 빌더. AR 피드 자리(어두운 배경) + 상단 상태바(인식중·누적단어) +
    /// 인디케이터 범례(🟢확정/🟡미인식/⚪감지중) + 우상단 메뉴(세션로그/종료) + TTS 토글 + 하단 자막.
    /// AR 카메라 피드·월드 말풍선·키포인트 오버레이는 파이프라인 단계에서 연동.
    /// 메뉴: Artti > Build Recognition Screen (재실행 안전)
    /// </summary>
    public static class RecognitionSceneBuilder
    {
        static readonly Color Feed   = new Color(0.06f, 0.08f, 0.11f, 1f);   // AR 피드 placeholder
        static readonly Color BarBg  = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color CapBg  = new Color(0f, 0f, 0f, 0.62f);
        static readonly Color Danger = new Color(0.86f, 0.30f, 0.30f);       // 종료

        [MenuItem("Artti/Build Recognition Screen")]
        public static void Build()
        {
            if (!FontsOk()) { EditorUtility.DisplayDialog("Builder", "NotoSansKR SDF 폰트를 찾지 못했습니다.", "확인"); return; }
            var bold = Bold(); var regular = Regular();
            var canvas = EnsureCanvas(); EnsureEventSystem(); EnsureBootstrap();

            var screen = ReplacePanel<RecognitionScreen>(canvas, "RecognitionScreen", AppScreen.Recognition, Feed, out var root);

            // AR 피드 안내(중앙, 옅게)
            var ph = AddText(root, "FeedPlaceholder", "AR 카메라 피드\n(파이프라인 단계에서 연동)",
                regular, 26, new Color(1, 1, 1, 0.25f));
            Center(ph.rectTransform, new Vector2(900, 140), Vector2.zero);

            // ── 상단 상태바(풀폭) ──
            var bar = AddImage(root, "TopBar", BarBg);
            FullWidthTop(bar.rectTransform, 190);

            var dot = AddImage(bar.transform, "StateDot", Pending, Circle(), Image.Type.Simple);
            TL(dot.rectTransform, new Vector2(30, 30), new Vector2(40, 44));
            var st = AddText(bar.transform, "StateLabel", "인식 중", bold, 26, Color.white, TextAlignmentOptions.Left);
            TL(st.rectTransform, new Vector2(220, 40), new Vector2(86, 36));
            var buf = AddText(bar.transform, "BufferLabel", "누적 단어: (없음)", regular, 22, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Left);
            TL(buf.rectTransform, new Vector2(640, 46), new Vector2(40, 104));

            var logBtn = AddButton(bar.transform, "SessionLogButton", new Color(1, 1, 1, 0.14f), "세션 로그", bold, 24, Color.white, out _);
            TR((RectTransform)logBtn.transform, new Vector2(210, 96), new Vector2(220, 32));
            var endBtn = AddButton(bar.transform, "EndButton", Danger, "종료", bold, 24, Color.white, out _);
            TR((RectTransform)endBtn.transform, new Vector2(160, 96), new Vector2(40, 32));

            // ── 인디케이터 범례 ──
            var legend = AddText(root, "Legend",
                "<color=#22C55E>●</color> 확정    <color=#F5C542>●</color> 미인식(?)    <color=#9AA2AE>●</color> 감지 중",
                regular, 24, Color.white);
            legend.richText = true;
            TopCenter(legend.rectTransform, new Vector2(960, 44), 220);

            // ── TTS 토글(자막 위 우측) ──
            var tts = AddButton(root, "TtsToggleButton", new Color(0.40f, 0.44f, 0.50f), "TTS 음성 꺼짐", bold, 22, Color.white, out var ttsLabel);
            BR((RectTransform)tts.transform, new Vector2(280, 84), new Vector2(40, 290));

            // ── 하단 자막(Live Caption) ──
            var cap = AddImage(root, "Caption", CapBg, Rounded(), Image.Type.Sliced);
            FullWidthBottom(cap.rectTransform, 210, 40, 40);
            var capTag = AddText(cap.transform, "Tag", "최근 변환", regular, 18, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Left);
            TL(capTag.rectTransform, new Vector2(300, 30), new Vector2(30, 22));
            var capText = AddText(cap.transform, "Sentence", "(변환된 문장이 여기에 표시됩니다)", bold, 32, Color.white);
            Center(capText.rectTransform, new Vector2(900, 90), new Vector2(0, -8));

            // ── 배선 ──
            Wire(screen, "_sessionLogButton", logBtn);
            Wire(screen, "_endButton", endBtn);
            Wire(screen, "_ttsToggleButton", tts);
            Wire(screen, "_ttsButtonBg", tts.GetComponent<Image>());
            Wire(screen, "_ttsLabel", ttsLabel);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[RecognitionBuilder] 골격 생성 완료. 씬 저장(Ctrl+S) 필요.");
        }

        // 이 화면 전용 풀폭/모서리 앵커 헬퍼
        static void FullWidthTop(RectTransform r, float h)
        { r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1); r.sizeDelta = new Vector2(0, h); r.anchoredPosition = Vector2.zero; }
        static void FullWidthBottom(RectTransform r, float h, float mx, float my)
        { r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0); r.sizeDelta = new Vector2(-2 * mx, h); r.anchoredPosition = new Vector2(0, my); }
        static void TL(RectTransform r, Vector2 size, Vector2 pad)
        { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.sizeDelta = size; r.anchoredPosition = new Vector2(pad.x, -pad.y); }
        static void TR(RectTransform r, Vector2 size, Vector2 pad)
        { r.anchorMin = r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(1, 1); r.sizeDelta = size; r.anchoredPosition = new Vector2(-pad.x, -pad.y); }
        static void BR(RectTransform r, Vector2 size, Vector2 pad)
        { r.anchorMin = r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(1, 0); r.sizeDelta = size; r.anchoredPosition = new Vector2(-pad.x, pad.y); }
    }
}
