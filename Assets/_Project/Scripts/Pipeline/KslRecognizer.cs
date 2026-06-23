using UnityEngine;
using Unity.InferenceEngine;          // 패키지: com.unity.ai.inference
using System;
using System.Collections.Generic;

// 수어 인식기 (3단어 모델, 세그먼트 방식)
//   - 손이 올라오면 동작 녹화 시작 → 손을 내리면 한 동작 끝 → 그때 인식 1회.
//   - 동작 전체를 100프레임으로 리샘플(빠르든 느리든 맞춰짐) + 코정규화 + 어깨310 스케일.
//   - 학습(직접 녹화) 전처리와 100% 동일.
public class KslRecognizer : MonoBehaviour
{
    [Header("모델 (3단어 ONNX)")]
    public ModelAsset modelAsset;

    [Header("단어 (출력 0,1,2 순서대로)")]
    public string[] words = { "월요일", "아침", "싫다" };

    [Header("인식 설정")]
    [Tooltip("이 확률 이상일 때만 확정")]
    public float confidenceThreshold = 0.6f;
    [Tooltip("이보다 짧은 동작(프레임)은 무시")]
    public int minSegFrames = 12;
    [Tooltip("손 관절(20점 중) 이 개수 이상이면 '손 있음'")]
    public int minHandPoints = 6;
    public bool scaleNormalize = true;
    public float targetShoulderPx = 310f;
    public bool verbose = true;

    public event Action<int, string, float> OnWordConfirmed;
    public enum RecogState { Detecting, Uncertain, Confirmed }
    public event Action<RecogState, string, float> OnStateChanged;

    const int V = 27;   // 관절
    const int C = 3;    // x,y,conf
    const int T = 100;  // 고정 프레임
    const int NOSE = 0, L_SH = 1, R_SH = 2;

    Model model;
    Worker worker;
    readonly List<float[]> _seg = new List<float[]>();
    bool _recording = false;

    void Start()
    {
        model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);  // 안 되면 BackendType.CPU
    }

    void OnDestroy() { worker?.Dispose(); }

    // bridge가 매 프레임 27관절(81=27*3) 전달
    public void OnFrame(float[] frame)
    {
        if (frame == null) return;

        int handPts = 0;
        for (int v = 7; v < V; v++) if (frame[v * C + 2] > 0.5f) handPts++;
        bool hands = handPts >= minHandPoints;

        if (hands)
        {
            _recording = true;
            _seg.Add((float[])frame.Clone());
            OnStateChanged?.Invoke(RecogState.Detecting, "", 0f);   // 동작 중(회색)
        }
        else if (_recording)            // 손 내림 → 한 동작 끝
        {
            _recording = false;
            if (_seg.Count >= minSegFrames) Recognize();
            _seg.Clear();
        }
    }

    void Recognize()
    {
        int n = _seg.Count;

        // 1) 100프레임으로 linspace 리샘플 (속도 무관)
        float[][] rs = new float[T][];
        for (int t = 0; t < T; t++)
        {
            int idx = Mathf.RoundToInt((float)t * (n - 1) / (T - 1));
            if (idx < 0) idx = 0; if (idx > n - 1) idx = n - 1;
            rs[t] = _seg[idx];
        }

        // 2) 코 시간평균
        float noseX = 0f, noseY = 0f;
        for (int t = 0; t < T; t++) { noseX += rs[t][NOSE * C + 0]; noseY += rs[t][NOSE * C + 1]; }
        noseX /= T; noseY /= T;

        // 3) 어깨너비 → 310 스케일
        float scale = 1f;
        if (scaleNormalize)
        {
            float sum = 0f; int cnt = 0;
            for (int t = 0; t < T; t++)
                if (rs[t][L_SH * C + 2] > 0.5f && rs[t][R_SH * C + 2] > 0.5f)
                {
                    float dx = rs[t][L_SH * C + 0] - rs[t][R_SH * C + 0];
                    float dy = rs[t][L_SH * C + 1] - rs[t][R_SH * C + 1];
                    sum += Mathf.Sqrt(dx * dx + dy * dy); cnt++;
                }
            if (cnt > 0) { float sh = sum / cnt; if (sh > 1f) scale = targetShoulderPx / sh; }
        }

        // 4) 텐서 [1,3,100,27,1].  위치 = (c*T + t)*V + v
        float[] data = new float[C * T * V];
        for (int t = 0; t < T; t++)
            for (int v = 0; v < V; v++)
            {
                data[(0 * T + t) * V + v] = (rs[t][v * C + 0] - noseX) * scale;
                data[(1 * T + t) * V + v] = (rs[t][v * C + 1] - noseY) * scale;
                data[(2 * T + t) * V + v] = rs[t][v * C + 2];
            }

        // 5) 추론
        using var input = new Tensor<float>(new TensorShape(1, C, T, V, 1), data);
        worker.Schedule(input);
        using var outT = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
        float[] scores = outT.DownloadToArray();

        float[] probs = Softmax(scores);
        int cls = ArgMax(probs);
        float p = probs[cls];

        if (verbose)
            Debug.Log($"[KSL] seg={n} cls={cls}({Word(cls)}) p={p:F2} scale={scale:F2}");

        if (p >= confidenceThreshold)
        {
            Debug.Log($"[KSL] ✅ 확정: {Word(cls)} ({p:P0})");
            OnWordConfirmed?.Invoke(cls, Word(cls), p);
            OnStateChanged?.Invoke(RecogState.Confirmed, Word(cls), p);
        }
        else
        {
            OnStateChanged?.Invoke(RecogState.Uncertain, Word(cls), p);  // 애매(주황)
        }
    }

    string Word(int c) => (c >= 0 && c < words.Length) ? words[c] : "?";

    static float[] Softmax(float[] z)
    {
        float mx = z[0]; for (int i = 1; i < z.Length; i++) if (z[i] > mx) mx = z[i];
        float s = 0f; var e = new float[z.Length];
        for (int i = 0; i < z.Length; i++) { e[i] = Mathf.Exp(z[i] - mx); s += e[i]; }
        for (int i = 0; i < z.Length; i++) e[i] /= s;
        return e;
    }
    static int ArgMax(float[] z) { int m = 0; for (int i = 1; i < z.Length; i++) if (z[i] > z[m]) m = i; return m; }
}