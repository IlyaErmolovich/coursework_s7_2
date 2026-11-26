using System;
using UnityEngine;
using WinterDuel.Environment;
public static class VerifyNaturalTerrain
{
 public static object Main(){
  var f=UnityEngine.Object.FindFirstObjectByType<FixedDuelFieldLayout>();var g=f.foreground;
  if(!g.fieldProfile.naturalTerrain||f.playerDistance!=38.25f)throw new Exception("Wrong field profile");
  var outline=g.GetComponent<PolygonCollider2D>().GetPath(0);float error=0;
  for(int i=0;i<=g.segments;i++)error=Mathf.Max(error,Vector3.Distance(g.transform.TransformPoint(outline[i]),g.GetWorldSurfacePoint(outline[i].x)));
  if(error>.001f)throw new Exception("Collision does not match visible surface");
  int seed=g.seed;float changed=0,minimum=100,maximum=-100,slope=0,previous=g.EvaluateLocalHeight(-8);
  try{
   for(int i=0;i<250;i++){
    float x=-8+i*.1f;g.seed=seed;float a=g.EvaluateLocalHeight(x);g.seed=seed+37;float b=g.EvaluateLocalHeight(x);g.seed=seed;
    if(g.EvaluateLocalHeight(x)!=a)throw new Exception("Seed was not reproducible");changed+=Mathf.Abs(a-b);minimum=Mathf.Min(minimum,a);maximum=Mathf.Max(maximum,a);slope=Mathf.Max(slope,Mathf.Abs(a-previous)/.1f);previous=a;
   }
   if(changed<1||maximum-minimum>3||slope>1.5f)throw new Exception("Invalid natural relief");
   return new{passed=true,seed,reproducible=true,collisionError=error,centralRelief=maximum-minimum,maximumSlope=slope,distance=f.playerDistance};
  }finally{g.seed=seed;}
 }
}
