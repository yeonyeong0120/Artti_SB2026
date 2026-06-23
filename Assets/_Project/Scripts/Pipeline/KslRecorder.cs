using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// 앱 안에서 직접 학습 데이터를 녹화하는 도구.
// - 화면에 단어 버튼이 뜸 → 버튼 누르고(초록) → 그 수어를 하고 손을 내리면 자동 저장.
// - 한 동작(손 올림~내림) = 한 샘플. 단어마다 20~30개씩 모으면 좋아요.
// - 저장 위치: Application.persistentDataPath/recordings  (로그에 경로 찍힘)
//
// 사용:
//   1) 빈 GameObject 에 이 스크립트 추가 (또는 MediaPipeToKsl 옆에)
//   2) MediaPipeToKsl 의 Recorder 칸에 이걸 연결
//   3) Words 에 녹화할 단어 3개 설정 (classId + 이름)
//   4) 빌드 → 폰에서 버튼 누르고 수어 → 손 내리면 저장
public class KslRecorder : MonoBehaviour
{
    [System.Serializable]
    public class WordDef { public int classId; public string name; }

    [Header("녹화할 단어 (3개 권장)")]
    public WordDef[] words = new WordDef[]
    {
        new WordDef { classId = 50, name = "엄마" },
        new WordDef { classId = 10, name = "좋다" },
        new WordDef { classId = 25, name = "친구" },
    };

    [Header("설정")]
    public int minFrames = 20;      // 이보다 짧으면 버림
    public int maxFrames = 200;     // 너무 길면 자름
    public int minHandPoints = 6;   // 손 인식 최소 점수

    int _cur = -1;                  // 선택된 단어 인덱스
    bool _rec = false;
    readonly List<float[]> _buf = new List<float[]>();
    readonly Dictionary<int, int> _cnt = new Dictionary<int, int>();
    string _dir;
    string _status = "단어 버튼을 누르고 수어하세요";

    void Awake()
    {
        _dir = Path.Combine(Application.persistentDataPath, "recordings");
        Directory.CreateDirectory(_dir);
        Debug.Log($"[REC] 저장 폴더: {_dir}");
    }

    // bridge 가 매 프레임 27관절(81개=27x3)을 넘겨줌
    public void Feed(float[] frame)
    {
        if (_cur < 0 || frame == null) return;

        int handPts = 0;
        for (int v = 7; v < 27; v++)
            if (frame[v * 3 + 2] > 0.5f) handPts++;
        bool hands = handPts >= minHandPoints;

        if (hands)
        {
            _rec = true;
            if (_buf.Count < maxFrames) _buf.Add((float[])frame.Clone());
        }
        else if (_rec)          // 손 내림 → 한 동작 끝 → 저장
        {
            _rec = false;
            SaveSegment();
        }
    }

    void SaveSegment()
    {
        if (_buf.Count < minFrames)
        {
            _status = $"너무 짧음 ({_buf.Count}프레임) — 다시";
            _buf.Clear();
            return;
        }
        var w = words[_cur];
        int n = (_cnt.ContainsKey(w.classId) ? _cnt[w.classId] : 0) + 1;
        _cnt[w.classId] = n;

        string path = Path.Combine(_dir, $"W{w.classId}_{n:D3}.csv");
        var sb = new StringBuilder();
        foreach (var f in _buf)
        {
            for (int i = 0; i < f.Length; i++)
            {
                sb.Append(f[i].ToString("F2"));
                if (i < f.Length - 1) sb.Append(',');
            }
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString());
        _buf.Clear();
        _status = $"저장됨: {w.name} #{n}";
        Debug.Log($"[REC] saved {path} ({_status})");
    }

    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;
        GUI.skin.button.fontSize = Mathf.RoundToInt(H * 0.030f);
        GUI.skin.label.fontSize = Mathf.RoundToInt(H * 0.025f);

        float bw = W / words.Length - 16;
        float bh = H * 0.13f;
        float y = H - bh - H * 0.12f;

        for (int i = 0; i < words.Length; i++)
        {
            int cnt = _cnt.ContainsKey(words[i].classId) ? _cnt[words[i].classId] : 0;
            var prev = GUI.color;
            if (i == _cur) GUI.color = Color.green;     // 선택된 단어 초록
            if (GUI.Button(new Rect(8 + i * (bw + 12), y, bw, bh), $"{words[i].name}\n({cnt})"))
                _cur = i;
            GUI.color = prev;
        }

        string head = _rec ? "● 녹화중..." : "대기";
        GUI.Label(new Rect(16, H - H * 0.10f, W - 32, H * 0.09f), $"[{head}] {_status}");
    }
}