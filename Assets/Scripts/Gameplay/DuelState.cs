using System;
namespace WinterDuel.Gameplay
{
    public enum MatchPhase { Ready, InFlight, Resolving, Finished }
    public enum ProjectileKind { Snow, Ice }
    [Serializable] public sealed class DuelPlayerState
    {
        public int Health { get; internal set; }
        public int Warmth { get; internal set; }
        public bool IceReady { get; internal set; }
    }
    /// <summary>Match rules independent of input, scene, physics and transport.</summary>
    public sealed class DuelState
    {
        private readonly DuelSettings settings;
        public DuelPlayerState[] Players { get; }=new[]{new DuelPlayerState(),new DuelPlayerState()};
        public int Active { get; private set; }
        public int Turn { get; private set; }=1;
        public int Winner { get; private set; }=-1;
        public MatchPhase Phase { get; private set; }=MatchPhase.Ready;
        public ProjectileKind Selected { get; private set; }
        public ProjectileKind FlyingKind { get; private set; }
        public DuelState(DuelSettings balance)
        {
            settings=balance;
            foreach(var p in Players){p.Health=settings.maxHealth;p.Warmth=settings.maxWarmth;}
        }
        public void ApplySnapshot(DuelSnapshot s)
        {
            Active=s.active;Turn=s.turn;Winner=s.winner;Phase=s.phase;Selected=s.selected;FlyingKind=s.flyingKind;
            for(int i=0;i<2;i++){Players[i].Health=s.health[i];Players[i].Warmth=s.warmth[i];Players[i].IceReady=s.ice[i];}
        }
        public bool SelectSnow()
        {
            if(Phase!=MatchPhase.Ready)return false;
            Selected=ProjectileKind.Snow;return true;
        }
        public bool PrepareOrSelectIce()
        {
            if(Phase!=MatchPhase.Ready)return false;
            var p=Players[Active];
            if(p.IceReady){Selected=ProjectileKind.Ice;return true;}
            if(p.Warmth<settings.iceCost)return false;
            p.Warmth-=settings.iceCost;p.IceReady=true;Phase=MatchPhase.Resolving;return true;
        }
        public bool RecoverWarmth()
        {
            if(Phase!=MatchPhase.Ready)return false;
            var p=Players[Active];p.Warmth=Math.Min(settings.maxWarmth,p.Warmth+settings.warmthRecovery);
            Phase=MatchPhase.Resolving;return true;
        }
        public bool BeginThrow()
        {
            if(Phase!=MatchPhase.Ready)return false;
            if(Selected==ProjectileKind.Ice && !Players[Active].IceReady)return false;
            FlyingKind=Selected;
            if(Selected==ProjectileKind.Ice)Players[Active].IceReady=false;
            Phase=MatchPhase.InFlight;return true;
        }
        public bool ResolveFlight(int target)
        {
            if(Phase!=MatchPhase.InFlight)return false;
            if(target>=0 && target<2 && target!=Active)
            {
                var p=Players[target];p.Health=Math.Max(0,p.Health-(FlyingKind==ProjectileKind.Ice?settings.iceDamage:settings.snowDamage));
                p.IceReady=false;
                if(p.Health==0){Winner=Active;Phase=MatchPhase.Finished;return true;}
            }
            Phase=MatchPhase.Resolving;return true;
        }
        public bool AdvanceTurn()
        {
            if(Phase!=MatchPhase.Resolving)return false;
            Active=1-Active;Turn++;Selected=ProjectileKind.Snow;Phase=MatchPhase.Ready;return true;
        }
    }
}
