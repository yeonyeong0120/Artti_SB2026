using UnityEditor;
using UnityEngine;
using Artti.SignBridge.UI;

namespace Artti.SignBridge.EditorTools
{
    /// <summary>
    /// 단일 씬 패널 구조에서 화면을 하나씩 보기 위한 에디터 도우미.
    /// 메뉴 Artti > Screens 에서 한 화면만 켜고 나머지는 끈다(편집·검수용).
    /// 실제 런타임 전환은 ScreenManager가 담당하므로 이 토글은 보기 편의일 뿐이다.
    /// </summary>
    public static class ScreenIsolator
    {
        static void Isolate(AppScreen only)
        {
            var panels = Object.FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (panels.Length == 0)
            {
                Debug.LogWarning("[ScreenIsolator] 씬에 UIScreen 패널이 없습니다. 빌더로 먼저 생성하세요.");
                return;
            }
            foreach (var p in panels)
                p.gameObject.SetActive(p.Screen == only);
            Debug.Log($"[ScreenIsolator] '{only}'만 표시.");
        }

        static void ShowAll()
        {
            foreach (var p in Object.FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.gameObject.SetActive(true);
        }

        [MenuItem("Artti/Screens/Show Only — Splash")]             static void Splash()    => Isolate(AppScreen.Splash);
        [MenuItem("Artti/Screens/Show Only — Camera Permission")]  static void Camera()    => Isolate(AppScreen.CameraPermission);
        [MenuItem("Artti/Screens/Show Only — Home")]               static void Home()      => Isolate(AppScreen.Home);
        [MenuItem("Artti/Screens/Show Only — Recognition")]        static void Recog()     => Isolate(AppScreen.Recognition);
        [MenuItem("Artti/Screens/Show Only — Session Log")]        static void SessLog()   => Isolate(AppScreen.SessionLog);
        [MenuItem("Artti/Screens/Show Only — All Logs")]           static void AllLogs()   => Isolate(AppScreen.AllLogs);
        [MenuItem("Artti/Screens/Show Only — Settings")]           static void Settings()  => Isolate(AppScreen.Settings);
        [MenuItem("Artti/Screens/Show All")]                       static void All()       => ShowAll();
    }
}
