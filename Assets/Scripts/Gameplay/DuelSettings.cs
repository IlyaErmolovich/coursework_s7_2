using UnityEngine;
namespace WinterDuel.Gameplay
{
    [CreateAssetMenu(menuName="Winter Duel/Balance")]
    public sealed class DuelSettings : ScriptableObject
    {
        [Header("Match")]
        [Min(1)] public int maxHealth=100;
        [Min(1)] public int maxWarmth=50;
        [Min(1)] public int snowDamage=20, iceDamage=35;
        [Min(1)] public int iceCost=15, warmthRecovery=15;
        [Header("Slingshot (pixels at 1080 screen height)")]
        [Min(1)] public float minimumDrag=28, maximumDrag=400;
        [Min(0.1f)] public float minimumSpeed=6, maximumSpeed=24;
        [Min(0.1f)] public float gravity=9.81f;
        [Header("Wind acceleration")]
        [Min(0)] public float maximumWind=1.8f;
        public int windSeed=7341;
        [Min(0)] public float snowWindSpeed=0.7f;
        [Header("Flight safety")]
        [Min(1)] public float flightTimeout=12;
        public float fieldLeft=-38, fieldRight=38, minimumY=-12, maximumY=42;
        [Header("Impact and turns")]
        [Min(0)] public float knockback=0.55f;
        public float playerLeftLimit=-20, playerRightLimit=28;
        [Min(1)] public float minimumPlayerDistance=4;
        [Min(0)] public float resultDelay=0.8f;
        [Header("Short trajectory hint")]
        [Range(0.1f,1)] public float previewDuration=0.42f;
        [Min(1)] public float previewDistance=5.5f;
        [Header("Camera coverage")]
        public float cameraLeft=-24, cameraRight=24, cameraTop=30;
        [Min(0.05f)] public float cameraSmoothTime=0.32f;
    }
}
