using UnityEngine;

// MediaPipe 랜드마크 → KSL 모델 입력(81숫자) 변환 + 전달 다리(bridge)
//
// 사용법:
//   1) 씬에 빈 GameObject 만들고 이 스크립트 Add Component (또는 KslRecognizer 와 같은 오브젝트)
//   2) 인스펙터의 Recognizer 칸에 KslRecognizer 연결
//   3) MediaPipe 결과 콜백 안에서, 점들을 Vector2[] 로 바꿔 Submit(...) 호출
//      (Vector2 변환 예시는 이 파일 맨 아래 주석 참고)
//
// ※ 이 스크립트는 MediaPipe 타입을 직접 참조하지 않아서, MediaPipe 설치 여부와 상관없이 항상 컴파일됩니다.
public class MediaPipeToKsl : MonoBehaviour
{
    [Header("연결: 씬의 KslRecognizer")]
    public KslRecognizer recognizer;

    // ── 27관절 매핑 (입력 규격서와 동일) ──
    // 몸 7점 = MediaPipe Pose 인덱스: 코, L어깨, R어깨, L팔꿈치, R팔꿈치, L손목, R손목
    static readonly int[] PoseIdx = { 0, 11, 12, 13, 14, 15, 16 };
    // 손 10점 = MediaPipe Hand 인덱스: 손목, 엄지끝, 검지밑, 검지끝, 중지밑, 중지끝, 약지밑, 약지끝, 새끼밑, 새끼끝
    static readonly int[] HandIdx = { 0, 4, 5, 8, 9, 12, 13, 16, 17, 20 };

    const int V = 27;   // 관절 수
    const int C = 3;    // x, y, conf

    // 매 프레임 호출.
    //   pose       : MediaPipe Pose 33점 (정규화 0~1). 미검출이면 null
    //   leftHand   : 사람 왼손 21점 (정규화 0~1). 미검출이면 null
    //   rightHand  : 사람 오른손 21점 (정규화 0~1). 미검출이면 null
    //   imgW, imgH : 카메라 영상의 픽셀 크기 (예: 1280, 720)
    public void Submit(Vector2[] pose, Vector2[] leftHand, Vector2[] rightHand, int imgW, int imgH)
    {
        if (recognizer == null)
        {
            Debug.LogError("[Bridge] Recognizer 가 연결되지 않았습니다. 인스펙터에서 KslRecognizer 를 연결하세요.");
            return;
        }

        float[] frame = new float[V * C];   // 81개
        int j = 0;                          // 27관절 채우는 위치

        // 1) 몸 7점
        foreach (int idx in PoseIdx)
            WriteJoint(frame, ref j, pose, idx, imgW, imgH);

        // 2) 왼손 10점
        foreach (int idx in HandIdx)
            WriteJoint(frame, ref j, leftHand, idx, imgW, imgH);

        // 3) 오른손 10점
        foreach (int idx in HandIdx)
            WriteJoint(frame, ref j, rightHand, idx, imgW, imgH);

        recognizer.OnFrame(frame);
    }

    // 관절 1개 채우기: (x_픽셀, y_픽셀, conf). 점이 없으면 (0, 0, 0)
    void WriteJoint(float[] frame, ref int j, Vector2[] src, int idx, int imgW, int imgH)
    {
        int p = j * C;
        if (src != null && idx < src.Length)
        {
            frame[p + 0] = src[idx].x * imgW;   // 정규화(0~1) → 픽셀
            frame[p + 1] = src[idx].y * imgH;
            frame[p + 2] = 1f;                  // 검출됨
        }
        else
        {
            frame[p + 0] = 0f;
            frame[p + 1] = 0f;
            frame[p + 2] = 0f;                  // 미검출
        }
        j++;
    }
}

// ──────────────────────────────────────────────────────────────────
// [MediaPipe 점 → Vector2[] 변환 예시]
//
// MediaPipe 결과(랜드마크 리스트)를 위 Submit 에 넘기기 전에 Vector2[] 로 바꿔야 합니다.
// 쓰는 샘플 API에 따라 필드 이름이 .x/.y 또는 .X/.Y 로 다를 수 있으니 그 한 줄만 맞추면 됩니다.
//
// 레거시 그래프(NormalizedLandmarkList) 예시:
//
//   Vector2[] ToArray(Mediapipe.NormalizedLandmarkList list) {
//       if (list == null) return null;
//       var arr = new Vector2[list.Landmark.Count];
//       for (int i = 0; i < arr.Length; i++)
//           arr[i] = new Vector2(list.Landmark[i].X, list.Landmark[i].Y);
//       return arr;
//   }
//
// Tasks API(NormalizedLandmarks) 예시:
//
//   Vector2[] ToArray(NormalizedLandmarks lms) {
//       if (lms.landmarks == null) return null;
//       var arr = new Vector2[lms.landmarks.Count];
//       for (int i = 0; i < arr.Length; i++)
//           arr[i] = new Vector2(lms.landmarks[i].x, lms.landmarks[i].y);
//       return arr;
//   }
//
// 그리고 MediaPipe 콜백 안에서:
//
//   bridge.Submit(
//       ToArray(poseLandmarks),
//       ToArray(leftHandLandmarks),
//       ToArray(rightHandLandmarks),
//       imageWidth, imageHeight);
//
// ※ HandLandmarker(Tasks API)로 양손을 한 번에 받는 경우, handedness(왼/오른) 결과를 보고
//    어느 손을 leftHand / rightHand 로 넣을지 직접 정해야 합니다.
// ※ 셀카(거울) 모드면 좌우가 뒤집힐 수 있으니, 나중에 화면에서 왼/오른손이 맞는지 한 번 확인하세요.
// ──────────────────────────────────────────────────────────────────