using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Artti.SignBridge.UI;
using com.convalise.UnityMaterialSymbols;
using static Artti.SignBridge.EditorTools.SbUiBuild;

namespace Artti.SignBridge.EditorTools
{
    /// <summary>
    /// 환경 설정(90·91.png) 빌더. 단일 화면.
    /// 좌표 기준은 다른 화면과 동일한 캔버스(ref 800×600 + Expand → 1536×2048 태블릿에서 논리 800×1066).
    /// 가로는 좌우 마진 stretch라 폭에 꽉 맞고, 폰트/세로는 800×1066 스케일(다른 화면과 일관).
    /// 캔버스 스케일러는 절대 건드리지 않는다(사용자 손배치 보존).
    /// 메뉴: Artti > Build Settings Screen (재실행 안전)
    /// </summary>
    public static class SettingsSceneBuilder
    {
        static readonly Color AppBg  = Hex("#F5F6F8");
        static readonly Color Danger = Hex("#F59E0B");

        const float SideMargin = 40f; // 화면 좌우 공통 마진(논리 800폭 기준)

        // Material Symbols 글리프
        const string GArrowBack = "e5c4"; // arrow_back
        const string GSpeaker   = "e98e"; // brand_awareness (음성 출력 테스트 스피커)
        const string GDelete    = "e872"; // delete
        const string GForward   = "f187"; // forward_to_inbox (팀 문의 메일)
        const string GClose     = "e5cd"; // close

        [MenuItem("Artti/Build Settings Screen")]
        public static void Build()
        {
            if (!FontsOk()) { EditorUtility.DisplayDialog("Builder", "NotoSansKR SDF 폰트를 찾지 못했습니다.", "확인"); return; }
            var bold = Bold(); var regular = Regular();
            var canvas = EnsureCanvas(); EnsureEventSystem(); EnsureBootstrap();

            var screen = ReplacePanel<SettingsScreen>(canvas, "SettingsScreen", AppScreen.Settings, AppBg, out var root);

            // ── 헤더: 뒤로 버튼(원형) + 제목 ──
            var backBg = AddImage(root, "BackButton", Color.white, Circle(), Image.Type.Simple);
            TopLeft(backBg.rectTransform, new Vector2(60, 60), new Vector2(SideMargin, 34));
            backBg.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.08f);
            var back = backBg.gameObject.AddComponent<Button>();
            var backIcon = MakeSymbol(backBg.transform, "Arrow", Glyph(GArrowBack), false, Ink);
            Center(backIcon.rectTransform, new Vector2(34, 34), Vector2.zero);

            var title = AddText(root, "Title", "환경 설정", bold, 40, Brand, TextAlignmentOptions.Center);
            TopCenter(title.rectTransform, new Vector2(520, 56), 38);

            // ── 음성 설정 ──
            SectionLabel(root, "음성 설정", 150, regular);
            var voiceCard = Card(root, "VoiceCard", 180, 250);

            var rateLabel = AddText(voiceCard.transform, "RateLabel", "음성 출력 속도", bold, 26, Ink, TextAlignmentOptions.Left);
            HStretchTop(rateLabel.rectTransform, 32, 24, 38);

            var slider = BuildSlider(voiceCard.transform, 32, 92);

            // 테스트 행(아이콘 + 샘플 문장) 전체가 미리듣기 트리거.
            var testImg = AddImage(voiceCard.transform, "TtsTestButton", PillBg, Pill(), Image.Type.Sliced);
            HStretchTop(testImg.rectTransform, 24, 168, 64);
            var testBtn = testImg.gameObject.AddComponent<Button>();

            var speaker = MakeSymbol(testImg.transform, "SpeakerIcon", Glyph(GSpeaker), true, SubInk);
            LeftMid(speaker.rectTransform, new Vector2(34, 34), 22);
            var testText = AddText(testImg.transform, "SampleText", "안녕하세요, SignBridge입니다!", regular, 20, Ink, TextAlignmentOptions.Left);
            var ttrt = testText.rectTransform;
            ttrt.anchorMin = new Vector2(0, 0); ttrt.anchorMax = new Vector2(1, 1);
            ttrt.offsetMin = new Vector2(70, 0); ttrt.offsetMax = new Vector2(-18, 0);

            // ── 데이터 · 개인정보 ──
            SectionLabel(root, "데이터 · 개인정보", 470, regular);
            var dataCard = Card(root, "DataCard", 500, 180);

            var dataDesc = AddText(dataCard.transform, "DataDesc", "디바이스에 저장된 전체 기록을 삭제합니다.", regular, 20, SubInk, TextAlignmentOptions.Left);
            HStretchTop(dataDesc.rectTransform, 32, 28, 36);

