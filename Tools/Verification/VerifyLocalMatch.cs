using System;
using System.Threading.Tasks;
using UnityEngine;
using WinterDuel.Gameplay;
public static class VerifyLocalMatch
{
 public static async Task<object> Main(){
  var m=UnityEngine.Object.FindFirstObjectByType<DuelMatchController>();m.NewBattle();Time.timeScale=3;int hits=0;
  try{
   for(int turn=0;turn<9;turn++){
    for(int j=0;j<250&&!m.CanAim;j++)await Task.Delay(20);
    if(!m.CanAim)throw new Exception("Camera/input never became ready");
    if(m.State.Active==1)m.RecoverWarmth();
    else{
     int n=160;float dt=Time.fixedDeltaTime;Vector2 delta=(Vector2)m.characters[1].hitbox.bounds.center-m.characters[0].LaunchOrigin;
     Vector2 velocity=delta/(n*dt)-new Vector2(m.Wind,-m.settings.gravity)*dt*(n+1)*.5f;
     if(!m.TryLaunch(velocity))throw new Exception("Launch rejected");
     int hp=m.State.Players[1].Health;
     for(int j=0;j<400&&m.ActiveProjectile!=null;j++)await Task.Delay(20);
     if(m.State.Players[1].Health!=hp-20)throw new Exception("Expected authoritative hit");hits++;
    }
    if(turn<8){int old=m.State.Turn;for(int j=0;j<300&&m.State.Turn==old;j++)await Task.Delay(20);}
   }
   if(m.State.Phase!=MatchPhase.Finished||m.State.Winner!=0||!m.hud.victoryPanel.activeSelf)throw new Exception("Victory missing");
   return new{passed=true,hits,turns=m.State.Turn,winner=m.State.Winner,resultPanel=true};
  }finally{Time.timeScale=1;}
 }
}
