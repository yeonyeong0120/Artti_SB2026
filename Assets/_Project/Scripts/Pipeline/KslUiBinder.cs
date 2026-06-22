using UnityEngine;
using UnityEngine.UI;     // Image (상태 점)
using TMPro;              // TMP_Text
using System.Collections.Generic;

// 인식 결과(KslRecognizer)를 SignBridge 앱의 실제 UI 자리에 연결.
//   - 누적 단어:  RecognitionScreen/TopBar/BufferLabel
//   - 최근 변환:  RecognitionScreen/Caption/Sentence
//   - 상태 글씨:  RecognitionScreen/TopBar/StateLabel  (감지중/미인식(?)/확정)
//   - 상태 점:    RecognitionScreen/TopBar/StateDot    (회색/주황/초록)
//
// 사용법:
//   1) 아무 GameObject(예: SignRecognizer)에 이 스크립트 Add Component
//   2) 아래 칸들을 인스펙터에서 연결
//   3) 떠다니던 Main Canvas/Container Panel 의 KslWordDisplay, KslSentenceDisplay 오브젝트는 비활성화
public class KslUiBinder : MonoBehaviour
{
    [Header("연결: 인식기")]
    public KslRecognizer recognizer;

    [Header("연결: 앱 UI (TopBar / Caption)")]
    public TMP_Text bufferLabel;    // 누적 단어 (BufferLabel)
    public TMP_Text sentenceLabel;  // 최근 변환 문장 (Caption/Sentence)
    public TMP_Text stateLabel;     // 상태 글씨 (StateLabel)
    public Image stateDot;       // 상태 점 (StateDot)

    [Header("누적 단어 표시")]
    [Tooltip("최대 몇 개까지 누적해 보여줄지")]
    public int maxWords = 20;
    [Tooltip("누적 단어 앞에 붙일 머리말")]
    public string bufferPrefix = "누적 단어: ";

    [Header("상태 색 (Legend 와 동일하게)")]
    public Color detectingColor = new Color(0.6f, 0.6f, 0.6f);     // 감지중(회색)
    public Color uncertainColor = new Color(1f, 0.65f, 0f);        // 미인식(주황)
    public Color confirmedColor = new Color(0.2f, 0.8f, 0.3f);     // 확정(초록)

    readonly List<string> _words = new List<string>();

    void OnEnable()
    {
        if (recognizer == null)
        {
            Debug.LogError("[UiBinder] recognizer 가 연결되지 않았습니다.");
            return;
        }
        recognizer.OnWordConfirmed += OnConfirmed;
        recognizer.OnStateChanged += OnState;
    }

    void OnDisable()
    {
        if (recognizer == null) return;
        recognizer.OnWordConfirmed -= OnConfirmed;
        recognizer.OnStateChanged -= OnState;
    }

    // 단어가 확정되면: 누적 단어 + 최근 변환 문장 갱신
    void OnConfirmed(int cls, string word, float prob)
    {
        _words.Add(word);
        while (_words.Count > maxWords) _words.RemoveAt(0);

        if (bufferLabel != null)
            bufferLabel.text = bufferPrefix + string.Join(", ", _words);

        if (sentenceLabel != null)
            sentenceLabel.text = string.Join(" ", _words);   // 임시: 단어 이어붙이기
                                                             // (진짜 문장 변환은 별도 NLP 작업)
    }

    // 매 추론마다: 상태 점/글씨 갱신
    void OnState(KslRecognizer.RecogState state, string word, float prob)
    {
        switch (state)
        {
            case KslRecognizer.RecogState.Confirmed:
                SetState("확정", confirmedColor); break;
            case KslRecognizer.RecogState.Uncertain:
                SetState("미인식(?)", uncertainColor); break;
            default:
                SetState("감지중", detectingColor); break;
        }
    }

    void SetState(string text, Color c)
    {
        if (stateLabel != null) stateLabel.text = text;
        if (stateDot != null) stateDot.color = c;
    }

    // 외부에서 누적 단어 비우기 (예: '문장 지우기' 버튼)
    public void ClearWords()
    {
        _words.Clear();
        if (bufferLabel != null) bufferLabel.text = bufferPrefix + "(없음)";
        if (sentenceLabel != null) sentenceLabel.text = "";
    }
}