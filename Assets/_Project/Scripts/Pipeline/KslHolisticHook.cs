using UnityEngine;
using Mediapipe;                          // NormalizedLandmarkList
using Mediapipe.Unity.Sample.Holistic;    // HolisticTrackingSolution 의 정적 이벤트 구독

// MediaPipe Holistic 결과 → MediaPipeToKsl 다리로 전달.
//
// ★ 성능/스레드 핵심 ★
//   MediaPipe 를 Async 모드로 돌리면 결과 콜백이 "백그라운드 스레드"에서 옵니다.
//   하지만 우리 ONNX 추론(Unity InferenceEngine, GPU 버퍼)은 "메인 스레드"에서만
//   호출할 수 있어요. 그래서:
//     - 콜백(OnLandmarks): 점 좌표를 Vector2[] 로 변환해 "저장"만 한다(백그라운드 OK).
//     - Update(): 저장된 최신 데이터로 추론을 호출한다(메인 스레드).
//   => MediaPipe 는 Async 로 빠르게 돌고, 추론은 안전하게 메인에서 돈다.
//
// 사용법:
//   1) 빈 GameObject 에 이 스크립트 추가
//   2) Bridge 칸에 MediaPipeToKsl 연결, Image Width/Height 입력
//   ※ Solution 의 Running Mode 는 다시 'Async' 로 두세요.
public class KslHolisticHook : MonoBehaviour
{
    [Header("연결: MediaPipeToKsl 다리")]
    public MediaPipeToKsl bridge;

    [Header("카메라 영상 픽셀 크기 (학습이 픽셀 좌표라 필요)")]
    public int imageWidth = 1280;
    public int imageHeight = 720;

    // ── 백그라운드(콜백) ↔ 메인(Update) 사이 데이터 전달용 ──
    readonly object _lock = new object();
    Vector2[] _pose, _left, _right;
    int _w, _h;
    bool _hasNew;

    void OnEnable()
    {
        HolisticTrackingSolution.OnKslLandmarks += OnLandmarks;
    }

    void OnDisable()
    {
        HolisticTrackingSolution.OnKslLandmarks -= OnLandmarks;
    }

    // ⚠️ Async 모드에서는 이 콜백이 "백그라운드 스레드"에서 호출됨.
    //    여기서는 변환/저장만! (좌표 읽기·복사는 백그라운드에서 안전)
    void OnLandmarks(NormalizedLandmarkList pose,
                     NormalizedLandmarkList leftHand,
                     NormalizedLandmarkList rightHand)
    {
        var p = ToArray(pose);
        var l = ToArray(leftHand);
        var r = ToArray(rightHand);

        lock (_lock)
        {
            _pose = p; _left = l; _right = r;
            _w = imageWidth; _h = imageHeight;
            _hasNew = true;          // 최신 프레임 표시 (latest-wins)
        }
    }

    // ✅ 추론(GPU/InferenceEngine)은 반드시 메인 스레드에서!
    void Update()
    {
        if (bridge == null) return;

        Vector2[] p, l, r; int w, h;
        lock (_lock)
        {
            if (!_hasNew) return;    // 새 데이터 없으면 건너뜀
            p = _pose; l = _left; r = _right; w = _w; h = _h;
            _hasNew = false;
        }

        bridge.Submit(p, l, r, w, h);
    }

    // NormalizedLandmarkList → Vector2[] (정규화 좌표 0~1)
    static Vector2[] ToArray(NormalizedLandmarkList list)
    {
        if (list == null || list.Landmark == null) return null;
        var arr = new Vector2[list.Landmark.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = new Vector2(list.Landmark[i].X, list.Landmark[i].Y);
        return arr;
    }
}