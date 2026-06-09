using System;
using System.Collections;
using UnityEngine;
using Artti.SignBridge.Data;

namespace Artti.SignBridge.Speech
{
    /// <summary>
    /// 온디바이스 네이티브 TTS(plan §3·§7). Android <c>android.speech.tts.TextToSpeech</c>를
    /// 래핑해 한국어 문장을 음성 출력한다. 에디터/비안드로이드에서는 시간 기반 시뮬로 폴백한다.
    ///
    /// 변환 문장(파이프라인) · 음성 출력 속도 테스트(설정 화면) 공용 진입점.
    /// 속도/볼륨은 <see cref="AppSettings"/>(TtsRate·TtsVolume)를 매 발화 시 반영한다.
    /// 발화 시작/종료를 이벤트로 노출해 UI가 스피커 아이콘 점등 등에 사용한다(90·91.png).
    ///
    /// _App(부트스트랩) 오브젝트에 컴포넌트로 배치한다(SbUiBuild.EnsureBootstrap이 보장).
    /// </summary>
    public class TtsService : MonoBehaviour
    {
        public static TtsService Instance { get; private set; }

        /// <summary>발화가 막 시작됨(아이콘 primary 점등 구독용).</summary>
        public event Action SpeakingStarted;

        /// <summary>발화가 끝남(정지·완료 포함).</summary>
        public event Action SpeakingFinished;

        /// <summary>현재 음성 출력 중인지.</summary>
        public bool IsSpeaking { get; private set; }

        /// <summary>엔진 사용 준비 완료 여부(Android init 성공). 에디터에서는 항상 true.</summary>
        public bool IsReady { get; private set; }

        // 출력 후 아이콘 점등이 너무 짧아 깜빡이지 않도록 최소 점등 시간.
        const float MinSpeakingSeconds = 0.4f;

        Coroutine _speakCo;

#if UNITY_ANDROID && !UNITY_EDITOR
        const int QueueFlush = 0;          // TextToSpeech.QUEUE_FLUSH
        const int InitPending = -99;
        const int InitSuccess = 0;         // TextToSpeech.SUCCESS

        AndroidJavaObject _tts;
        volatile int _initStatus = InitPending;

        /// <summary>android.speech.tts.TextToSpeech$OnInitListener 프록시.</summary>
        class InitListener : AndroidJavaProxy
        {
            readonly TtsService _svc;
            public InitListener(TtsService svc)
                : base("android.speech.tts.TextToSpeech$OnInitListener") { _svc = svc; }
            // Java가 호출(메인 스레드). 상태만 저장하고 설정은 코루틴에서 수행.
            void onInit(int status) { _svc._initStatus = status; }
        }
#endif

        private void Awake()
        {
            // Reload Domain 비활성 — static 명시적 초기화(CLAUDE.md).
            Instance = this;
            IsSpeaking = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            IsReady = false;
            StartCoroutine(InitAndroid());
#else
            IsReady = true; // 에디터: 시뮬 사용
#endif
        }

        private void OnDestroy()
        {
            Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts != null)
            {
                try { _tts.Call("shutdown"); } catch { /* 종료 중 무시 */ }
                _tts.Dispose();
                _tts = null;
            }
#endif
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Stop(); // 백그라운드 진입 시 발화 중단
        }

        /// <summary>한국어 문장을 음성 출력한다. 진행 중인 발화는 끊고 새로 시작(QUEUE_FLUSH).</summary>
        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Stop();
            _speakCo = StartCoroutine(SpeakRoutine(text));
        }

        /// <summary>현재 발화를 즉시 중단한다.</summary>
        public void Stop()
        {
            if (_speakCo != null) { StopCoroutine(_speakCo); _speakCo = null; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_tts != null) { try { _tts.Call<int>("stop"); } catch { } }
#endif
            RaiseFinished();
        }

        // ── 발화 루틴(플랫폼 분기) ────────────────────────────────────────

#if UNITY_ANDROID && !UNITY_EDITOR
        IEnumerator InitAndroid()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new InitListener(this));
            }

            // onInit 콜백 대기(최대 5초).
            float t = 0f;
            while (_initStatus == InitPending && t < 5f) { t += Time.deltaTime; yield return null; }

            if (_initStatus == InitSuccess)
            {
                try
                {
                    using (var locale = new AndroidJavaObject("java.util.Locale", "ko", "KR"))
                        _tts.Call<int>("setLanguage", locale);
                    IsReady = true;
                }
                catch (Exception e) { Debug.LogError($"[TtsService] setLanguage 실패: {e.Message}"); }
            }
            else
            {
                Debug.LogError($"[TtsService] TTS 초기화 실패(status={_initStatus}).");
            }
        }

        IEnumerator SpeakRoutine(string text)
        {
            // 준비 전이면 잠깐 대기(최대 2초).
            float wait = 0f;
            while (!IsReady && wait < 2f) { wait += Time.deltaTime; yield return null; }
            if (!IsReady || _tts == null) { RaiseFinished(); yield break; }

            try
            {
                _tts.Call<int>("setSpeechRate", Mathf.Clamp(AppSettings.TtsRate, 0.1f, 3f));
                using (var bundle = new AndroidJavaObject("android.os.Bundle"))
                {
                    bundle.Call("putFloat", "volume", Mathf.Clamp01(AppSettings.TtsVolume)); // KEY_PARAM_VOLUME
                    _tts.Call<int>("speak", text, QueueFlush, bundle, "sb_tts");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TtsService] speak 실패: {e.Message}");
                RaiseFinished();
                yield break;
            }

            RaiseStarted();

            // 엔진이 시작할 여유를 준 뒤 isSpeaking()이 false로 떨어지는 시점을 폴링.
            float elapsed = 0f;
            yield return new WaitForSeconds(0.2f); elapsed += 0.2f;
            while (true)
            {
                bool speaking;
                try { speaking = _tts.Call<bool>("isSpeaking"); }
                catch { speaking = false; }
                if (!speaking && elapsed >= MinSpeakingSeconds) break;
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            _speakCo = null;
            RaiseFinished();
        }
#else
        IEnumerator SpeakRoutine(string text)
        {
            // 에디터/비안드로이드: 길이·배율에 비례한 점등 시뮬(실제 음성 없음).
            RaiseStarted();
            float rate = Mathf.Max(0.1f, AppSettings.TtsRate);
            float seconds = Mathf.Clamp(text.Length * 0.12f / rate, MinSpeakingSeconds, 6f);
            yield return new WaitForSeconds(seconds);
            _speakCo = null;
            RaiseFinished();
        }
#endif

        void RaiseStarted()
        {
            if (IsSpeaking) return;
            IsSpeaking = true;
            SpeakingStarted?.Invoke();
        }

        void RaiseFinished()
        {
            if (!IsSpeaking) return;
            IsSpeaking = false;
            SpeakingFinished?.Invoke();
        }
    }
}
