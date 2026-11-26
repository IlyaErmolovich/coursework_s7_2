using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using WinterDuel.App;
using WinterDuel.Networking;
public static class VerifyConnectionLifecycle {
 static void Set(GameSession s,string name,object value){var p=typeof(GameSession).GetProperty(name);if(p!=null)p.SetValue(s,value);else typeof(GameSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,value);}
 static void Invoke(GameSession s,string name,object arg=null){typeof(GameSession).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,arg==null?null:new[]{arg});}
 static void Single(GameSession original){if(GameSession.Instance!=original||UnityEngine.Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length!=1||UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length!=1)throw new Exception("Duplicated session/EventSystem");}
 public static async Task<object> Main(){
  var s=GameSession.Instance;if(s==null)throw new Exception("Start from MainMenu");
  Set(s,"Mode",SessionMode.Local);bool wasMatch=s.InMatch;Invoke(s,"OnNative",new BluetoothEvent{type="disconnected",text="Постороннее событие Bluetooth"});
  if(s.InMatch!=wasMatch||s.Status=="Постороннее событие Bluetooth")throw new Exception("Bluetooth event interrupted local mode");
  Set(s,"Mode",SessionMode.Host);Set(s,"Handshake",true);Invoke(s,"OnNative",new BluetoothEvent{type="disconnected",text="Проверочный разрыв"});await Task.Delay(100);Single(s);
  if(s.Networked||s.InMatch||s.Status!="Проверочный разрыв")throw new Exception("Disconnect cleanup failed");
  for(int i=0;i<3;i++){s.CreateGame();s.Cancel();s.JoinGame();s.Cancel();await Task.Delay(25);Single(s);}
  Set(s,"Mode",SessionMode.Host);Set(s,"Handshake",true);Invoke(s,"OnApplicationPause",true);await Task.Delay(50);
  if(s.Networked||!s.Status.Contains("фон"))throw new Exception("Background cleanup failed");Single(s);
  Set(s,"Mode",SessionMode.Host);Set(s,"Handshake",true);typeof(ClassicBluetoothTransport).GetProperty("Connected").SetValue(s.transport,true);Set(s,"receivedAt",Time.unscaledTime-20);Invoke(s,"Update");
  if(s.Networked||!s.Status.Contains("не отвечает"))throw new Exception("Heartbeat timeout cleanup failed");Single(s);
  s.JoinGame();Invoke(s,"OnNative",new BluetoothEvent{type="error",text="Bluetooth выключен"});Invoke(s,"OnNative",new BluetoothEvent{type="scanDone",text="Поиск завершён"});
  if(s.Status!="Bluetooth выключен")throw new Exception("Discovery completion hid an actionable error");
  s.Cancel();return new{passed=true,disconnect=true,background=true,timeout=true,repeatedEntry=true,radioTested=false};
 }
}
