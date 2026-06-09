using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Artti.SignBridge.Data
{
    /// <summary>
    /// 세션 기록을 기기 로컬(JSON)에 저장/로드한다. 외부 서버 전송 없음(plan §7).
    /// persistentDataPath 아래 sessions.json 단일 파일로 보관한다.
    /// </summary>
    public static class SessionStore
    {
        [Serializable]
        private class SessionList
        {
            public List<ClassSession> sessions = new List<ClassSession>();
        }

        private const string FileName = "sessions.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>저장된 모든 세션을 최신순(시작 시각 내림차순)으로 반환.</summary>
        public static List<ClassSession> LoadAll()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new List<ClassSession>();

                string json = File.ReadAllText(FilePath);
                SessionList wrapper = JsonUtility.FromJson<SessionList>(json);
                List<ClassSession> list = wrapper?.sessions ?? new List<ClassSession>();
                list.Sort((a, b) => b.startedUnixMs.CompareTo(a.startedUnixMs));
                return list;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SessionStore] 로드 실패: {e.Message}");
                return new List<ClassSession>();
            }
        }

        /// <summary>세션 하나를 저장한다. 같은 id가 이미 있으면 갱신, 없으면 추가.</summary>
        public static void Save(ClassSession session)
        {
            if (session == null) return;

            List<ClassSession> all = LoadAll();
            int idx = all.FindIndex(s => s.id == session.id);
            if (idx >= 0) all[idx] = session;
            else all.Add(session);

            Persist(all);
        }

        /// <summary>지정 세션을 삭제한다.</summary>
        public static void Delete(string sessionId)
        {
            List<ClassSession> all = LoadAll();
            all.RemoveAll(s => s.id == sessionId);
            Persist(all);
        }

        /// <summary>
        /// 디바이스에 저장된 전체 세션 기록을 삭제한다(설정 > 전체 기록 삭제, 90·91.png).
        /// 외부 서버 전송이 없으므로 로컬 sessions.json 제거가 곧 전체 삭제다. 되돌릴 수 없음.
        /// </summary>
        public static void ClearAll()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SessionStore] 전체 기록 삭제 실패: {e.Message}");
            }
        }

        private static void Persist(List<ClassSession> all)
        {
            try
            {
                var wrapper = new SessionList { sessions = all };
                File.WriteAllText(FilePath, JsonUtility.ToJson(wrapper, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SessionStore] 저장 실패: {e.Message}");
            }
        }
    }
}
