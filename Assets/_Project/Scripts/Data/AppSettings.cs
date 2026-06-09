using UnityEngine;

namespace Artti.SignBridge.Data
{
    /// <summary>Gemini 환각 통제 모드(plan §6·§7).</summary>
    public enum HallucinationControlMode { Strict = 0, Relaxed = 1 }

    /// <summary>
    /// 앱 전역 설정. 변경 즉시 PlayerPrefs에 영속화한다(plan §7 설정 화면 항목).
    /// </summary>
    public static class AppSettings
    {
        private const string K_Threshold = "sb.threshold";
        private const string K_TtsRate = "sb.tts.rate";
        private const string K_TtsVolume = "sb.tts.volume";
        private const string K_TtsAlwaysOn = "sb.tts.alwaysOn";
        private const string K_GeminiEnabled = "sb.gemini.enabled";
        private const string K_HallucMode = "sb.gemini.hallucMode";
        private const string K_ConsentOnly = "sb.consentOnly";

        /// <summary>기본 신뢰도 임계값(plan §6 = 0.7).</summary>
        public const float DefaultThreshold = 0.7f;
        public const float MinThreshold = 0.5f;
        public const float MaxThreshold = 0.9f;

        /// <summary>인식 신뢰도 임계값(0.5~0.9). 미달 시 "?" 토큰 처리(plan §6).</summary>
        public static float ConfidenceThreshold
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(K_Threshold, DefaultThreshold), MinThreshold, MaxThreshold);
            set { PlayerPrefs.SetFloat(K_Threshold, Mathf.Clamp(value, MinThreshold, MaxThreshold)); PlayerPrefs.Save(); }
        }

        /// <summary>TTS 말하기 속도 배율.</summary>
        public static float TtsRate
        {
            get => PlayerPrefs.GetFloat(K_TtsRate, 1.0f);
            set { PlayerPrefs.SetFloat(K_TtsRate, value); PlayerPrefs.Save(); }
        }

        /// <summary>TTS 볼륨(0~1).</summary>
        public static float TtsVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(K_TtsVolume, 1.0f));
            set { PlayerPrefs.SetFloat(K_TtsVolume, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        /// <summary>TTS 항상 켜기. 변환 문장 자동 음성 출력(plan §7).</summary>
        public static bool TtsAlwaysOn
        {
            get => PlayerPrefs.GetInt(K_TtsAlwaysOn, 1) == 1;
            set { PlayerPrefs.SetInt(K_TtsAlwaysOn, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Gemini 문장 변환 on/off(plan §7).</summary>
        public static bool GeminiEnabled
        {
            get => PlayerPrefs.GetInt(K_GeminiEnabled, 1) == 1;
            set { PlayerPrefs.SetInt(K_GeminiEnabled, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Gemini 환각 통제 모드(엄격/완화)(plan §7).</summary>
        public static HallucinationControlMode HallucinationMode
        {
            get => (HallucinationControlMode)PlayerPrefs.GetInt(K_HallucMode, (int)HallucinationControlMode.Strict);
            set { PlayerPrefs.SetInt(K_HallucMode, (int)value); PlayerPrefs.Save(); }
        }

        /// <summary>"동의된 학생만 인식" 토글(plan §2 개인정보 동의·§7).</summary>
        public static bool ConsentedStudentsOnly
        {
            get => PlayerPrefs.GetInt(K_ConsentOnly, 0) == 1;
            set { PlayerPrefs.SetInt(K_ConsentOnly, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