            var del = AddButton(dataCard.transform, "DeleteAllButton", Danger, "전체 기록 삭제", bold, 26, Color.white, out var delLabel);
            del.image.sprite = Pill();
            HStretchTop((RectTransform)del.transform, 32, 86, 76);
            delLabel.rectTransform.offsetMin = new Vector2(54, delLabel.rectTransform.offsetMin.y); // 좌측 휴지통 자리
            var delIcon = MakeSymbol(del.transform, "DeleteIcon", Glyph(GDelete), true, Color.white);
            Center(delIcon.rectTransform, new Vector2(34, 34), new Vector2(-108, 0));

            // ── 하단 문의 링크(텍스트형) ──
            var inquiry = AddButton(root, "InquiryButton", new Color(0, 0, 0, 0), "팀 ARTTI에 문의하기", bold, 22, SubInk, out var inqLabel);
            Bottom((RectTransform)inquiry.transform, new Vector2(460, 64), 40);
            inqLabel.alignment = TextAlignmentOptions.Left;
            inqLabel.rectTransform.offsetMin = new Vector2(48, 0);
            var mail = MakeSymbol(inquiry.transform, "MailIcon", Glyph(GForward), true, SubInk);
            LeftMid(mail.rectTransform, new Vector2(32, 32), 6);

            // ── 삭제 확인 모달(숨김, 91.png) ──
            var modal = BuildDeleteModal(root, bold, regular, out var confirm, out var cancel, out var close);

            // ── 배선 ──
            Wire(screen, "_backButton", back);
            Wire(screen, "_ttsRateSlider", slider);
            Wire(screen, "_ttsTestButton", testBtn);
            Wire(screen, "_ttsSpeakerIcon", speaker);
            Wire(screen, "_deleteAllButton", del);
            Wire(screen, "_deleteModal", modal);
            Wire(screen, "_deleteConfirmButton", confirm);
            Wire(screen, "_deleteCancelButton", cancel);
            Wire(screen, "_deleteCloseButton", close);
            Wire(screen, "_inquiryButton", inquiry);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[SettingsBuilder] 생성 완료(논리 800×1066 기준). 씬 저장(Ctrl+S) 필요.");
        }

        // ── 레이아웃 헬퍼 ───────────────────────────────────────────────────

