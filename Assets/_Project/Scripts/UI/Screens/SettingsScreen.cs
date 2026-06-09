using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Artti.SignBridge.Data;
using Artti.SignBridge.Speech;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 환경 설정(90·91.png). 단일 화면 — 항목별 페이지 전환 없음.
    ///  · 음성 출력 속도 슬라이더(tick 임의) + 음성 출력 속도 테스트(미리듣기 중 스피커 아이콘 primary)
    ///  · 전체 기록 삭제(미들 모달로 한 번 더 확인 후 SessionStore.ClearAll)
    ///  · 팀 ARTTI에 문의하기(OS 기본 메일 작성 화면 mailto 호출 — 앱이 직접 전송하지 않음)
    /// 비주얼은 SettingsSceneBuilder가 구성하고 SerializeField를 배선한다.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        [Header("헤더")]
        [SerializeField] private Button _backButton;

        [Header("음성 설정")]
        [Tooltip("TTS 출력 속도. 정수 tick(0..N) → TtsRateStops 배율로 매핑.")]
        [SerializeField] private Slider _ttsRateSlider;
        [Tooltip("음성 출력 속도 테스트 트리거(샘플 문장 미리듣기).")]
        [SerializeField] private Button _ttsTestButton;
        [Tooltip("TTS 출력 중 primary color로 점등되는 스피커 아이콘(Image/MaterialSymbol 등 Graphic).")]
        [SerializeField] private Graphic _ttsSpeakerIcon;

        [Header("데이터 · 개인정보")]
        [SerializeField] private Button _deleteAllButton;
        [Tooltip("삭제 확인 미들 모달(라이트박스 포함). 기본 숨김.")]
        [SerializeField] private GameObject _deleteModal;
        [SerializeField] private Button _deleteConfirmButton;
        [SerializeField] private Button _deleteCancelButton;
        [SerializeField] private Button _deleteCloseButton;

        [Header("문의")]
        [SerializeField] private Button _inquiryButton;

        [Header("TTS 아이콘 색")]
        [SerializeField] private Color _iconIdle = new Color(0.49f, 0.53f, 0.58f);   // #7E8795
        [SerializeField] private Color _iconActive = new Color(0.30f, 0.55f, 0.88f); // #4C8DE0 primary

        /// <summary>음성 출력 속도 tick 값(임의 설정). 슬라이더 정수 인덱스 → 배율.</summary>
        static readonly float[] TtsRateStops = { 0.75f, 1.0f, 1.25f, 1.5f, 2.0f };

        /// <summary>문의 수신처(팀 ARTTI 공용 메일). 실제 운영 주소로 교체할 것.</summary>
        const string InquiryEmail = "team.artti.sb@gmail.com";

        const string TtsSampleText = "안녕하세요, SignBridge입니다!";

        Coroutine _previewCo;

        private void Awake()
        {
            if (_backButton != null) _backButton.onClick.AddListener(() => ScreenManager.Instance.Back());

            if (_ttsRateSlider != null)
            {
                _ttsRateSlider.wholeNumbers = true;
                _ttsRateSlider.minValue = 0;
                _ttsRateSlider.maxValue = TtsRateStops.Length - 1;
                _ttsRateSlider.onValueChanged.AddListener(OnRateChanged);
            }
            if (_ttsTestButton != null)     _ttsTestButton.onClick.AddListener(PlayTtsPreview);
            if (_deleteAllButton != null)   _deleteAllButton.onClick.AddListener(() => ToggleDeleteModal(true));
            if (_deleteCancelButton != null) _deleteCancelButton.onClick.AddListener(() => ToggleDeleteModal(false));
            if (_deleteCloseButton != null)  _deleteCloseButton.onClick.AddListener(() => ToggleDeleteModal(false));
            if (_deleteConfirmButton != null) _deleteConfirmButton.onClick.AddListener(ConfirmDeleteAll);
            if (_inquiryButton != null)      _inquiryButton.onClick.AddListener(OpenInquiryMail);

            SetIconActive(false);
            ToggleDeleteModal(false);
        }

        public override void OnShow()
        {
            // 저장된 배율을 가장 가까운 tick으로 표시(콜백 미발생).
            if (_ttsRateSlider != null)
                _ttsRateSlider.SetValueWithoutNotify(NearestStopIndex(AppSettings.TtsRate));

            // 실제 TTS 발화 시작/종료에 맞춰 스피커 아이콘을 점등한다.
            var tts = TtsService.Instance;
            if (tts != null)
            {
                tts.SpeakingStarted += OnTtsStarted;
                tts.SpeakingFinished += OnTtsFinished;
                SetIconActive(tts.IsSpeaking);
            }
            else SetIconActive(false);

            ToggleDeleteModal(false);
        }

        public override void OnHide()
        {
            var tts = TtsService.Instance;
            if (tts != null)
            {
                tts.SpeakingStarted -= OnTtsStarted;
                tts.SpeakingFinished -= OnTtsFinished;
                tts.Stop();
            }
            StopPreview();
        }

        void OnTtsStarted()  => SetIconActive(true);
        void OnTtsFinished() => SetIconActive(false);

        // ── 음성 출력 속도 ────────────────────────────────────────────────
        void OnRateChanged(float idx)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(idx), 0, TtsRateStops.Length - 1);
            AppSettings.TtsRate = TtsRateStops[i];
        }

        static int NearestStopIndex(float rate)
        {
            int best = 0;
            float bestDiff = float.MaxValue;
            for (int i = 0; i < TtsRateStops.Length; i++)
            {
                float d = Mathf.Abs(TtsRateStops[i] - rate);
                if (d < bestDiff) { bestDiff = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// 음성 출력 속도 테스트. 네이티브 TTS로 샘플 문장을 발화하고, 출력 중에는
        /// 스피커 아이콘이 primary로 점등된다(점등은 TtsService 시작/종료 이벤트가 구동).
        /// TtsService가 없을 때만(구 씬 등) 길이·배율 비례 시뮬로 폴백한다.
        /// </summary>
        public void PlayTtsPreview()
        {
            var tts = TtsService.Instance;
            if (tts != null) { tts.Speak(TtsSampleText); return; }

            // 폴백(서비스 미부착): 점등만 시뮬.
            StopPreview();
            if (isActiveAndEnabled)
                _previewCo = StartCoroutine(PreviewRoutine());
        }

        IEnumerator PreviewRoutine()
        {
            SetIconActive(true);
            float rate = Mathf.Max(0.1f, AppSettings.TtsRate);
            float seconds = Mathf.Clamp(TtsSampleText.Length * 0.12f / rate, 0.6f, 4f);
            yield return new WaitForSeconds(seconds);
            SetIconActive(false);
            _previewCo = null;
        }

        void StopPreview()
        {
            if (_previewCo != null) { StopCoroutine(_previewCo); _previewCo = null; }
            SetIconActive(false);
        }

        void SetIconActive(bool active)
        {
            if (_ttsSpeakerIcon != null) _ttsSpeakerIcon.color = active ? _iconActive : _iconIdle;
        }

        // ── 전체 기록 삭제 ────────────────────────────────────────────────
        void ToggleDeleteModal(bool show)
        {
            if (_deleteModal != null) _deleteModal.SetActive(show);
        }

        void ConfirmDeleteAll()
        {
            SessionStore.ClearAll();
            ToggleDeleteModal(false);
            // TODO(선택): 삭제 완료 토스트/스낵바 노출.
        }

        // ── 문의하기(mailto) ──────────────────────────────────────────────
        /// <summary>OS 기본 메일 작성 화면을 연다(mailto). 수신자·제목·본문 자동 입력, 앱은 직접 전송하지 않음.</summary>
        void OpenInquiryMail()
        {
            const string subject = "[SignBridge] 문의하기";

            var body = new StringBuilder();
            body.Append("문의하실 내용을 적어주세요.\n\n");
            body.Append("──────────\n");
            body.Append("앱 버전: SignBridge ").Append(Application.version).Append('\n');
            body.Append("기기: ").Append(SystemInfo.deviceModel).Append('\n');
            body.Append("OS: ").Append(SystemInfo.operatingSystem).Append('\n');

            string url = "mailto:" + InquiryEmail
                + "?subject=" + Uri.EscapeDataString(subject)
                + "&body=" + Uri.EscapeDataString(body.ToString());
            Application.OpenURL(url);
        }
    }
}
