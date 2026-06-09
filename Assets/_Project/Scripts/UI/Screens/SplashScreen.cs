using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 스플래시 화면(61.png). 로고·부제와 함께 "모델 준비 중" 진행바를 표시한다.
    /// 전환 흐름과 진행도 갱신은 AppBootstrap이 구동하며, 이 패널은 표시만 담당.
    /// </summary>
    public class SplashScreen : UIScreen
    {
        [Tooltip("진행바 채움 이미지. 좌측 앵커 폭(anchorMax.x)으로 진행도를 표현한다.")]
        [SerializeField] private Image _progressFill;

        [Tooltip("진행 상태 안내 텍스트(\"수어 인식 모델 준비 중\").")]
        [SerializeField] private TMP_Text _statusText;

        /// <summary>진행도 0~1을 진행바에 반영한다.</summary>
        public void SetProgress(float value01)
        {
            if (_progressFill == null) return;
            RectTransform rt = _progressFill.rectTransform;
            Vector2 max = rt.anchorMax;
            max.x = Mathf.Clamp01(value01); // 좌측 앵커 폭으로 채움(Filled 모서리 아티팩트 회피)
            rt.anchorMax = max;
        }

        /// <summary>상태 안내 문구를 설정한다.</summary>
        public void SetStatus(string text)
        {
            if (_statusText != null)
                _statusText.text = text;
        }
    }
}