        /// <summary>부모 폭 기준 가로 stretch + 상단 고정 배치. 폭은 부모를 따라가고(좌우 margin),
        /// 세로는 yFromTop·height로 고정. 어떤 화면비에서도 폭에 꽉 맞춘다.</summary>
        static void HStretchTop(RectTransform r, float margin, float yFromTop, float height)
        {
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1f);
            r.offsetMin = new Vector2(margin, -(yFromTop + height)); // left, bottom
            r.offsetMax = new Vector2(-margin, -yFromTop);           // right, top
        }

        /// <summary>섹션 머리글(좌측 정렬). 화면 폭 기준 stretch.</summary>
        static void SectionLabel(Transform root, string text, float yFromTop, TMP_FontAsset font)
        {
            var t = AddText(root, "Section_" + text, text, font, 20, Muted, TextAlignmentOptions.Left);
            HStretchTop(t.rectTransform, SideMargin + 8, yFromTop, 32);
        }

        /// <summary>흰 카드(둥근 + 옅은 그림자/테두리). 화면 폭 기준 stretch, 상단 배치.</summary>
        static Image Card(Transform root, string name, float yFromTop, float height)
        {
            var c = AddImage(root, name, CardBg, Pill(), Image.Type.Sliced);
            HStretchTop(c.rectTransform, SideMargin, yFromTop, height);
            c.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.05f);
            var ol = c.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.04f);
            ol.effectDistance = new Vector2(1, -1);
            return c;
        }

        /// <summary>표준 UGUI Slider 구조. 둥근 트랙/필 + 원형 핸들, 정수 tick 5단계. 부모 폭 stretch.</summary>
        static Slider BuildSlider(Transform parent, float margin, float yFromTop)
        {
            const float handle = 36f, track = 10f;

            var rootRT = NewRect("TtsRateSlider", parent);
            HStretchTop(rootRT, margin, yFromTop, 36);
            var slider = rootRT.gameObject.AddComponent<Slider>();

            var bg = AddImage(rootRT, "Background", CardLine, Pill(), Image.Type.Sliced);
            var bgRT = bg.rectTransform;
            bgRT.anchorMin = new Vector2(0, 0.5f); bgRT.anchorMax = new Vector2(1, 0.5f);
            bgRT.pivot = new Vector2(0.5f, 0.5f); bgRT.sizeDelta = new Vector2(0, track); bgRT.anchoredPosition = Vector2.zero;

            var fillArea = NewRect("Fill Area", rootRT);
            fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.sizeDelta = new Vector2(-handle, track); fillArea.anchoredPosition = Vector2.zero;
            var fill = AddImage(fillArea, "Fill", Primary, Pill(), Image.Type.Sliced);
            fill.rectTransform.sizeDelta = new Vector2(handle, 0);

            var handleArea = NewRect("Handle Slide Area", rootRT);
            handleArea.anchorMin = new Vector2(0, 0); handleArea.anchorMax = new Vector2(1, 1);
            handleArea.pivot = new Vector2(0.5f, 0.5f);
            handleArea.sizeDelta = new Vector2(-handle, 0); handleArea.anchoredPosition = Vector2.zero;
            var h = AddImage(handleArea, "Handle", Primary, Circle(), Image.Type.Simple);
            h.rectTransform.sizeDelta = new Vector2(handle, handle);
            h.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.18f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = h.rectTransform;
            slider.targetGraphic = h;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0; slider.maxValue = 4; slider.value = 1; // 기본 1.0배(인덱스 1)
            return slider;
        }

        /// <summary>삭제 확인 미들 모달(91.png): 라이트박스 + 중앙 카드 + 메시지 + 취소/확인 + X.</summary>
        static GameObject BuildDeleteModal(Transform root, TMP_FontAsset bold, TMP_FontAsset regular,
            out Button confirm, out Button cancel, out Button close)
        {
            var dim = AddImage(root, "DeleteModal", new Color(0, 0, 0, 0.5f)); // 클릭 차단 라이트박스
            Stretch(dim.rectTransform);

            var card = AddImage(dim.transform, "ModalCard", CardBg, Pill(), Image.Type.Sliced);
            Center(card.rectTransform, new Vector2(620, 350), Vector2.zero);
            card.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.15f);

            // 우상단 X(투명 히트영역)
            var closeHit = AddImage(card.transform, "CloseButton", new Color(0, 0, 0, 0));
            var crt = closeHit.rectTransform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
            crt.sizeDelta = new Vector2(56, 56); crt.anchoredPosition = new Vector2(-16, -16);
            close = closeHit.gameObject.AddComponent<Button>();
            var x = MakeSymbol(closeHit.transform, "X", Glyph(GClose), false, Muted);
            Center(x.rectTransform, new Vector2(30, 30), Vector2.zero);

            // 메시지(2줄)
            var msg = AddText(card.transform, "Message",
                "정말 기록을 삭제할까요?\n이 작업은 되돌릴 수 없습니다.", bold, 24, Ink, TextAlignmentOptions.Center);
            var mrt = msg.rectTransform;
            mrt.anchorMin = new Vector2(0, 1); mrt.anchorMax = new Vector2(1, 1); mrt.pivot = new Vector2(0.5f, 1f);
            mrt.sizeDelta = new Vector2(-60, 120); mrt.anchoredPosition = new Vector2(0, -72);
            msg.lineSpacing = 12f;

            // 취소(아웃라인) / 확인(앰버)
            cancel = AddButton(card.transform, "CancelButton", Color.white, "취소", bold, 24, Ink, out _);
            cancel.image.sprite = Pill();
            var cancelOl = cancel.gameObject.AddComponent<Outline>();
            cancelOl.effectColor = CardLine; cancelOl.effectDistance = new Vector2(2, -2);
            var ca = (RectTransform)cancel.transform;
            ca.anchorMin = ca.anchorMax = new Vector2(0.5f, 0f); ca.pivot = new Vector2(0.5f, 0f);
            ca.sizeDelta = new Vector2(250, 80); ca.anchoredPosition = new Vector2(-135, 36);

            confirm = AddButton(card.transform, "ConfirmButton", Danger, "확인", bold, 24, Color.white, out _);
            confirm.image.sprite = Pill();
            var co = (RectTransform)confirm.transform;
            co.anchorMin = co.anchorMax = new Vector2(0.5f, 0f); co.pivot = new Vector2(0.5f, 0f);
            co.sizeDelta = new Vector2(250, 80); co.anchoredPosition = new Vector2(135, 36);

            dim.gameObject.SetActive(false); // 기본 숨김(SettingsScreen.Awake도 닫힘 보장)
            return dim.gameObject;
        }

        /// <summary>Material Symbols 1글자(위치/크기는 호출부에서). 버튼 내부 아이콘이라 raycast 끔.</summary>
        static MaterialSymbol MakeSymbol(Transform parent, string name, char code, bool fill, Color color)
        {
            var rt = NewRect(name, parent);
            var sym = rt.gameObject.AddComponent<MaterialSymbol>();
            sym.color = color;
            sym.fill = fill;
            sym.scale = 0.92f;
            sym.code = code;
            sym.raycastTarget = false;
            return sym;
        }

        static char Glyph(string hex) => MaterialSymbol.ConvertHexToChar(hex);
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }
}
