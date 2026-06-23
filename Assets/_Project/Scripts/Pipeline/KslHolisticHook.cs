using UnityEngine;
using Mediapipe;                          // NormalizedLandmarkList
using Mediapipe.Unity.Sample.Holistic;    // HolisticTrackingSolution (정적 이벤트 + 카메라 크기)

// MediaPipe Holistic 결과 → MediaPipeToKsl 다리로 전달.
//
// ★ 카메라 크기 ★
//   Solution(HolisticTrackingSolution)이 실제 카메라 크기를 KslImgW/KslImgH 에 넣어둠.
//   여기선 그걸 읽어서 씀 → 세로/가로 비율이 자동으로 맞음.
//   (그래도 찌그러지면 Swap Width Height 토글)
public class KslHolisticHook : MonoBehaviour
{
    [Header("연결: MediaPipeToKsl 다리")]
    public MediaPipeToKsl bridge;

    [Header("카메라 크기")]
    [Tooltip("켜면 Solution이 알려준 실제 카메라 크기를 사용 (권장)")]
    public bool autoImageSize = true;
    [Tooltip("세로 화면에서 스켈레톤이 찌그러지면 켜서 가로세로를 바꿔보세요")]
    public bool swapWidthHeight = false;
    [Tooltip("자동 크기를 못 받았을 때 폴백 값")]
    public int imageWidth = 1280;
    public int imageHeight = 720;

    readonly object _lock = new object();
    Vector2[] _pose, _left, _right;
    bool _hasNew;
    bool _logged = false;

    void OnEnable() { HolisticTrackingSolution.OnKslLandmarks += OnLandmarks; }
    void OnDisable() { HolisticTrackingSolution.OnKslLandmarks -= OnLandmarks; }

    void OnLandmarks(NormalizedLandmarkList pose,
                     NormalizedLandmarkList leftHand,
                     NormalizedLandmarkList rightHand)
    {
        var p = ToArray(pose);
        var l = ToArray(leftHand);
        var r = ToArray(rightHand);
        lock (_lock) { _pose = p; _left = l; _right = r; _hasNew = true; }
    }

    void Update()
    {
        if (bridge == null) return;

        Vector2[] p, l, r;
        lock (_lock)
        {
            if (!_hasNew) return;
            p = _pose; l = _left; r = _right; _hasNew = false;
        }

        // ★ Solution이 저장한 실제 카메라 크기 사용 ★
        int w = imageWidth, h = imageHeight;
        if (autoImageSize && HolisticTrackingSolution.KslImgW > 0)
        {
            w = HolisticTrackingSolution.KslImgW;
            h = HolisticTrackingSolution.KslImgH;
        }
        if (swapWidthHeight) { int t = w; w = h; h = t; }

        if (!_logged)
        {
            Debug.Log($"[KSL] imageSize used = {w}x{h} (auto={autoImageSize}, swap={swapWidthHeight})");
            _logged = true;
        }

        bridge.Submit(p, l, r, w, h);
    }

    static Vector2[] ToArray(NormalizedLandmarkList list)
    {
        if (list == null || list.Landmark == null) return null;
        var arr = new Vector2[list.Landmark.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = new Vector2(list.Landmark[i].X, list.Landmark[i].Y);
        return arr;
    }
}