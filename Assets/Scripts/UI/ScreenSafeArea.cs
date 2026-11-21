using UnityEngine;

namespace WinterDuel.UI
{
    /// <summary>Fits an existing HUD RectTransform to the device safe area.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class ScreenSafeArea : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private RectTransform target;

        private void OnEnable() => Refresh();

        private void Update()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height ||
                lastSafeArea != Screen.safeArea)
                Refresh();
        }

        [ContextMenu("Refresh Safe Area")]
        public void Refresh()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            if (target == null) target = GetComponent<RectTransform>();
            Rect area = Screen.safeArea;
            // Some editor render targets report an empty safe area during a resize.
            if (area.width <= 0 || area.height <= 0)
                area = new Rect(0, 0, Screen.width, Screen.height);
            target.anchorMin = new Vector2(Mathf.Clamp01(area.xMin / Screen.width), Mathf.Clamp01(area.yMin / Screen.height));
            target.anchorMax = new Vector2(Mathf.Clamp01(area.xMax / Screen.width), Mathf.Clamp01(area.yMax / Screen.height));
            target.offsetMin = target.offsetMax = Vector2.zero;
            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
