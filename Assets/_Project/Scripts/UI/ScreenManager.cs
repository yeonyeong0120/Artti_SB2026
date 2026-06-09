using System.Collections.Generic;
using UnityEngine;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 단일 씬 패널 기반 화면 전환 관리자. 등록된 UIScreen 패널을 켜고 끄며
    /// 뒤로가기용 백스택을 유지한다(plan §7 화면 전환 흐름).
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Tooltip("씬에 배치된 모든 화면 패널. Inspector에서 등록한다.")]
        [SerializeField] private List<UIScreen> _screens = new List<UIScreen>();

        private readonly Dictionary<AppScreen, UIScreen> _lookup = new Dictionary<AppScreen, UIScreen>();
        private readonly Stack<AppScreen> _history = new Stack<AppScreen>();

        /// <summary>현재 표시 중인 화면.</summary>
        public AppScreen Current { get; private set; }

        private void Awake()
        {
            // Reload Domain 비활성 환경 — static 상태 명시적 초기화(CLAUDE.md).
            Instance = this;
            _lookup.Clear();
            _history.Clear();
            Current = default;

            foreach (UIScreen screen in _screens)
            {
                if (screen == null) continue;
                _lookup[screen.Screen] = screen;
                screen.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 지정 화면으로 전환한다.
        /// addToHistory=false면 백스택에 쌓지 않는다(스플래시·세션 종료 후 홈 복귀 등).
        /// </summary>
        public void Show(AppScreen target, bool addToHistory = true)
        {
            if (!_lookup.TryGetValue(target, out UIScreen next) || next == null)
            {
                Debug.LogError($"[ScreenManager] 등록되지 않은 화면: {target}");
                return;
            }

            if (_lookup.TryGetValue(Current, out UIScreen prev) && prev != null && prev.gameObject.activeSelf)
            {
                prev.OnHide();
                prev.gameObject.SetActive(false);
                if (addToHistory) _history.Push(Current);
            }

            Current = target;
            next.gameObject.SetActive(true);
            next.OnShow();
        }

        /// <summary>백스택 한 단계 뒤로. 비어 있으면 false.</summary>
        public bool Back()
        {
            if (_history.Count == 0) return false;
            AppScreen prev = _history.Pop();
            Show(prev, addToHistory: false);
            return true;
        }

        /// <summary>백스택 초기화(세션 종료 후 홈 복귀 시 등).</summary>
        public void ClearHistory() => _history.Clear();

        /// <summary>등록된 화면 패널을 가져온다(없으면 null). 컨트롤러 직접 접근용.</summary>
        public T Get<T>(AppScreen screen) where T : UIScreen
        {
            return _lookup.TryGetValue(screen, out UIScreen s) ? s as T : null;
        }
    }
}
