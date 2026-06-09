using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Artti.SignBridge.Data;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 전체 수업 로그(홈 진입). 저장된 모든 세션을 날짜순으로 조회한다(plan §7).
    /// </summary>
    public class AllLogsScreen : UIScreen
    {
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(() => ScreenManager.Instance.Back());
        }

        public override void OnShow()
        {
            List<ClassSession> sessions = SessionStore.LoadAll();
            // TODO(PNG 수령 후): sessions를 날짜순 목록 UI에 바인딩.
            //   세션 선택 시 해당 수업의 발화 조회 + 텍스트 내보내기(plan §7).
        }
    }
}
