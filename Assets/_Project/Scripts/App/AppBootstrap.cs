using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using Artti.SignBridge.UI;

namespace Artti.SignBridge.App
{
    /// <summary>
    /// 앱 진입점. 스플래시 → 카메라 권한 확인 → 홈 순으로 초기 흐름을 구동한다(plan §7).
    /// 씬의 부트스트랩 오브젝트에 ScreenManager·SessionController와 함께 배치한다.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Tooltip("스플래시 최소 표시 시간(초). 모델 준비가 더 빨라도 이 시간은 보여준다.")]
        [SerializeField] private float _splashSeconds = 2.0f;

        [Tooltip("스플래시 진행 안내 문구.")]
        [SerializeField] private string _loadingStatus = "수어 인식 모델 준비 중";

        /// <summary>스플래시의 모델 준비(현재 시뮬) 완료 여부. 홈 준비상태 초록불 판정에 사용.</summary>
        public static bool ModelReady { get; private set; }

        private void Awake()
        {
            // Reload Domain 비활성 환경 — static 명시적 초기화(CLAUDE.md).
            ModelReady = false;
        }

        private IEnumerator Start()
        {
            ScreenManager sm = ScreenManager.Instance;
            if (sm == null)
            {
                Debug.LogError("[AppBootstrap] ScreenManager가 씬에 없습니다.");
                yield break;
            }

            sm.Show(AppScreen.Splash, addToHistory: false);

            SplashScreen splash = sm.Get<SplashScreen>(AppScreen.Splash);
            splash?.SetStatus(_loadingStatus);

            // 모델 준비 진행바 구동. 현재는 시간 기반 시뮬레이션이며,
            // 파이프라인 단계에서 실제 Sentis ONNX 로드 진행도로 교체한다.
            float t = 0f;
            while (t < _splashSeconds)
            {
                t += Time.deltaTime;
                splash?.SetProgress(t / _splashSeconds);
                yield return null;
            }
            splash?.SetProgress(1f);
            ModelReady = true;

            sm.Show(HasCameraPermission() ? AppScreen.Home : AppScreen.CameraPermission,
                    addToHistory: false);
        }

        /// <summary>카메라 권한 보유 여부. 에디터에서는 항상 true로 흐름을 진행시킨다.</summary>
        public static bool HasCameraPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
            return true;
#endif
        }
    }
}
