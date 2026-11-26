using System;
using UnityEngine;
namespace WinterDuel.Gameplay
{
    [Serializable] public sealed class DuelSnapshot
    {
        public int active,turn,winner;
        public MatchPhase phase;
        public ProjectileKind selected,flyingKind;
        public int[] health,warmth;
        public bool[] ice;
        public float[] x;
        public float wind;
        public bool projectile,focusResult;
        public Vector2 projectilePosition,projectileVelocity;
        public Vector3 resultPosition;
        public string status;
    }
}
