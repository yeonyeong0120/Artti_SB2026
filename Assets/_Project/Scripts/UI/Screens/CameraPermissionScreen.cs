using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using Artti.SignBridge.App;

namespace Artti.SignBridge.UI
{
    /// <summary>카메라 권한 요청 화면. 허용 시 홈으로 진입한다(plan §7).</summary>
    public class CameraPermissionScreen : UIScreen
    {
        [SerializeField] private Button _requestButton;

        private void Awake()
        {
            if (_requestButton != null)
                _requestButton.onClick.AddListener(RequestPermission);
        }

        public override void OnShow()
        {
            // 이미 허용돼 있으면 곧바로 홈으로.
            if (AppBootstrap.HasCameraPermission())
                ScreenManager.Instance.Show(AppScreen.Home, addToHistory: false);
        }

        private void RequestPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ =>
                ScreenManager.Instance.Show(AppScreen.Home, addToHistory: false);
            callbacks.PermissionDenied += _ =>
            {
                // 카메라 없이는 핵심 기능이 불가하므로 앱을 종료한다(62.png 스펙).
                Debug.LogWarning("[CameraPermission] 카메라 권한 거부 — 앱을 종료합니다.");
                Application.Quit();
            };
            Permission.RequestUserPermission(Permission.Camera, callbacks);
#else
            ScreenManager.Instance.Show(AppScreen.Home, addToHistory: false);
#endif
        }
    }
}
