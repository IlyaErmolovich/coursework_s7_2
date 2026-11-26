using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using WinterDuel.App;
using WinterDuel.Gameplay;
using WinterDuel.Networking;
public static class VerifyNetworkSession
{
 static int checks;
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
 static void Set(GameSession s,string name,object value){var t=typeof(GameSession);var p=t.GetProperty(name);if(p!=null)p.SetValue(s,value);else t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,value);}
 static void Receive(GameSession s,DuelMessage message){typeof(GameSession).GetMethod("Receive",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,new object[]{JsonUtility.ToJson(message)});}
 static DuelMessage Message(string type,long seq,int round,int turn=0)=>new DuelMessage{type=type,sequence=seq,round=round,turn=turn,session="protocol-verification"};
 public static async Task<object> Main()
 {
  checks=0;var s=GameSession.Instance;var m=UnityEngine.Object.FindFirstObjectByType<DuelMatchController>();
  Check(Application.isPlaying&&s!=null&&m!=null,"Start a local match from MainMenu first");
  var packets=new List<DuelMessage>();Action<string> observed=json=>packets.Add(JsonUtility.FromJson<DuelMessage>(json));s.transport.ObservedSend+=observed;
  try{
   Set(s,"Mode",SessionMode.Host);Set(s,"sessionId","protocol-verification");Set(s,"match",m);Set(s,"InMatch",false);Set(s,"MatchReady",false);Set(s,"gate",new HostCommandGate());Set(s,"Round",0);
   Receive(s,Message("helloAck",1,0));Check(s.Handshake&&s.Page==MenuPage.Lobby,"Handshake reaches lobby");
   var ready=Message("ready",2,0);ready.ready=true;Receive(s,ready);s.ToggleReady();Check(s.CanStart,"Both ready enables host start");
   s.StartMatch();Check(s.Round==1&&!s.MatchReady&&m.State.Turn==1,"Host starts agreed round and waits for scene");
   Receive(s,Message("loaded",3,1));var setup=packets.Last(p=>p.type=="setup");Check(setup.field.seed==7341&&setup.field.distance==38.25f&&setup.state.health[0]==100,"Initial terrain, balance and state serialized");
   Receive(s,Message("setupAck",4,1));Check(s.MatchReady,"Client setup acknowledgement enables match");
   var invalid=Message("action",5,1,1);invalid.action=DuelAction.Warmth;Receive(s,invalid);Check(m.State.Turn==1&&m.State.Phase==MatchPhase.Ready,"Client rejected outside its turn");
   m.ToggleMenu();Check(m.Paused&&Time.timeScale==1,"Network menu does not pause simulation");m.Resume();
   Check(s.Submit(DuelAction.Warmth,Vector2.zero),"Host action accepted");Time.timeScale=3;
   for(int i=0;i<100&&m.State.Active!=1;i++)await Task.Delay(20);
   Check(m.State.Active==1&&m.State.Turn==2,"Host advances turn once");
   var bad=Message("action",6,1,2);bad.action=DuelAction.Throw;bad.velocity=new Vector2(1000,1000);Receive(s,bad);Check(m.State.Phase==MatchPhase.Ready,"Excess power rejected");
   Vector2 delta=(Vector2)m.characters[0].hitbox.bounds.center-m.characters[1].LaunchOrigin;int n=160;float dt=Time.fixedDeltaTime;
   var launch=Message("action",7,1,2);launch.action=DuelAction.Throw;launch.velocity=delta/(n*dt)-new Vector2(m.Wind,-m.settings.gravity)*dt*(n+1)*.5f;
   Receive(s,launch);var original=m.ActiveProjectile;Check(original!=null&&m.State.Phase==MatchPhase.InFlight,"Client command launches authoritative projectile");
   Receive(s,launch);Check(m.ActiveProjectile==original,"Duplicate command does not launch twice");
   var flying=m.CaptureSnapshot();
   for(int i=0;i<400&&m.ActiveProjectile!=null;i++)await Task.Delay(20);
   Check(m.State.Players[0].Health==80,"Only host applies hit damage");var resolved=m.CaptureSnapshot();
   Receive(s,launch);Check(m.State.Players[0].Health==80,"Duplicate remains harmless after resolution");
   Set(s,"Mode",SessionMode.Client);m.InitializeReplica();m.ApplySnapshot(flying);
   Check(m.ActiveProjectile!=null&&!m.ActiveProjectile.enabled&&!m.ActiveProjectile.GetComponent<Rigidbody2D>().simulated&&!m.ActiveProjectile.GetComponent<Collider2D>().enabled,"Replica has no authoritative flight physics");
   m.ApplySnapshot(resolved);Check(m.State.Players[0].Health==80&&m.ActiveProjectile==null,"Replica applies authoritative result");
   m.cameraRig.Snap();var own=m.cameraRig.gameCamera.WorldToViewportPoint(m.characters[1].transform.position);var enemy=m.cameraRig.gameCamera.WorldToViewportPoint(m.characters[0].transform.position);
   Check(own.x<.4f&&enemy.x>1,"Client sees own player left and enemy right");
   var snapshot=JsonUtility.FromJson<DuelSnapshot>(JsonUtility.ToJson(resolved));m.ApplySnapshot(snapshot);Check(m.State.Turn==resolved.turn&&m.State.Players[0].Warmth==resolved.warmth[0],"State serialization round trip");
   // Client cannot resolve damage even if a stale callback occurs.
   m.FlightFinished(null,0,Vector2.zero);Check(m.State.Players[0].Health==80,"Replica ignores local resolution callback");
   Set(s,"Mode",SessionMode.Host);m.NewBattle();
   for(int i=0;i<5;i++){m.State.BeginThrow();m.State.ResolveFlight(1);if(i<4){m.State.AdvanceTurn();m.State.BeginThrow();m.State.ResolveFlight(-1);m.State.AdvanceTurn();}}
   m.hud.ShowVictory();s.RequestReplay();Check(s.Round==1&&s.ReplayReady,"Replay waits for second participant");
   var replay=Message("replay",8,1);replay.ready=true;Receive(s,replay);Check(s.Round==2&&m.State.Players[1].Health==100&&!s.MatchReady,"Both replay votes reset the authoritative round");
   Receive(s,Message("loaded",9,2));Receive(s,Message("setupAck",10,2));var stale=Message("action",11,1,1);stale.action=DuelAction.Warmth;Receive(s,stale);Check(m.State.Phase==MatchPhase.Ready&&m.State.Turn==1,"Previous-round action rejected");
   m.NewBattle();Set(s,"Mode",SessionMode.Local);Set(s,"Handshake",false);Set(s,"MatchReady",true);m.cameraRig.Snap();
   return new{checks,passed=true,packets=packets.Count,radioConnectionTested=false};
  }finally{s.transport.ObservedSend-=observed;Set(s,"Mode",SessionMode.Local);Set(s,"Handshake",false);Set(s,"MatchReady",true);if(m!=null)m.NewBattle();Time.timeScale=1;}
 }
}
