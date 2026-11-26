using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WinterDuel.Gameplay;
using WinterDuel.Environment;
public static class VerifyFixedField
{
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 static DuelMatchController M => UnityEngine.Object.FindFirstObjectByType<DuelMatchController>();
 static Vector2 Aim(DuelMatchController m,int player,int steps,float wind){
  Vector2 delta=(Vector2)m.characters[1-player].hitbox.bounds.center-m.characters[player].LaunchOrigin;
  float dt=Time.fixedDeltaTime,t=steps*dt;
  return delta/t-new Vector2(wind,-m.settings.gravity)*(dt*(steps+1)*.5f);
 }
 static int PredictedHit(DuelMatchController m,int owner,Vector2 v,float wind){
  var p=m.characters[owner].LaunchOrigin;var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.AllLayers);filter.useTriggers=false;
  var hits=new RaycastHit2D[16];
  for(int n=0;n<600;n++){
   var next=p;ShotMotion.Step(ref next,ref v,Time.fixedDeltaTime,m.settings.gravity,wind);var d=next-p;
   int count=Physics2D.CircleCast(p,.17f,d.normalized,filter,hits,d.magnitude);float best=float.MaxValue;int victim=-2;
   for(int i=0;i<count;i++){var c=hits[i].collider.GetComponentInParent<DuelCharacter>();if(c!=null&&c.playerIndex==owner)continue;if(hits[i].distance<best){best=hits[i].distance;victim=c==null?-1:c.playerIndex;}}
   if(victim!=-2)return victim;p=next;
   if(p.x<m.settings.fieldLeft||p.x>m.settings.fieldRight||p.y<m.settings.minimumY||p.y>m.settings.maximumY)return -1;
  }return -1;
 }
 static float FootError(DuelCharacter c){float worst=0;foreach(var f in new[]{c.leftFoot,c.rightFoot}){Vector3 p=c.transform.TransformPoint(f);worst=Mathf.Max(worst,Mathf.Abs(p.y-c.ground.GetWorldSurfacePoint(p.x).y));}return worst;}
 public static async Task<object> Main(){
  var m=M;Check(Application.isPlaying,"Run in Play Mode");var output=new List<object>();
  try{
   for(int player=0;player<2;player++){
    m.NewBattle();if(player==1){m.State.RecoverWarmth();m.State.AdvanceTurn();m.cameraRig.Snap();}
    float wind=player==0?-m.settings.maximumWind:m.settings.maximumWind;
    typeof(DuelMatchController).GetProperty("Wind").SetValue(m,wind);
    var valid=new List<int>();float minAngle=90,maxAngle=0,minSpeed=100,maxSpeed=0;
    for(int n=90;n<=280;n++){var v=Aim(m,player,n,wind);if(v.magnitude>m.settings.maximumSpeed)continue;if(PredictedHit(m,player,v,wind)!=1-player)continue;valid.Add(n);float a=Mathf.Atan2(v.y,Mathf.Abs(v.x))*Mathf.Rad2Deg;minAngle=Mathf.Min(minAngle,a);maxAngle=Mathf.Max(maxAngle,a);minSpeed=Mathf.Min(minSpeed,v.magnitude);maxSpeed=Mathf.Max(maxSpeed,v.magnitude);}
    Check(valid.Count>60,"Useful range of successful trajectories");
    output.Add(new{player,wind,solutions=valid.Count,minAngle,maxAngle,minSpeed,maxSpeed});
    for(int shot=0;shot<5;shot++){
     m.NewBattle();if(player==1){m.State.RecoverWarmth();m.State.AdvanceTurn();m.cameraRig.Snap();}
     typeof(DuelMatchController).GetProperty("Wind").SetValue(m,wind);
     bool high=shot<3;int n=valid[Mathf.RoundToInt((valid.Count-1)*new[]{.2f,.55f,.85f}[shot%3])];
     Vector2 v=high?Aim(m,player,n,wind):new Vector2((player==0?1:-1)*Mathf.Cos((shot==3?15:20)*Mathf.Deg2Rad),Mathf.Sin((shot==3?15:20)*Mathf.Deg2Rad))*m.settings.maximumSpeed;
     Check(m.TryLaunch(v),"Launch accepted");Time.timeScale=3;
     float maxY=-100,maxCameraY=-100;var end=DateTime.UtcNow.AddSeconds(12);
     while(m.ActiveProjectile!=null&&DateTime.UtcNow<end){maxY=Mathf.Max(maxY,m.ActiveProjectile.transform.position.y);maxCameraY=Mathf.Max(maxCameraY,m.cameraRig.transform.position.y);var cam=m.cameraRig.gameCamera;Check(Mathf.Abs(cam.transform.position.x)+cam.orthographicSize*cam.aspect<50,"Horizontal backdrop coverage");Check(cam.transform.position.y+cam.orthographicSize<60,"Sky covers high camera");await Task.Delay(25);}
     Check(m.ActiveProjectile==null,"Flight completed");bool hit=m.State.Players[1-player].Health==80;
     if(high)Check(hit,"High arc reaches the opponent");

     if(high)Check(maxCameraY>2,"Camera follows the arc");
     float feet=Mathf.Max(FootError(m.characters[0]),FootError(m.characters[1]));Check(feet<.025f,"Feet remain on snow after knockback");
     output.Add(new{player,shot,high,velocity=v.ToString("F3"),hit,maxY,maxCameraY,impact=m.ResultPosition.ToString("F3"),feet});
    }
    // Repeated knockback and extreme requested placements stay on the authored pads.
    var c=m.characters[player];c.Place(-1000);Check(FootError(c)<.025f&&c.transform.position.x==c.placementMinX,"Left pad limit");c.Place(1000);Check(FootError(c)<.025f&&c.transform.position.x==c.placementMaxX,"Right pad limit");
   }
   return output;
  }finally{m.NewBattle();Time.timeScale=1;}
 }
}
