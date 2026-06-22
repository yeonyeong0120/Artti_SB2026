using UnityEngine;
using Unity.InferenceEngine;          // 패키지: com.unity.ai.inference
using System;
using System.Collections.Generic;

// SignBridge KSL-60 수어 인식기 (단어 확정 규칙 포함)
// - ksl_60_joint_best.onnx 를 Model Asset 에 연결
// - MediaPipe 쪽이 매 프레임 OnFrame(...) 호출
// - 같은 단어가 안정적으로 나올 때만 '확정'하여 OnWordConfirmed 이벤트 발생
public class KslRecognizer : MonoBehaviour
{
    [Header("모델: ksl_60_joint_best.onnx 를 여기에 연결")]
    public ModelAsset modelAsset;

    [Header("단어 확정 규칙")]
    [Tooltip("이 확률(0~1) 이상일 때만 후보로 인정")]
    public float confidenceThreshold = 0.6f;
    [Tooltip("같은 단어가 연속 몇 번 나오면 확정할지")]
    public int confirmFrames = 10;
    [Tooltip("켜면 매 프레임 예측을 콘솔에 찍어 디버깅에 도움")]
    public bool verbose = false;

    [Header("스케일 정규화 (학습 좌표 크기에 맞춤)")]
    [Tooltip("켜면 어깨너비 기준으로 좌표 크기를 학습 스케일에 맞춤. 학습은 코 기준 이동만 보정하고 크기는 안 맞춰서, 이걸로 크기를 맞춰주면 정확도가 올라감")]
    public bool scaleNormalize = true;
    [Tooltip("학습 데이터의 어깨너비(픽셀) 목표값. 학습 좌표 x[695,1198] y[144,1032] 기준 추정치. .npy 로 정확값 계산 예정. 인식이 이상하면 이 값을 조절")]
    public float targetShoulderPx = 250f;

    [Header("게이팅 (연속 헛인식 방지)")]
    [Tooltip("이 개수(20점 중) 이상 손 관절이 잡혀야 인식 시도. 손 없으면 대기")]
    public int minHandPoints = 6;
    [Tooltip("단어 확정 후, 손을 한 번 내려야(손 점 < minHandPoints) 다음 단어를 받음 → 동작 사이 끊김 생성")]
    public bool requireReleaseBetweenWords = true;

    // 단어가 확정될 때 호출 (UI 등에서 구독). 인자: (클래스번호, 단어, 확률)
    public event Action<int, string, float> OnWordConfirmed;

    // 인식 상태 (UI 점/글씨 표시용)
    public enum RecogState { Detecting, Uncertain, Confirmed }   // 감지중 / 미인식(?) / 확정
    // 매 추론마다 현재 상태 알림. 인자: (상태, 최상위 단어, 확률)
    public event Action<RecogState, string, float> OnStateChanged;

    const int C = 3;     // 채널: x, y, confidence
    const int T = 100;   // 프레임 수 (고정)
    const int V = 27;    // 관절 수
    const int NOSE = 0;  // 코 = 0번 관절 (정규화 기준점)
    const int L_SH = 1;  // 왼어깨 = 1번 (어깨너비 측정용)
    const int R_SH = 2;  // 오른어깨 = 2번

    // 0~59번 클래스 → 한국어 단어 (class_mapping.json 기준, 순서 고정)
    static readonly string[] Words =
    {
        "고민", "슬프다", "교실", "일요일", "금요일", "수요일", "어떻게", "월요일",
        "화요일", "얼마", "좋다", "가다", "쓰다", "모르다", "원하다", "잘하다",
        "마지막", "듣다", "오다", "아프다", "피곤하다", "맞다", "의자", "알다",
        "죄송", "친구", "이해", "어렵다", "친하다", "쉽다", "화나다", "놀라다",
        "후회", "못하다", "싫다", "감사", "시원하다", "기억", "검정", "빨강",
        "노랑", "초록", "파랑", "잘못하다", "왼쪽", "오른쪽", "간단하다", "틀리다",
        "괜찮다", "따뜻하다", "엄마", "시작", "부탁", "재미없다", "조용하다", "위",
        "받다", "깜빡하다", "주다", "아래",
    };

    Model model;
    Worker worker;
    readonly Queue<float[]> frames = new Queue<float[]>();

    // 확정 규칙 상태
    int _candidateClass = -1;   // 현재 후보 단어
    int _streak = 0;            // 후보가 연속으로 나온 횟수
    int _lastConfirmed = -1;    // 마지막으로 확정한 단어 (중복 확정 방지)
    bool _justConfirmed = false; // 이번 추론에서 막 확정됐는지 (상태 표시용)
    bool _awaitingRelease = false; // 확정 후, 손을 내려야 다음 단어 인식 (게이팅)

