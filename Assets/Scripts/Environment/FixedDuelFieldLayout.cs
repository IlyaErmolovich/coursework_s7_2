using UnityEngine;
using WinterDuel.Gameplay;

namespace WinterDuel.Environment
{
    /// <summary>One authored, reproducible field. Changes are applied in Edit Mode, not each turn.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class FixedDuelFieldLayout : MonoBehaviour
    {
        [Header("Existing scene references")]
        public SnowTerrain foreground;
        public DuelCharacter leftPlayer, rightPlayer;

        [Header("Fixed field (world units)")]
        public int seed = 7341;
        public float centerX = 4.25f;
        [Min(18)] public float playerDistance = 38.25f;
        [Min(4)] public float platformWidth = 10;
        public float leftPlatformHeight = -2.8f, rightPlatformHeight = -2.35f;
        [Min(0.1f)] public float platformBlend = 2;
        public SnowTerrain.DuelHill[] hills = {
            new SnowTerrain.DuelHill {positionBetweenPlayers=0.375f,height=6,width=19},
            new SnowTerrain.DuelHill {positionBetweenPlayers=0.65f,height=5.5f,width=16}
        };
        [SerializeField, HideInInspector] private int appliedSignature;
        public float LeftX => centerX - playerDistance * 0.5f;
        public float RightX => centerX + playerDistance * 0.5f;

        private int Signature()
        {
            unchecked
            {
                int h = seed;
                foreach (float value in new[] {centerX, playerDistance, platformWidth, leftPlatformHeight,
                    rightPlatformHeight, platformBlend, foreground.width, foreground.segments})
                    h = h * 31 + value.GetHashCode();
                foreach (var hill in hills)
                {h = h * 31 + hill.positionBetweenPlayers.GetHashCode(); h = h * 31 + hill.height.GetHashCode(); h = h * 31 + hill.width.GetHashCode();}
                return h;
            }
        }

        private void OnEnable() => ApplyIfChanged();
#if UNITY_EDITOR
        private void Update() { if (!Application.isPlaying) ApplyIfChanged(); }
        private void OnValidate()
        {
            platformWidth = Mathf.Max(4, platformWidth);
            playerDistance = Mathf.Max(platformWidth + 8, playerDistance);
            platformBlend = Mathf.Max(0.1f, platformBlend);
            if (hills == null) hills = new SnowTerrain.DuelHill[0];
            for (int i = 0; i < hills.Length; i++)
            {
                hills[i].positionBetweenPlayers = Mathf.Clamp01(hills[i].positionBetweenPlayers);
                hills[i].height = Mathf.Max(0, hills[i].height);
                hills[i].width = Mathf.Max(1, hills[i].width);
            }
            UnityEditor.EditorApplication.delayCall -= ApplyIfChanged;
            UnityEditor.EditorApplication.delayCall += ApplyIfChanged;
        }
        private void OnDestroy() => UnityEditor.EditorApplication.delayCall -= ApplyIfChanged;
#endif

        private void ApplyIfChanged()
        {
            if (this == null || Application.isPlaying || foreground == null || leftPlayer == null || rightPlayer == null) return;
            if (appliedSignature != Signature() || !foreground.fieldProfile.enabled ||
                leftPlayer.initialX != LeftX || rightPlayer.initialX != RightX) ApplyLayout();
        }

        [ContextMenu("Apply Fixed Field Layout")]
        public void ApplyLayout()
        {
            if (foreground == null || leftPlayer == null || rightPlayer == null || Application.isPlaying) return;
            var p = foreground.fieldProfile;
            p.enabled = true; p.leftX = LeftX; p.rightX = RightX;
            p.leftHeight = leftPlatformHeight; p.rightHeight = rightPlatformHeight;
            p.padHalfWidth = platformWidth * 0.5f; p.padBlend = platformBlend;
            p.hills = (SnowTerrain.DuelHill[])hills.Clone();
            foreground.seed = seed;
            foreground.Rebuild();
            ConfigurePlayer(leftPlayer, LeftX);
            ConfigurePlayer(rightPlayer, RightX);
            appliedSignature = Signature();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(foreground);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }

        private void ConfigurePlayer(DuelCharacter player, float x)
        {
            player.initialX = x; player.ground = foreground;
            player.constrainPlacement = true;
            float room = Mathf.Max(0.5f, platformWidth * 0.5f - 0.6f);
            player.placementMinX = x - room; player.placementMaxX = x + room;
            player.Place(x);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(player);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
#endif
        }
    }
}
