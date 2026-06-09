using System;
using System.Collections.Generic;

namespace Artti.SignBridge.Data
{
    /// <summary>
    /// 수업 세션. 홈에서 "수업 시작"~"종료"까지의 발화를 하나로 묶는다(plan §7).
    /// 진행 중에는 메모리에 누적되고, 종료 시 SessionStore로 기기에 영속화된다.
    /// </summary>
    [Serializable]
    public class ClassSession
    {
        /// <summary>세션 고유 ID (Guid "N").</summary>
        public string id;

        /// <summary>세션 시작 시각 (Unix epoch ms, UTC).</summary>
        public long startedUnixMs;

        /// <summary>세션 종료 시각 (Unix epoch ms, UTC). 0이면 진행 중.</summary>
        public long endedUnixMs;

        /// <summary>세션 동안 누적된 발화 목록(시간순).</summary>
        public List<UtteranceEntry> utterances = new List<UtteranceEntry>();

        public bool IsActive => endedUnixMs == 0;

        public DateTime StartedLocal =>
            DateTimeOffset.FromUnixTimeMilliseconds(startedUnixMs).LocalDateTime;
    }
}
