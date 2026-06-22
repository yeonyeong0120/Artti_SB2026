using UnityEngine;
using TMPro;
using System.Collections.Generic;

// 확정된 수어 단어를 화면에 표시.
// KslRecognizer.OnWordConfirmed 신호를 구독해서, 단어가 확정될 때마다 텍스트를 갱신합니다.
//
// 사용법:
//   1) 화면에 보여줄 TextMeshPro 텍스트를 준비 (없으면 GameObject > UI > Text - TextMeshPro)
//   2) 빈 GameObject 에 이 스크립트 추가
//   3) Recognizer 칸에 SignRecognizer, Word Text 칸에 위 텍스트 연결
//      (Sentence Text 는 선택 — 단어를 문장으로 이어붙이고 싶을 때만)
public class KslWordDisplay : MonoBehaviour
{
    [Header("연결")]
    public KslRecognizer recognizer;     // 인식기 (SignRecognizer)
    public TMP_Text wordText;            // 방금 확정된 단어 한 개
    public TMP_Text sentenceText;        // (선택) 확정된 단어들을 이어붙인 문장

    [Header("문장 설정")]
    public bool buildSentence = true;    // 단어를 문장으로 이어붙일지
    public int maxWords = 12;            // 문장에 보관할 최대 단어 수

    readonly List<string> _words = new List<string>();

    void OnEnable()
    {
        if (recognizer != null) recognizer.OnWordConfirmed += OnWord;
        else Debug.LogError("[Display] Recognizer 가 연결되지 않았습니다.");
    }

    void OnDisable()
    {
        if (recognizer != null) recognizer.OnWordConfirmed -= OnWord;
    }

    // 단어가 확정될 때마다 호출됨
    void OnWord(int cls, string word, float prob)
    {
        if (wordText != null) wordText.text = word;

        if (buildSentence && sentenceText != null)
        {
            _words.Add(word);
            if (_words.Count > maxWords) _words.RemoveAt(0);
            sentenceText.text = string.Join(" ", _words);
        }
    }

    // 문장 지우기 — UI 버튼의 OnClick 에 연결하면 됩니다.
    public void ClearSentence()
    {
        _words.Clear();
        if (sentenceText != null) sentenceText.text = "";
        if (wordText != null) wordText.text = "";
    }
}