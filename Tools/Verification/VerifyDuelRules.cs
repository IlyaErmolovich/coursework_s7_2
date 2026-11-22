using System;
using UnityEngine;
using UnityEditor;
using WinterDuel.Gameplay;
public static class VerifyDuelRules
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    public static object Main()
    {
        var b=AssetDatabase.LoadAssetAtPath<DuelSettings>("Assets/Settings/Gameplay/LocalDuelBalance.asset");
        var s=new DuelState(b);Check(s.Players[0].Health==100 && s.Players[1].Warmth==50,"initial state");
        Check(s.PrepareOrSelectIce(),"prepare");Check(s.Players[0].IceReady && s.Players[0].Warmth==35,"ice cost");
        Check(!s.RecoverWarmth() && !s.BeginThrow(),"one action only");
        s.AdvanceTurn();s.BeginThrow();s.ResolveFlight(0);
        Check(!s.Players[0].IceReady && s.Players[0].Health==80,"hit breaks defender ice");
        Check(!s.ResolveFlight(0) && s.Players[0].Health==80,"duplicate damage rejected");
        s.AdvanceTurn();s.PrepareOrSelectIce();s.AdvanceTurn();s.BeginThrow();s.ResolveFlight(-1);s.AdvanceTurn();
        Check(s.Players[0].IceReady,"miss preserves defender preparation");
        Check(s.PrepareOrSelectIce() && s.Selected==ProjectileKind.Ice && s.Phase==MatchPhase.Ready,"select prepared ice without spending turn");
        Check(s.BeginThrow() && !s.Players[0].IceReady,"consume prepared ice on launch");
        Check(!s.BeginThrow() && !s.RecoverWarmth() && !s.AdvanceTurn(),"flight lock");
        s.ResolveFlight(1);Check(s.Players[1].Health==65,"ice damage");
        var fixture=new DuelState(b);
        for(int i=0;i<3;i++)
        {
            fixture.PrepareOrSelectIce();fixture.AdvanceTurn();fixture.BeginThrow();fixture.ResolveFlight(-1);fixture.AdvanceTurn();
            fixture.PrepareOrSelectIce();fixture.BeginThrow();fixture.ResolveFlight(-1);fixture.AdvanceTurn();fixture.BeginThrow();fixture.ResolveFlight(-1);fixture.AdvanceTurn();
        }
        Check(fixture.Players[0].Warmth==5,"repeated preparation drains warmth");
        Check(!fixture.PrepareOrSelectIce() && fixture.Phase==MatchPhase.Ready,"not enough warmth does not spend turn");
        fixture.RecoverWarmth();Check(fixture.Players[0].Warmth==20,"warmth recovery");
        var win=new DuelState(b);
        for(int i=0;i<5;i++){win.BeginThrow();win.ResolveFlight(1);if(i<4){win.AdvanceTurn();win.BeginThrow();win.ResolveFlight(-1);win.AdvanceTurn();}}
        Check(win.Phase==MatchPhase.Finished && win.Winner==0 && win.Players[1].Health==0,"full match victory");
        Check(!win.BeginThrow() && !win.AdvanceTurn() && !win.PrepareOrSelectIce(),"finished match locked");
        var redWin=new DuelState(b);redWin.BeginThrow();redWin.ResolveFlight(-1);redWin.AdvanceTurn();
        for(int i=0;i<5;i++){redWin.BeginThrow();redWin.ResolveFlight(0);if(i<4){redWin.AdvanceTurn();redWin.BeginThrow();redWin.ResolveFlight(-1);redWin.AdvanceTurn();}}
        Check(redWin.Phase==MatchPhase.Finished && redWin.Winner==1 && redWin.Players[0].Health==0,"second player victory");
        var cap=new DuelState(b);cap.RecoverWarmth();Check(cap.Players[0].Warmth==b.maxWarmth,"warmth is capped");
        var fresh=new DuelState(b);Check(fresh.Active==0 && fresh.Turn==1 && fresh.Winner==-1 && fresh.Players[1].Health==100 && !fresh.Players[0].IceReady,"new match resets rules");
        return new{checks,passed=true};
    }
}
