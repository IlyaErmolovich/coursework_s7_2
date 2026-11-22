using UnityEngine;
using UnityEngine.UI;

namespace WinterDuel.UI
{
    /// <summary>Existing button selection, delegated to the local match when connected.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class HudActionSelection : MonoBehaviour
    {
        public enum Action { Snowball, IceSnowball, RestoreWarmth }

        public WinterDuel.Gameplay.DuelMatchController match;
        public Button snowballButton;
        public Button iceButton;
        public Button warmthButton;
        public Color normalColor = new Color(0.96f, 0.94f, 0.87f, 1);
        public Color selectedColor = new Color(0.64f, 0.78f, 0.75f, 1);
        public Action selectedAction = Action.Snowball;

        private void OnEnable() => Refresh();

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RefreshAfterValidation;
            UnityEditor.EditorApplication.delayCall += RefreshAfterValidation;
#endif
        }

#if UNITY_EDITOR
        private void RefreshAfterValidation()
        {
            if (this != null && isActiveAndEnabled) Refresh();
        }

        private void OnDestroy() => UnityEditor.EditorApplication.delayCall -= RefreshAfterValidation;
#endif

        public void SelectSnowball() { if (Application.isPlaying && match != null) match.ChooseSnowball(); else Select(Action.Snowball); }
        public void SelectIce() { if (Application.isPlaying && match != null) match.PrepareOrSelectIce(); else Select(Action.IceSnowball); }
        public void SelectWarmth() { if (Application.isPlaying && match != null) match.RecoverWarmth(); else Select(Action.RestoreWarmth); }

        private void Select(Action action)
        {
            selectedAction = action;
            Refresh();
        }

        [ContextMenu("Refresh Button Colors")]
        public void Refresh()
        {
            SetColor(snowballButton, Action.Snowball);
            SetColor(iceButton, Action.IceSnowball);
            SetColor(warmthButton, Action.RestoreWarmth);
        }

        private void SetColor(Button button, Action action)
        {
            if (button != null && button.image != null)
                button.image.color = selectedAction == action ? selectedColor : normalColor;
        }
    }
}
