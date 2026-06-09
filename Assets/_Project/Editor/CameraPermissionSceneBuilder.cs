using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Artti.SignBridge.UI;
using static Artti.SignBridge.EditorTools.SbUiBuild;

namespace Artti.SignBridge.EditorTools
{
    /// <summary>
    /// 카메라 권한 화면(62.png) 빌더. 중앙 카드(카메라 배지 + 안내) + 하단 "카메라 권한 허용" 버튼.
    /// 버튼 탭 → CameraPermissionScreen이 Android CAMERA 권한 요청, 거부 시 앱 종료.
    /// 메뉴: Artti > Build Camera Permission Screen (재실행 안전)
    /// </summary>
    public static class CameraPermissionSceneBuilder
    {
        [MenuItem("Artti/Build Camera Permission Screen")]
        public static void Build()
        {
            if (!FontsOk()) { EditorUtility.DisplayDialog("Builder", "NotoSansKR SDF 폰트를 찾지 못했습니다.", "확인"); return; }
            var bold = Bold(); var regular = Regular();
            var canvas = EnsureCanvas(); EnsureEventSystem(); EnsureBootstrap();

            var screen = ReplacePanel<CameraPermissionScreen>(canvas, "CameraPermissionScreen",
                AppScreen.CameraPermission, ScreenBg, out var root);

            // ── 중앙 카드 ──
            var card = AddImage(root, "Card", CardBg, Pill(), Image.Type.Sliced); // 둥근 카드
            Center(card.rectTransform, new Vector2(760, 560), new Vector2(0, 80));
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.10f);
            shadow.effectDistance = new Vector2(0, -6);

            // 카메라 배지(파란 원 + 흰 카메라 근사 아이콘) — 아이콘 에셋 수령 시 교체
            var badge = AddImage(card.transform, "Badge", Primary, Circle(), Image.Type.Simple);
            Center(badge.rectTransform, new Vector2(150, 150), new Vector2(0, 150));
            var camBody = AddImage(badge.transform, "CamBody", Color.white, Pill(), Image.Type.Sliced);
            Center(camBody.rectTransform, new Vector2(80, 58), new Vector2(0, -4));
            var camTop = AddImage(camBody.transform, "CamTop", Color.white, Pill(), Image.Type.Sliced);
            Center(camTop.rectTransform, new Vector2(26, 14), new Vector2(0, 32));
            var lens = AddImage(camBody.transform, "Lens", Primary, Circle(), Image.Type.Simple);
            Center(lens.rectTransform, new Vector2(28, 28), Vector2.zero);

            // 타이틀 / 본문
            var title = AddText(card.transform, "Title", "카메라 권한이 필요해요", bold, 34, Ink);
            Center(title.rectTransform, new Vector2(680, 50), new Vector2(0, 14));

            var body = AddText(card.transform, "Body",
                "수업 중 수어를 인식하려면\n카메라 접근이 필요합니다.\n<b>영상은 외부로 전송되지 않습니다</b>",
                regular, 20, new Color32(78, 88, 104, 255)); // Muted는 너무 연해 진한 슬레이트로 + 안심 문구 볼드
            Center(body.rectTransform, new Vector2(680, 150), new Vector2(0, -110));
            body.lineSpacing = 12f;

            // ── 허용 버튼(카드 아래) ──
            var btn = AddButton(root, "AllowButton", Primary, "카메라 권한 허용", bold, 26, Color.white, out _);
            btn.image.sprite = Pill(); // 둥근 버튼(AddButton 기본 스프라이트가 각져서 교체)
            Center((RectTransform)btn.transform, new Vector2(520, 96), new Vector2(0, -340));

            Wire(screen, "_requestButton", btn);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[CameraPermissionBuilder] 생성 완료. 씬 저장(Ctrl+S) 필요.");
        }
    }
}
