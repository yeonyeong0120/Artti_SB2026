using UnityEngine;
using UnityEngine.UI;
using Artti.SignBridge.App;

namespace Artti.SignBridge.UI
{
    /// <summary>홈(대시보드). 수업 시작·전체 수업 로그·설정으로 분기하는 허브(plan §7).</summary>
    public class HomeScreen : UIScreen
    {
        [Header("준비 상태 인디케이터(조건 성립 시에만 초록불, 63.png)")]
        [Tooltip("수어 모델 로드 상태 체크 아이콘. 색만 토글한다.")]
        [SerializeField] private Image _modelStatusIcon;
        [Tooltip("필수 카메라 권한 상태 체크 아이콘. 색만 토글한다.")]
        [SerializeField] private Image _cameraStatusIcon;

        [Header("사용 안내(미들 모달)")]
        [SerializeField] private Button _guideButton;
        [SerializeField] private GameObject _guideModal;
        [SerializeField] private Button _guideCloseButton;

        [Header("주요 동작")]
        [SerializeField] private Button _startClassButton;
        [SerializeField] private Button _allLogsButton;
        [SerializeField] private Button _settingsButton;

        // 준비 상태 색(63.png): 충족=초록, 미충족=회색.
        static readonly Color Ready   = new Color(0.13f, 0.77f, 0.37f); // #22C55E
        static readonly Color Pending = new Color(0.78f, 0.80f, 0.84f); // #C7CDD6

        private void Awake()
        {
            if (_startClassButton != null) _startClassButton.onClick.AddListener(StartClass);
            if (_allLogsButton != null) _allLogsButton.onClick.AddListener(() => ScreenManager.Instance.Show(AppScreen.AllLogs));
            if (_settingsButton != null) _settingsButton.onClick.AddListener(() => ScreenManager.Instance.Show(AppScreen.Settings));

            if (_guideButton != null) _guideButton.onClick.AddListener(() => ToggleGuide(true));
            if (_guideCloseButton != null) _guideCloseButton.onClick.AddListener(() => ToggleGuide(false));
            ToggleGuide(false);
        }

        public override void OnShow()
        {
            RefreshReadyState();
        }

        /// <summary>수어 모델 로드·필수 카메라 권한 상태를 인디케이터에 반영. 조건 성립 시에만 초록.</summary>
        private void RefreshReadyState()
        {
            if (_modelStatusIcon != null)
                _modelStatusIcon.color = AppBootstrap.ModelReady ? Ready : Pending;
            if (_cameraStatusIcon != null)
                _cameraStatusIcon.color = AppBootstrap.HasCameraPermission() ? Ready : Pending;
        }

        private void ToggleGuide(bool show)
        {
            if (_guideModal != null) _guideModal.SetActive(show);
        }

        private void StartClass()
        {
            // 세션 시작 → 파이프라인 기동(SessionController가 이벤트 발행) → 인식 화면(SCR-100) 진입.
            SessionController.Instance.StartSession();
            ScreenManager.Instance.Show(AppScreen.Recognition);
        }
    }
}
