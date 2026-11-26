using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Diagnostics;using System.Threading.Tasks;
using UnityEngine;using WinterDuel.UI;
public static class VerifySmokeContinuity {
 [Serializable] public class Sample{public double seconds;public float simulationTime,coverageMean,meanPixelChange,maxPixelChange;public int particles,tracked;public float maxSpeed;}
 [Serializable] public class Report{public double seconds;public int frames,initialParticles,minParticles=10000,maxParticles,restarts;public float maxTrackedSpeed;public bool passed;public List<Sample> samples=new List<Sample>();}
 public static async Task<object> Main(){
 if(!Application.isPlaying)throw new Exception("Run MainMenu in Play Mode");
 var original=Camera.main;var fit=original.GetComponent<MenuBackdropFit>();var ps=fit.chimneyEmitter.GetComponent<ParticleSystem>();var composite=original.GetComponent<MenuSmokeComposite>();
 if(!ps.main.prewarm||!ps.main.loop||ps.emission.burstCount!=0)throw new Exception("Expected prewarmed continuous emission");
 var dir=Path.GetFullPath(".utmp/smoke-motion");Directory.CreateDirectory(dir);
 var report=new Report{initialParticles=ps.particleCount};int minimum=Mathf.FloorToInt(ps.emission.rateOverTime.constant*ps.main.startLifetime.constantMin)-2;int maximum=Mathf.CeilToInt(ps.emission.rateOverTime.constant*ps.main.startLifetime.constantMax)+2;
 if(report.initialParticles<minimum)throw new Exception("Plume not formed on entry");
 var go=new GameObject("Smoke motion verification camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(original);camera.enabled=false;camera.orthographicSize=1.2f;camera.aspect=1;
 camera.transform.position=new Vector3(fit.chimneyEmitter.position.x+.25f,fit.chimneyEmitter.position.y+.6f,original.transform.position.z);
 var rt=new RenderTexture(512,512,16,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
 var frame=new Texture2D(512,512,TextureFormat.RGB24,false);var mask=new Texture2D(512,512,TextureFormat.RGBA32,false);var oldActive=RenderTexture.active;
 var particles=new ParticleSystem.Particle[ps.main.maxParticles];var previous=new Dictionary<uint,ParticleSystem.Particle>();byte[] previousAlpha=null;var clock=Stopwatch.StartNew();
 try{
 while(clock.Elapsed.TotalSeconds<18){
  var sample=new Sample{seconds=clock.Elapsed.TotalSeconds,simulationTime=ps.time};
  if(!ps.isPlaying)throw new Exception("Particle system stopped");
  // The test never calls Play, Stop, Clear, Emit or Simulate.
  // A normal duration wrap is not a restart; retained live particles identify continuity.
  int count=ps.GetParticles(particles);sample.particles=count;report.minParticles=Math.Min(report.minParticles,count);report.maxParticles=Math.Max(report.maxParticles,count);
  var next=new Dictionary<uint,ParticleSystem.Particle>();
  for(int j=0;j<count;j++){var p=particles[j];next[p.randomSeed]=p;if(previous.TryGetValue(p.randomSeed,out var before)){float ageDelta=before.remainingLifetime-p.remainingLifetime;if(ageDelta>0.001f){sample.tracked++;sample.maxSpeed=Mathf.Max(sample.maxSpeed,Vector3.Distance(before.position,p.position)/ageDelta);}}}
  if(previous.Count>0&&sample.tracked<count/2)report.restarts++;
  previous=next;report.maxTrackedSpeed=Mathf.Max(report.maxTrackedSpeed,sample.maxSpeed);
  composite.RefreshAndRender();RenderTexture.active=composite.coverage;mask.ReadPixels(new Rect(0,0,512,512),0,0);mask.Apply();var pixels=mask.GetPixels32();var alpha=new byte[pixels.Length];double total=0,diff=0;
  for(int j=0;j<pixels.Length;j++){alpha[j]=pixels[j].a;total+=alpha[j];if(previousAlpha!=null){float change=Math.Abs(alpha[j]-previousAlpha[j])/255f;diff+=change;sample.maxPixelChange=Mathf.Max(sample.maxPixelChange,change);}}
  sample.coverageMean=(float)(total/(pixels.Length*255));sample.meanPixelChange=(float)(diff/pixels.Length);previousAlpha=alpha;
  camera.Render();RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,512,512),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(dir,"frame-"+report.frames.ToString("D4")+".png"),frame.EncodeToPNG());
  report.samples.Add(sample);report.frames++;await Task.Delay(50);
 }
 report.seconds=clock.Elapsed.TotalSeconds;report.passed=report.seconds>=18&&report.frames>=100&&report.restarts==0&&report.minParticles>=minimum&&report.maxParticles<=maximum&&report.maxTrackedSpeed<.3f;
 File.WriteAllText(Path.Combine(dir,"samples.csv"),"seconds,simulationTime,particles,tracked,maxSpeed,coverageMean,meanPixelChange,maxPixelChange\n"+string.Join("\n",report.samples.Select(s=>FormattableString.Invariant($"{s.seconds},{s.simulationTime},{s.particles},{s.tracked},{s.maxSpeed},{s.coverageMean},{s.meanPixelChange},{s.maxPixelChange}"))));
 File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));if(!report.passed)throw new Exception("Continuity check failed: "+JsonUtility.ToJson(report));
 return new{report.seconds,report.frames,report.initialParticles,report.minParticles,report.maxParticles,report.restarts,report.maxTrackedSpeed,report.passed,directory=dir};
 }finally{RenderTexture.active=oldActive;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(mask);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
}
