using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

// 인식 결과 → 누적 단어 + 문장 생성.
//   규칙 A: 동사(싫다 등)가 인식되면 자동으로 한 문장 완성.
//   문장 규칙: 연속중복 제거 + 동사 앞 명사에 조사(이/가) + 동사 활용(싫다→싫어요).
public class KslUiBinder : MonoBehaviour
{
    [Header("연결: 인식기")]
    public KslRecognizer recognizer;

    [Header("연결: 앱 UI")]
    public TMP_Text bufferLabel;    // 누적 단어
    public TMP_Text sentenceLabel;  // 최근 변환 (문장)
    public TMP_Text stateLabel;     // 상태 글씨
    public Image stateDot;       // 상태 점

    [Header("(선택) TTS")]
    public KslTts tts;

    [Header("표시")]
    public int maxWords = 20;
    public string bufferPrefix = "누적 단어: ";

    [Header("상태 색")]
    public Color detectingColor = new Color(0.6f, 0.6f, 0.6f);
    public Color uncertainColor = new Color(1f, 0.65f, 0f);
    public Color confirmedColor = new Color(0.2f, 0.8f, 0.3f);

    // 동사 활용표 (필요하면 추가)
    static readonly Dictionary<string, string> VERBS = new Dictionary<string, string>
    {
        { "싫다", "싫어요" }, { "좋다", "좋아요" }, { "가다", "가요" }, { "오다", "와요" },
    };

    readonly List<string> _words = new List<string>();
    string _lastSentence = "";

    void OnEnable()
    {
        if (recognizer == null) { Debug.LogError("[UiBinder] recognizer 미연결"); return; }
        recognizer.OnWordConfirmed += OnConfirmed;
        recognizer.OnStateChanged += OnState;
        UpdateBuffer();
    }
    void OnDisable()
    {
        if (recognizer == null) return;
        recognizer.OnWordConfirmed -= OnConfirmed;
        recognizer.OnStateChanged -= OnState;
    }

    void OnConfirmed(int cls, string word, float prob)
    {
        // 이미 버퍼에 있는 단어면 추가 안 함 (월요일 아침 월요일 → 월요일 아침)
        if (!_words.Contains(word))
            _words.Add(word);
        while (_words.Count > maxWords) _words.RemoveAt(0);
        UpdateBuffer();

        // 규칙 A: 동사가 나오면 문장 완성
        if (VERBS.ContainsKey(word))
            GenerateSentence();
    }

    // '문장 생성' 버튼 / 동사 자동완성에서 호출
    public void GenerateSentence()
    {
        _lastSentence = BuildSentence(_words);
        if (sentenceLabel != null) sentenceLabel.text = _lastSentence;
        _words.Clear();
        UpdateBuffer();
    }

    // 문장(최근 변환)을 탭하면 호출 → TTS 켜져 있으면 말함
    public void SpeakSentence()
    {
        string s = (sentenceLabel != null && !string.IsNullOrEmpty(sentenceLabel.text))
                   ? sentenceLabel.text : _lastSentence;
        Debug.Log($"[KSL] SpeakSentence 호출됨. 문장='{s}', tts={(tts != null)}");
        if (tts != null) tts.Speak(s);
    }

    public void ClearWords()
    {
        _words.Clear(); _lastSentence = "";
        if (sentenceLabel != null) sentenceLabel.text = "";
        UpdateBuffer();
    }

    void UpdateBuffer()
    {
        if (bufferLabel != null)
            bufferLabel.text = bufferPrefix + (_words.Count == 0 ? "(없음)" : string.Join(", ", _words));
    }

    // ── 문장 만들기 ──
    string BuildSentence(List<string> ws)
    {
        // 연속 중복 제거
        var d = new List<string>();
        foreach (var w in ws) if (d.Count == 0 || d[d.Count - 1] != w) d.Add(w);
        if (d.Count == 0) return "";

        // 마지막 동사 위치
        int vi = -1;
        for (int i = d.Count - 1; i >= 0; i--) if (VERBS.ContainsKey(d[i])) { vi = i; break; }

        if (vi < 0) return string.Join(" ", d);   // 동사 없으면 단어 나열

        var sb = new StringBuilder();
        for (int i = 0; i < vi; i++)
        {
            sb.Append(d[i]);
            if (i == vi - 1) sb.Append(Particle(d[i]));   // 동사 바로 앞 명사에 조사
            sb.Append(' ');
        }
        sb.Append(VERBS[d[vi]]);
        return sb.ToString();
    }

    // 받침 있으면 '이', 없으면 '가'
    static string Particle(string w)
    {
        if (string.IsNullOrEmpty(w)) return "";
        char c = w[w.Length - 1];
        bool jong = (c >= 0xAC00 && c <= 0xD7A3) && (((c - 0xAC00) % 28) != 0);
        return jong ? "이" : "가";
    }

    void OnState(KslRecognizer.RecogState state, string word, float prob)
    {
        switch (state)
        {
            case KslRecognizer.RecogState.Confirmed: SetState("확정", confirmedColor); break;
            case KslRecognizer.RecogState.Uncertain: SetState("미인식(?)", uncertainColor); break;
            default: SetState("감지중", detectingColor); break;
        }
    }
    void SetState(string text, Color c)
    {
        if (stateLabel != null) stateLabel.text = text;
        if (stateDot != null) stateDot.color = c;
    }
}