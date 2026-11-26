using System;
using UnityEngine;
using WinterDuel.Environment;
using WinterDuel.Gameplay;
namespace WinterDuel.Networking
{
    public enum DuelAction { Snow, Ice, Warmth, Throw }
    [Serializable] public sealed class FieldSetup
    {
        public int seed; public float center,distance,leftHeight,rightHeight,leftWidth,rightWidth,blend,relief,minFeature,maxFeature;
        public float width,bottom; public int segments;
        public static FieldSetup Capture(FixedDuelFieldLayout f) => new FieldSetup{seed=f.seed,center=f.centerX,distance=f.playerDistance,leftHeight=f.leftPlatformHeight,rightHeight=f.rightPlatformHeight,leftWidth=f.platformWidth,rightWidth=f.rightPlatformWidth,blend=f.platformBlend,relief=f.terrainRelief,minFeature=f.minimumFeatureWidth,maxFeature=f.maximumFeatureWidth,width=f.foreground.width,bottom=f.foreground.bottomHeight,segments=f.foreground.segments};
        public void Apply(FixedDuelFieldLayout f)
        {
            f.seed=seed;f.centerX=center;f.playerDistance=distance;f.leftPlatformHeight=leftHeight;f.rightPlatformHeight=rightHeight;f.platformWidth=leftWidth;f.rightPlatformWidth=rightWidth;f.platformBlend=blend;f.terrainRelief=relief;f.minimumFeatureWidth=minFeature;f.maximumFeatureWidth=maxFeature;f.naturalTerrain=true;f.foreground.width=width;f.foreground.bottomHeight=bottom;f.foreground.segments=segments;
            // The same saved scene components are used on both devices; only data is synchronized.
            f.ApplyNetworkLayout();
        }
    }
    [Serializable] public sealed class DuelMessage
    {
        public int protocol=1,round,turn;
        public long sequence,acknowledged;
        public string type,session,balance;
        public bool ready,peerReady,matchReady;
        public DuelAction action;
        public Vector2 velocity;
        public FieldSetup field;
        public DuelSnapshot state;
    }
    /// <summary>Per-connection ordered stream gate; stale rounds, duplicates and off-turn commands cannot mutate rules.</summary>
    public sealed class HostCommandGate
    {
        public long LastSequence {get;private set;}
        public bool AcceptSequence(long sequence){if(sequence<=LastSequence)return false;LastSequence=sequence;return true;}
        public bool Allows(DuelMessage message,int round,DuelState state,DuelSettings settings)
        {
            if(message.round!=round||state==null||message.turn!=state.Turn||state.Active!=1||state.Phase!=MatchPhase.Ready)return false;
            if(!Enum.IsDefined(typeof(DuelAction),message.action))return false;
            if(message.action!=DuelAction.Throw)return true;
            var v=message.velocity;
            return !float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&v.magnitude>=settings.minimumSpeed-.01f&&v.magnitude<=settings.maximumSpeed+.01f;
        }
    }
}
