using System;
using System.Collections.Generic;

namespace Artti.SignBridge.Data
{
    /// <summary>
    /// 한 번의 수어 발화 단위. 원본으로 인식된 수어 단어 나열과
    /// Gemini가 변환한 한국어 문장, 미인식 토큰 포함 여부를 함께 보관한다(plan §6·§7).
    /// </summary>
    [Serializable]
    public class UtteranceEntry
    {
        /// <summary>임계값 미달 구간에 push되는 미인식 토큰(plan §6).</summary>
        public const string UnrecognizedToken = "?";

        /// <summary>Sentis가 순차 인식한 원본 수어 단어 나열. 미인식 구간은 "?".</summary>
        public List<string> signTokens = new List<string>();

        /// <summary>Gemini가 변환한 자연스러운 한국어 문장. 변환 전이면 빈 문자열.</summary>
        public string sentence = string.Empty;

        /// <summary>발화 생성 시각 (Unix epoch milliseconds, UTC).</summary>
        public long timestampUnixMs;

        /// <summary>"?" 미인식 토큰을 하나라도 포함하는지(plan §7 별도 표시용).</summary>
        public bool hasUnrecognized;

        public UtteranceEntry() { }

        public UtteranceEntry(List<string> tokens, long timestampUnixMs)
        {
            signTokens = tokens ?? new List<string>();
            this.timestampUnixMs = timestampUnixMs;
            hasUnrecognized = signTokens.Contains(UnrecognizedToken);
        }

        public DateTime TimestampLocal =>
            DateTimeOffset.FromUnixTimeMilliseconds(timestampUnixMs).LocalDateTime;
    }
}
