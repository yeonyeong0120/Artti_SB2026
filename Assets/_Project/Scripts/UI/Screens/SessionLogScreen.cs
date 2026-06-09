using UnityEngine;
using UnityEngine.UI;
using Artti.SignBridge.App;
using Artti.SignBridge.Data;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 세션 로그(수업 중). 현재 진행 중인 세션의 발화만 시간순으로 표시한다(plan §7).
    /// </summary>
    public class SessionLogScreen : UIScreen
    {
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(() => ScreenManager.Instance.Back());
        }

        public override void OnShow()
        {
            ClassSession current = SessionController.Instance != null ? SessionController.Instance.Current : null;
            if (current == null) return;

            // TODO(PNG 수령 후): current.utterances를 리스트 UI에 바인딩.
            //   각 항목 = 원본 수어 단어 나열 + Gemini 변환 문장.
            //   hasUnrecognized=true 항목은 "?" 아이콘 별도 표시(plan §7).
        }
    }
}
