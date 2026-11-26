using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WinterDuel.App;
using WinterDuel.Gameplay;
using WinterDuel.Networking;
public static class VerifyClientInput {
 public static async Task<object> Main(){
  var s=GameSession.Instance;var m=UnityEngine.Object.FindFirstObjectByType<DuelMatchController>();
  if(s==null||m==null)throw new Exception("Start local game through menu");
  typeof(GameSession).GetProperty("Mode").SetValue(s,SessionMode.Client);typeof(GameSession).GetProperty("MatchReady").SetValue(s,true);
  m.InitializeReplica();m.State.RecoverWarmth();m.State.AdvanceTurn();m.cameraRig.Snap();await Task.Delay(100);
  var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();var start=new Vector2(Screen.width*.6f,Screen.height*.6f);var end=start-new Vector2(.64f,.768f)*Screen.height*.30f;
  DuelMessage sent=null;Action<string> observe=json=>{var p=JsonUtility.FromJson<DuelMessage>(json);if(p.type=="action")sent=p;};s.transport.ObservedSend+=observe;
  try{
   InputSystem.QueueStateEvent(mouse,new MouseState{position=start,buttons=1});await Task.Delay(100);
   InputSystem.QueueStateEvent(mouse,new MouseState{position=end,buttons=1});await Task.Delay(100);
   if(!m.input.IsDragging||m.input.PreviewVelocity.x>=0||m.input.PreviewVelocity.y<=0)throw new Exception("Mirrored drag failed");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=end,buttons=0});await Task.Delay(100);
   if(sent==null||sent.action!=DuelAction.Throw||sent.velocity.x>=0||sent.velocity.y<=0||m.ActiveProjectile!=null||!s.PendingAction||m.State.Phase!=MatchPhase.Ready)throw new Exception("Client must send command without simulating authority");
   return new{passed=true,velocity=sent.velocity,localPhysics=false,awaitingHost=s.PendingAction};
  }finally{s.transport.ObservedSend-=observe;InputSystem.QueueStateEvent(mouse,new MouseState{position=end,buttons=0});typeof(GameSession).GetProperty("Mode").SetValue(s,SessionMode.Local);typeof(GameSession).GetField("pendingAction",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(s,0L);m.NewBattle();}
 }
}
