using UnityEngine;
using TMPro;

// 안드로이드 TTS(음성 합성) 래퍼.
//   - Toggle(): 켜짐/꺼짐 전환 (버튼에 연결). 버튼 글씨도 바꿔줌.
//   - Speak(text): 켜져 있을 때만 한국어로 읽음.
public class KslTts : MonoBehaviour
{
    [Header("켜짐/꺼짐 상태")]
    public bool ttsOn = false;

    [Header("(선택) 토글 버튼 글씨 — 켜짐/꺼짐 표시")]
    public TMP_Text toggleLabel;
    public string onText = "TTS 음성 켜짐";
    public string offText = "TTS 음성 꺼짐";

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject _tts;
    bool _ready = false;

    class InitListener : AndroidJavaProxy
    {
        readonly KslTts _o;
        public InitListener(KslTts o) : base("android.speech.tts.TextToSpeech$OnInitListener") { _o = o; }
        void onInit(int status) { _o.OnTtsInit(status == 0); }   // SUCCESS == 0
    }

    void Start()
    {
        try
        {
            var act = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                          .GetStatic<AndroidJavaObject>("currentActivity");
            _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", act, new InitListener(this));
        }
        catch (System.Exception e) { Debug.LogError("[TTS] init FAIL: " + e.Message); }
    }

    public void OnTtsInit(bool ok)
    {
        _ready = ok;
        if (!ok) { Debug.LogError("[TTS] OnInit FAIL (status!=0)"); return; }
        try
        {
            var ko = new AndroidJavaObject("java.util.Locale", "ko", "KR");
            int langRes = _tts.Call<int>("setLanguage", ko);
            Debug.Log("[TTS] READY (ko-KR), langResult=" + langRes);
        }
        catch (System.Exception e) { Debug.LogError("[TTS] setLanguage FAIL: " + e.Message); }
    }

    public void Speak(string text)
    {
        Debug.Log($"[TTS] Speak() enter: ttsOn={ttsOn} ready={_ready} ttsNull={_tts==null} text=\"{text}\"");
        if (!ttsOn) { Debug.Log("[TTS] skip: ttsOn=false"); return; }
        if (string.IsNullOrEmpty(text)) { Debug.Log("[TTS] skip: empty text"); return; }
        if (_tts == null || !_ready) { Debug.LogWarning("[TTS] skip: not ready"); return; }
        try { var b = new AndroidJavaObject("android.os.Bundle"); int r = _tts.Call<int>("speak", text, 0, b, "kslutt"); Debug.Log("[TTS] speak() returned " + r); }   // 0=QUEUE_FLUSH, 결과 0이면 성공
        catch (System.Exception e) { Debug.LogError("[TTS] speak FAIL: " + e.Message); }
    }

    void OnDestroy() { try { _tts?.Call("shutdown"); } catch { } }
#else
    // 에디터/비안드로이드: 로그만
    public void Speak(string text)
    {
        if (!ttsOn || string.IsNullOrEmpty(text)) return;
        Debug.Log("[TTS] (에디터) 읽기: " + text);
    }
#endif

    // TTS 버튼에 연결: 누를 때마다 켜짐/꺼짐 전환
    public void Toggle()
    {
        ttsOn = !ttsOn;
        if (toggleLabel != null) toggleLabel.text = ttsOn ? onText : offText;
        Debug.Log("[TTS] toggled ttsOn=" + ttsOn);
    }
}