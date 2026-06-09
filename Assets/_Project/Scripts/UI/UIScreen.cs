using UnityEngine;

namespace Artti.SignBridge.UI
{
    /// <summary>
    /// 모든 화면 패널의 베이스. 패널 GameObject 활성/비활성으로 표시되며
    /// 전환 시 OnShow/OnHide 훅이 호출된다.
    /// 비주얼 레이아웃(배경 PNG·버튼 배치)은 디자인 수령 후 인스펙터에서 구성한다.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private AppScreen _screen;

        /// <summary>이 패널이 담당하는 화면.</summary>
        public AppScreen Screen => _screen;

        /// <summary>표시될 때 호출. 데이터 바인딩/갱신은 여기서.</summary>
        public virtual void OnShow() { }

        /// <summary>가려질 때 호출. 정리/구독 해제는 여기서.</summary>
        public virtual void OnHide() { }
    }
}
