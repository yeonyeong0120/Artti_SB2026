using System;
using UnityEngine;
using Artti.SignBridge.Data;

namespace Artti.SignBridge.App
{
    /// <summary>
    /// 수업 세션 수명주기 관리. "수업 시작"~"종료" 동안 발화를 누적하고,
    /// 종료 시 SessionStore에 저장한다. 파이프라인 기동/정지 훅을 이벤트로 노출(plan §7).
    ///
    /// 파이프라인(카메라·MediaPipe·Sentis·TTS)은 SessionStarted에서 기동,
    /// SessionEnded에서 정지하도록 추후 단계에서 구독한다(발열·배터리 절약).
    /// </summary>
    public class SessionController : MonoBehaviour
    {
        public static SessionController Instance { get; private set; }

        /// <summary>현재 진행 중인 세션(없으면 null).</summary>
        public ClassSession Current { get; private set; }

        public bool HasActiveSession => Current != null && Current.IsActive;

        /// <summary>세션이 막 시작됨. 파이프라인 기동 구독용.</summary>
        public event Action SessionStarted;

        /// <summary>세션이 종료됨(저장 완료 후). 파이프라인 정지 구독용.</summary>
        public event Action<ClassSession> SessionEnded;

        /// <summary>새 발화가 현재 세션에 추가됨.</summary>
        public event Action<UtteranceEntry> UtteranceAdded;

        private void Awake()
        {
            // Reload Domain 비활성 환경 — static 상태 명시적 초기화(CLAUDE.md).
            Instance = this;
            Current = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>새 수업 세션을 시작하고 파이프라인 기동 이벤트를 발행한다.</summary>
        public void StartSession()
        {
            if (HasActiveSession)
            {
                Debug.LogWarning("[SessionController] 이미 진행 중인 세션이 있습니다.");
                return;
            }

            Current = new ClassSession
            {
                id = Guid.NewGuid().ToString("N"),
                startedUnixMs = NowUnixMs(),
                endedUnixMs = 0,
            };

            SessionStarted?.Invoke();
        }

        /// <summary>현재 세션에 발화를 추가한다(파이프라인 → 이 메서드 호출 예정).</summary>
        public void AddUtterance(UtteranceEntry entry)
        {
            if (!HasActiveSession || entry == null) return;
            Current.utterances.Add(entry);
            UtteranceAdded?.Invoke(entry);
        }

        /// <summary>현재 세션을 종료·저장하고 파이프라인 정지 이벤트를 발행한다.</summary>
        public void EndSession()
        {
            if (!HasActiveSession) return;

            Current.endedUnixMs = NowUnixMs();
            SessionStore.Save(Current);

            ClassSession finished = Current;
            Current = null;
            SessionEnded?.Invoke(finished);
        }

        private static long NowUnixMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
