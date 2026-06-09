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
    /// 홈 대시보드(63.png) 빌더. 로고 + 준비상태 카드(모델/카메라, 조건 성립 시 초록불) +
    /// 가이드 pill(미들 모달) + "수업 시작" CTA + 하단 수업로그/환경설정 + 숨김 가이드 모달.
    /// 메뉴: Artti > Build Home Screen (재실행 안전)
    /// </summary>
    public static class HomeSceneBuilder
    {
        [MenuItem("Artti/Build Home Screen")]
        public static void Build()
        {
            if (!FontsOk()) { EditorUtility.DisplayDialog("Builder", "NotoSansKR SDF 폰트를 찾지 못했습니다.", "확인"); return; }
            var bold = Bold(); var regular = Regular();
            var canvas = EnsureCanvas(); EnsureEventSystem(); EnsureBootstrap();

            var screen = ReplacePanel<HomeScreen>(canvas, "HomeScreen", AppScreen.Home, ScreenBg, out var root);

            // ── 헤더: 대표 앱 아이콘 + 워드마크 ──
            var iconSprite = AppIcon();
            Image logo = iconSprite != null
                ? AddImage(root, "Logo", Color.white, iconSprite, Image.Type.Simple) // 흰 틴트=원본 색 그대로
                : AddImage(root, "Logo", Primary, Circle(), Image.Type.Simple);       // 폴백
            TopLeft(logo.rectTransform, new Vector2(78, 78), new Vector2(41.075f, 51.075f));   // 손배치 좌표 박음
            var word = AddText(root, "Wordmark", "SIGNBRIDGE", bold, 40, Brand, TextAlignmentOptions.Left);
            TopLeft(word.rectTransform, new Vector2(560, 60), new Vector2(160, 85)); // 손배치 좌표 박음

            // ── 준비 상태 카드 ──
            var card = AddImage(root, "StatusCard", CardBg, Pill(), Image.Type.Sliced);
            TopCenter(card.rectTransform, new Vector2(920, 210), 178); // 손배치 좌표 박음
            card.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.06f);

            var modelIcon = StatusRow(card.transform, "ModelRow", "한국어 수어 모델 로드 완료", bold, regular, 46);
            var camIcon = StatusRow(card.transform, "CameraRow", "필수 카메라 권한 허용", bold, regular, -46);

            // ── 가이드 안내 pill(미들 모달 트리거) ──
            var pill = AddButton(root, "GuideButton", PillBg, "SignBridge 가이드가 필요하신가요?",
                bold, 23, Ink, out var pillLabel);
            pill.image.sprite = Pill();
            TopCenter((RectTransform)pill.transform, new Vector2(920, 110), 392.39f); // 손배치 좌표 박음
            // 흰 배경에서 잘 보이도록 연한 파란 테두리
            var pillLine = pill.gameObject.AddComponent<Outline>();
            pillLine.effectColor = new Color(Primary.r, Primary.g, Primary.b, 0.45f);
            pillLine.effectDistance = new Vector2(2, -2);
            // 라벨은 ? 아이콘을 피해 좌측 여백 확보(공백 대신 오프셋)
            pillLabel.alignment = TextAlignmentOptions.Left;
            var lrt = pillLabel.rectTransform;
            lrt.offsetMin = new Vector2(96, 0);
            lrt.offsetMax = new Vector2(-24, 0);
            var q = AddImage(pill.transform, "Q", Primary, Circle(), Image.Type.Simple);
            LeftMid(q.rectTransform, new Vector2(50, 50), 26);
            var qm = AddText(q.transform, "QMark", "?", bold, 28, Color.white);
            Stretch(qm.rectTransform);

            // ── "수업 시작" CTA(상단 기준 고정 — 짧은 화면에서도 겹침 없음) ──
            var start = AddButton(root, "StartButton", Primary, "수업 시작", bold, 42, Color.white, out var startLabel);
            start.image.sprite = Pill();
            TopCenter((RectTransform)start.transform, new Vector2(920, 200), 531.7f); // 손배치 좌표 박음
            // 라벨을 우측으로 밀어 좌측에 학사모 아이콘 자리 확보
            startLabel.rectTransform.offsetMin = new Vector2(80, startLabel.rectTransform.offsetMin.y);
            // 학사모: Material Symbols 'school'(U+E80C)
            AddSymbol(start.transform, "StartIcon", '', fill: true, size: 66, color: Color.white,
                      pos: new Vector2(-130, 0));

            // ── 하단 보조 버튼 2개 ──
            var logs = AddButton(root, "AllLogsButton", Secondary, "수업 로그", bold, 26, Ink, out _);
            logs.image.sprite = Pill();
            BottomHalf((RectTransform)logs.transform, -134, 232.4f);   // 손배치 좌표 박음
            var settings = AddButton(root, "SettingsButton", Secondary, "환경 설정", bold, 26, Ink, out _);
            settings.image.sprite = Pill();
            BottomHalf((RectTransform)settings.transform, 143.86f, 232.4f); // 손배치 좌표 박음

            // ── 사용 안내 모달(숨김) ──
            var modal = BuildGuideModal(root, bold, regular, out var closeBtn);

            // ── 배선 ──
            Wire(screen, "_modelStatusIcon", modelIcon);
            Wire(screen, "_cameraStatusIcon", camIcon);
            Wire(screen, "_guideButton", pill);
            Wire(screen, "_guideModal", modal);
            Wire(screen, "_guideCloseButton", closeBtn);
            Wire(screen, "_startClassButton", start);
            Wire(screen, "_allLogsButton", logs);
            Wire(screen, "_settingsButton", settings);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[HomeBuilder] 생성 완료. 씬 저장(Ctrl+S) 필요.");
        }

        /// <summary>준비상태 한 줄(체크 원 + 라벨). 체크 Image를 반환(컨트롤러가 색 토글).</summary>
        static Image StatusRow(Transform card, string name, string text,
            TMP_FontAsset bold, TMP_FontAsset regular, float y)
        {
            var row = NewRect(name, card);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(820, 72);
            row.anchoredPosition = new Vector2(0, y);

            var icon = AddImage(row, "Check", Pending, Circle(), Image.Type.Simple);
            LeftMid(icon.rectTransform, new Vector2(40, 40), 10);
            var v = AddText(icon.transform, "V", "✓", bold, 24, Color.white);
            Stretch(v.rectTransform);

            var label = AddText(row, "Label", text, bold, 26, Ink, TextAlignmentOptions.Left); // 준비상태는 볼드
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(70, 0); lrt.offsetMax = new Vector2(-10, 0);
            return icon;
        }

        /// <summary>하단 엣지 기준 좌/우 배치(보조 버튼).</summary>
        static void BottomHalf(RectTransform r, float x, float y)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(430, 120);
            r.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Material Symbols 아이콘 1글자를 (pos) 중심에 배치. code는 글리프 유니코드(예 school=U+E80C).</summary>
        static MaterialSymbol AddSymbol(Transform parent, string name, char code, bool fill,
            float size, Color color, Vector2 pos)
        {
            var rt = NewRect(name, parent);
            var sym = rt.gameObject.AddComponent<MaterialSymbol>();
            Center(rt, new Vector2(size, size), pos);
            sym.color = color;
            sym.fill = fill;
            sym.scale = 0.92f;          // 정사각 글자가 박스를 꽉 채우지 않게 약간 여백
            sym.code = code;            // 폰트/텍스트 갱신 트리거
            sym.raycastTarget = false;  // 버튼 클릭 방해 방지
            return sym;
        }

        static GameObject BuildGuideModal(Transform root, TMP_FontAsset bold, TMP_FontAsset regular, out Button closeBtn)
        {
            var dim = AddImage(root, "GuideModal", new Color(0, 0, 0, 0.5f)); // 라이트박스(클릭 차단)
            Stretch(dim.rectTransform);

            // ── 중앙 카드(66.png 이용 가이드) ──
            var card = AddImage(dim.transform, "ModalCard", CardBg, Pill(), Image.Type.Sliced);
            Center(card.rectTransform, new Vector2(860, 1040), Vector2.zero); // 화면 정중앙
            card.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.12f);

            // 헤더: 제목(2줄) + 우상단 X
            var title = AddText(card.transform, "Title", "SIGNBRIDGE\n이용 가이드", bold, 40, Brand, TextAlignmentOptions.TopLeft);
            TopLeft(title.rectTransform, new Vector2(560, 130), new Vector2(52, 48));
            title.lineSpacing = 6f;

            var closeHit = AddImage(card.transform, "CloseButton", new Color(0, 0, 0, 0)); // 투명 히트영역
            var crt = closeHit.rectTransform;
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1f, 1f);
            crt.sizeDelta = new Vector2(76, 76);
            crt.anchoredPosition = new Vector2(-24, -24);
            closeBtn = closeHit.gameObject.AddComponent<Button>();
            AddSymbol(closeHit.transform, "X", MaterialSymbol.ConvertHexToChar("e5cd"), // close
                      fill: false, size: 44, color: Muted, pos: Vector2.zero);

            // 일러스트(수어 교실 그림). 없으면 회색 자리표시로 폴백.
            var illustSprite = GuideIllustration();
            if (illustSprite != null)
            {
                var illust = AddImage(card.transform, "Illustration", Color.white, illustSprite, Image.Type.Simple);
                illust.preserveAspect = true; // 2816×1536(1.833) 비율 유지
                TopCenter(illust.rectTransform, new Vector2(744, 406), 196);
            }
            else
            {
                var illust = AddImage(card.transform, "Illustration", new Color32(234, 237, 241, 255), Pill(), Image.Type.Sliced);
                TopCenter(illust.rectTransform, new Vector2(744, 360), 200);
                AddSymbol(illust.transform, "IllustIcon", MaterialSymbol.ConvertHexToChar("e3f4"), // image
                          fill: true, size: 96, color: new Color32(185, 194, 207, 255), pos: Vector2.zero);
            }

            // 번호 뱃지 3단계
            StepRow(card.transform, 1, "'수업 시작' 버튼을 눌러 수어 인식 화면으로 진입합니다.", bold, 620);
            StepRow(card.transform, 2, "디바이스의 카메라를 학생의 상반신이 보이게 배치합니다.", bold, 740);
            StepRow(card.transform, 3, "'분석 시작' 버튼을 눌러 수어를 인식하고 통역을 시작합니다.", bold, 860);

            dim.gameObject.SetActive(false); // 기본 숨김(HomeScreen.Awake도 닫힘 보장)
            return dim.gameObject;
        }

        /// <summary>번호 뱃지(파란 원) + 설명 한 줄. 카드 상단 기준 yFromTop에 배치.</summary>
        static void StepRow(Transform card, int number, string text, TMP_FontAsset font, float yFromTop)
        {
            var row = NewRect("Step" + number, card);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(740, 100);
            row.anchoredPosition = new Vector2(0, -yFromTop);

            var badge = AddImage(row, "Badge", Primary, Circle(), Image.Type.Simple);
            LeftMid(badge.rectTransform, new Vector2(50, 50), 0);
            var num = AddText(badge.transform, "Num", number.ToString(), font, 26, Color.white);
            Stretch(num.rectTransform);

            var label = AddText(row, "Text", text, font, 24, Ink, TextAlignmentOptions.Left);
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(74, 0); lrt.offsetMax = new Vector2(0, 0);
        }
    }
}
