using UnityEngine;

// [임시 테스트용] 모델이 Unity에서 실제로 작동하는지 확인하는 스크립트.
// 사용법:
//   1) KslRecognizer 가 붙어 있는 같은 GameObject 에 이 스크립트도 Add Component
//   2) Play(▶) 누르기
//   3) 1.5초 뒤 Console 에 "[KSL] 인식된 수어: ..." 가 뜨면 성공
//
// ※ 가짜(랜덤) 데이터라 나오는 단어 자체는 의미 없습니다.
//   "오류 없이 끝까지 돌아간다"는 것만 확인하는 용도예요.
//   확인이 끝나면 이 스크립트는 지워도 됩니다.
public class KslSelfTest : MonoBehaviour
{
    KslRecognizer recognizer;

    void Start()
    {
        recognizer = GetComponent<KslRecognizer>();
        if (recognizer == null)
        {
            Debug.LogError("[KSL테스트] 같은 오브젝트에 KslRecognizer 가 없습니다. 같은 GameObject 에 두 스크립트를 모두 붙여주세요.");
            return;
        }
        Invoke(nameof(RunTest), 1.5f);   // Play 후 1.5초 뒤 자동 실행
    }

    void RunTest()
    {
        Debug.Log("[KSL테스트] 가짜 100프레임으로 추론 테스트 시작...");
        for (int t = 0; t < 100; t++)
        {
            float[] frame = new float[27 * 3];   // 27관절 × (x, y, conf)
            for (int i = 0; i < frame.Length; i += 3)
            {
                frame[i + 0] = Random.Range(600f, 1200f);  // 가짜 x (학습 픽셀 범위 흉내)
                frame[i + 1] = Random.Range(100f, 1000f);  // 가짜 y
                frame[i + 2] = 1f;                         // conf
            }
            recognizer.OnFrame(frame);
        }
        Debug.Log("[KSL테스트] 완료. 위에 '[KSL] 인식된 수어: ...' 가 떴으면 모델이 정상 작동하는 거예요.");
    }
}