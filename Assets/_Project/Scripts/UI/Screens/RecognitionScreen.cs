using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Artti.SignBridge.App;
using Artti.SignBridge.Data;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 수업 인식(메인 AR, SCR-100) 화면. AR 카메라 피드 위에 말풍선·자막·인식 인디케이터가 얹히며,
    /// 우상단 메뉴로 세션 로그 진입 / 종료, TTS 항상 켜기 토글을 제공한다(plan §7).
    /// AR 피드·키포인트 오버레이·말풍선 연동은 파이프라인 단계에서 추가한다.
    /// </summary>
    public class RecognitionScreen : UIScreen
    {
        [Header("우상단 메뉴")]
        [SerializeField] private Button _sessionLogButton;
        [SerializeField] private Button _endButton;

        [Header("TTS 항상 켜기 토글(활성 시 강조)")]
        [SerializeField] private Button _ttsToggleButton;
        [SerializeField] private Graphic _ttsButtonBg;
        [SerializeField] private TMP_Text _ttsLabel;

        static readonly Color TtsOn  = new Color(0.30f, 0.55f, 0.88f); // 강조 색
        static readonly Color TtsOff = new Color(0.40f, 0.44f, 0.50f); // 비활성

        private void Awake()
        {
            if (_sessionLogButton != null) _sessionLogButton.onClick.AddListener(() => ScreenManager.Instance.Show(AppScreen.SessionLog));
            if (_endButton != null) _endButton.onClick.AddListener(EndClass);
            if (_ttsToggleButton != null) _ttsToggleButton.onClick.AddListener(ToggleTts);
        }

        public override void OnShow() => RefreshTts();

        private void EndClass()
        {
            // 세션 종료(저장)·파이프라인 정지 → 홈 복귀. 인식 중 쌓인 백스택은 초기화.
            SessionController.Instance.EndSession();
            ScreenManager.Instance.ClearHistory();
            ScreenManager.Instance.Show(AppScreen.Home, addToHistory: false);
        }

        private void ToggleTts()
        {
            AppSettings.TtsAlwaysOn = !AppSettings.TtsAlwaysOn;
            RefreshTts();
            // TODO(파이프라인): 전환 시 토스트 안내(plan §7).
        }

        /// <summary>TTS 항상 켜기 상태를 토글 버튼 색·문구에 반영(활성 시 강조 색상, plan §7).</summary>
        private void RefreshTts()
        {
            bool on = AppSettings.TtsAlwaysOn;
            if (_ttsButtonBg != null) _ttsButtonBg.color = on ? TtsOn : TtsOff;
            if (_ttsLabel != null) _ttsLabel.text = on ? "TTS 음성 켜짐" : "TTS 음성 꺼짐";
        }
    }
}
