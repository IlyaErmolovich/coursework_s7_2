using System;
using System.Threading.Tasks;
using UnityEngine;
using WinterDuel.App;
using WinterDuel.Gameplay;
using WinterDuel.Networking;
public static class VerifyNetworkPause
{
 public static async Task<object> Main(){
  var s=GameSession.Instance;var m=UnityEngine.Object.FindFirstObjectByType<DuelMatchController>();
  if(s==null||m==null)throw new Exception("Start a local game through MainMenu");
  typeof(GameSession).GetProperty("Mode").SetValue(s,SessionMode.Host);
  try{
   m.NewBattle();m.State.RecoverWarmth();m.State.AdvanceTurn();m.cameraRig.Snap();m.ToggleMenu();
   if(!m.Paused||Time.timeScale!=1)throw new Exception("Local network menu paused simulation");
   if(!m.ApplyAuthoritativeAction(1,DuelAction.Warmth,Vector2.zero))throw new Exception("Opponent action rejected because host menu is open");
   if(m.ResultFollowsFlight)throw new Exception("Warmth must not pan either camera to the host view");
   for(int i=0;i<150&&m.State.Phase==MatchPhase.Resolving;i++)await Task.Delay(20);
   if(m.State.Active!=0||m.State.Phase!=MatchPhase.Ready||m.CanAim)throw new Exception("Network turn did not advance behind local menu");
   m.Resume();if(m.Paused||Time.timeScale!=1)throw new Exception("Resume failed");
   return new{passed=true,opponentActionAccepted=true,turnAdvanced=true,cameraStayedWithOwnPlayer=true};
  }finally{typeof(GameSession).GetProperty("Mode").SetValue(s,SessionMode.Local);m.NewBattle();}
 }
}