    void Start()
    {
        model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);  // 미지원 플랫폼이면 BackendType.CPU
    }

    // MediaPipe 쪽이 매 프레임 호출. jointsXYZ: 길이 81 = 27관절 × (x_픽셀, y_픽셀, conf)
    public void OnFrame(float[] jointsXYZ)
    {
        if (jointsXYZ == null || jointsXYZ.Length != V * C)
        {
            Debug.LogError($"[KSL] 관절 데이터 길이 오류: {(jointsXYZ?.Length ?? 0)} (기대값 {V * C})");
            return;
        }

        frames.Enqueue((float[])jointsXYZ.Clone());
        if (frames.Count > T) frames.Dequeue();
        if (frames.Count == T) RunInference();
    }

    void RunInference()
    {
        float[][] buf = frames.ToArray();

        // 1) 코 기준 정규화 (x,y의 코 시간평균을 뺌)
        float noseMeanX = 0f, noseMeanY = 0f;
        for (int t = 0; t < T; t++)
        {
            noseMeanX += buf[t][NOSE * C + 0];
            noseMeanY += buf[t][NOSE * C + 1];
        }
        noseMeanX /= T;
        noseMeanY /= T;

        // 1b) 어깨너비 기준 스케일 (학습 좌표 크기에 맞춤)
        float scale = 1f;
        if (scaleNormalize)
        {
            float sumSh = 0f; int cntSh = 0;
            for (int t = 0; t < T; t++)
            {
                if (buf[t][L_SH * C + 2] > 0.5f && buf[t][R_SH * C + 2] > 0.5f)
                {
                    float dx = buf[t][L_SH * C + 0] - buf[t][R_SH * C + 0];
                    float dy = buf[t][L_SH * C + 1] - buf[t][R_SH * C + 1];
                    sumSh += Mathf.Sqrt(dx * dx + dy * dy);
                    cntSh++;
                }
            }
            if (cntSh > 0)
            {
                float meanSh = sumSh / cntSh;
                if (meanSh > 1f) scale = targetShoulderPx / meanSh;   // 어깨너비를 목표값으로 맞춤
            }
        }

        // 2) [1, C, T, V, 1] 텐서.  코 기준 이동 + 스케일 보정.  위치 = (c*T + t)*V + v
        float[] data = new float[C * T * V];
        for (int t = 0; t < T; t++)
        {
            for (int v = 0; v < V; v++)
            {
                data[(0 * T + t) * V + v] = (buf[t][v * C + 0] - noseMeanX) * scale;
                data[(1 * T + t) * V + v] = (buf[t][v * C + 1] - noseMeanY) * scale;
                data[(2 * T + t) * V + v] = buf[t][v * C + 2];   // conf 그대로
            }
        }

        // 3) 추론
        using var input = new Tensor<float>(new TensorShape(1, C, T, V, 1), data);
        worker.Schedule(input);
        using var logits = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
        float[] scores = logits.DownloadToArray();

        // 4) softmax → 확률, 최상위 단어
        float[] probs = Softmax(scores);
        int cls = ArgMax(probs);
        float p = probs[cls];

        // 4b) 손 존재 개수 (게이팅용) — 최신 프레임의 양손 20점 중
        int handPts = 0;
        for (int v = 7; v < V; v++)
            if (buf[T - 1][v * C + 2] > 0.5f) handPts++;
        bool handsPresent = handPts >= minHandPoints;

        if (verbose)
            Debug.Log($"[KSL] cls={cls} p={p:F2} streak={_streak} hands={handPts}/20 scale={scale:F2}");

        // 5) 게이팅 + 단어 확정
        _justConfirmed = false;
        RecogState state;
        if (!handsPresent)
        {
            // 손 없음/내림 → 대기. 후보 리셋 + 다음 단어 받을 준비(release).
            _candidateClass = -1;
            _streak = 0;
            _awaitingRelease = false;
            _lastConfirmed = -1;            // 손 내렸으니 같은 단어도 다시 인식 가능
            state = RecogState.Detecting;   // 감지중(회색)
        }
        else
        {
            ApplyConfirmRule(cls, p);
            if (_justConfirmed) state = RecogState.Confirmed;   // 확정(초록)
            else if (_awaitingRelease) state = RecogState.Detecting;   // 확정 후 손 내리기 대기
            else if (p >= confidenceThreshold) state = RecogState.Uncertain;   // 미인식(?)(주황)
            else state = RecogState.Detecting;   // 감지중(회색)
        }
        OnStateChanged?.Invoke(state, Word(cls), p);
    }

    void ApplyConfirmRule(int cls, float p)
    {
        if (p >= confidenceThreshold)
        {
            // 같은 후보면 연속 횟수 증가, 바뀌면 1로 리셋
            if (cls == _candidateClass) _streak++;
            else { _candidateClass = cls; _streak = 1; }

            // 확정 후 아직 손을 안 내렸으면(release 대기) 새 확정 보류
            bool blocked = requireReleaseBetweenWords && _awaitingRelease;

            if (_streak >= confirmFrames && !blocked && _candidateClass != _lastConfirmed)
            {
                _lastConfirmed = _candidateClass;
                _awaitingRelease = true;     // 다음 단어는 손을 내린 뒤에
                _justConfirmed = true;
                string word = Word(_candidateClass);
                Debug.Log($"[KSL] ✅ 확정: {word} (확률 {p:P0})");
                OnWordConfirmed?.Invoke(_candidateClass, word, p);   // UI 등으로 전달
            }
        }
        else
        {
            // 확신 낮음(동작 전환 등) → 후보 초기화
            _candidateClass = -1;
            _streak = 0;
        }
    }

    static float[] Softmax(float[] x)
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < x.Length; i++) if (x[i] > max) max = x[i];
        float sum = 0f;
        float[] o = new float[x.Length];
        for (int i = 0; i < x.Length; i++) { o[i] = Mathf.Exp(x[i] - max); sum += o[i]; }
        for (int i = 0; i < x.Length; i++) o[i] /= sum;
        return o;
    }

    int ArgMax(float[] v)
    {
        int idx = 0;
        for (int n = 1; n < v.Length; n++) if (v[n] > v[idx]) idx = n;
        return idx;
    }

    string Word(int cls) => (cls >= 0 && cls < Words.Length) ? Words[cls] : $"class {cls}";

    void OnDestroy() => worker?.Dispose();
}