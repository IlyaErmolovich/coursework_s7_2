using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Diagnostics;using System.Threading.Tasks;
using UnityEngine;using UnityEngine.UI;using WinterDuel.UI;using WinterDuel.App;
public static class VerifySmokeContinuity {
 [Serializable] public class Sample{public double seconds;public float simulationTime,coverageMean,meanPixelChange,maxPixelChange;public int particles,tracked;public float maxSpeed;}
 [Serializable] public class Report{public double seconds;public int frames,initialParticles,minParticles=10000,maxParticles,restarts;public float maxTrackedSpeed;public bool passed;public List<Sample> samples=new List<Sample>();}
 // Uncompressed BMP keeps capture overhead low enough to assess motion, not PNG encoding stalls.
 static byte[] CaptureBmp(Texture2D texture){
  int width=texture.width,height=texture.height,stride=(width*3+3)&~3;var rgb=texture.GetRawTextureData<byte>();var bmp=new byte[54+stride*height];
  bmp[0]=66;bmp[1]=77;Buffer.BlockCopy(BitConverter.GetBytes(bmp.Length),0,bmp,2,4);bmp[10]=54;bmp[14]=40;
  Buffer.BlockCopy(BitConverter.GetBytes(width),0,bmp,18,4);Buffer.BlockCopy(BitConverter.GetBytes(height),0,bmp,22,4);bmp[26]=1;bmp[28]=24;
  for(int y=0;y<height;y++)for(int x=0;x<width;x++){int src=(y*width+x)*3,dst=54+y*stride+x*3;bmp[dst]=rgb[src+2];bmp[dst+1]=rgb[src+1];bmp[dst+2]=rgb[src];}return bmp;
 }
 public static async Task<object> Main(){
 if(!Application.isPlaying)throw new Exception("Run MainMenu in Play Mode");
 var original=Camera.main;var fit=original.GetComponent<MenuBackdropFit>();var ps=fit.chimneyEmitter.GetComponent<ParticleSystem>();var composite=original.GetComponent<MenuSmokeComposite>();
 if(!ps.main.prewarm||!ps.main.loop||ps.emission.burstCount!=0)throw new Exception("Expected prewarmed continuous emission");
 var dir=Path.GetFullPath(".utmp/smoke-motion");Directory.CreateDirectory(dir);
 var report=new Report{initialParticles=ps.particleCount};int minimum=Mathf.FloorToInt(ps.emission.rateOverTime.constant*ps.main.startLifetime.constantMin)-2;int maximum=Mathf.CeilToInt(ps.emission.rateOverTime.constant*ps.main.startLifetime.constantMax)+2;
 if(report.initialParticles<minimum)throw new Exception("Plume not formed on entry");
 var go=new GameObject("Smoke motion verification camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(original);camera.enabled=false;camera.aspect=13f/6;
 camera.transform.position=original.transform.position;
 var canvas=UnityEngine.Object.FindFirstObjectByType<MainMenuController>().GetComponent<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldPlane=canvas.planeDistance;var oldScale=canvas.scaleFactor;var oldScaler=scaler.enabled;scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.scaleFactor=.5f;
 var rt=new RenderTexture(1170,540,16,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
 var frame=new Texture2D(1170,540,TextureFormat.RGB24,false);var mask=new Texture2D(512,512,TextureFormat.RGBA32,false);var oldActive=RenderTexture.active;
 var particles=new ParticleSystem.Particle[ps.main.maxParticles];var previous=new Dictionary<uint,ParticleSystem.Particle>();byte[] previousAlpha=null;var clock=new Stopwatch();
 try{
 Canvas.ForceUpdateCanvases();foreach(var t in canvas.GetComponentsInChildren<TMPro.TMP_Text>())t.ForceMeshUpdate(true,true);
 // Allow Editor script compilation and the first Canvas rebuild to settle before timing.
 await Task.Delay(1000);clock.Start();
 while(clock.Elapsed.TotalSeconds<18){
  var sample=new Sample{seconds=clock.Elapsed.TotalSeconds,simulationTime=ps.time};
  if(!ps.isPlaying)throw new Exception("Particle system stopped");
  // The test never calls Play, Stop, Clear, Emit or Simulate.
  // A normal duration wrap is not a restart; retained live particles identify continuity.
  int count=ps.GetParticles(particles);sample.particles=count;report.minParticles=Math.Min(report.minParticles,count);report.maxParticles=Math.Max(report.maxParticles,count);
  var next=new Dictionary<uint,ParticleSystem.Particle>();
  for(int j=0;j<count;j++){var p=particles[j];next[p.randomSeed]=p;if(previous.TryGetValue(p.randomSeed,out var before)){float ageDelta=before.remainingLifetime-p.remainingLifetime;if(ageDelta>=0){sample.tracked++;if(ageDelta>0.001f)sample.maxSpeed=Mathf.Max(sample.maxSpeed,Vector3.Distance(before.position,p.position)/ageDelta);}}}
  if(previous.Count>0&&sample.tracked<count/2)report.restarts++;
  previous=next;report.maxTrackedSpeed=Mathf.Max(report.maxTrackedSpeed,sample.maxSpeed);
  composite.RefreshAndRender();RenderTexture.active=composite.coverage;mask.ReadPixels(new Rect(0,0,512,512),0,0);mask.Apply();var pixels=mask.GetPixels32();var alpha=new byte[pixels.Length];double total=0,diff=0;
  for(int j=0;j<pixels.Length;j++){alpha[j]=pixels[j].a;total+=alpha[j];if(previousAlpha!=null){float change=Math.Abs(alpha[j]-previousAlpha[j])/255f;diff+=change;sample.maxPixelChange=Mathf.Max(sample.maxPixelChange,change);}}
  sample.coverageMean=(float)(total/(pixels.Length*255));sample.meanPixelChange=(float)(diff/pixels.Length);previousAlpha=alpha;
  Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;frame.ReadPixels(new Rect(0,0,1170,540),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(dir,"frame-"+report.frames.ToString("D4")+".bmp"),CaptureBmp(frame));
  report.samples.Add(sample);report.frames++;await Task.Delay(50);
 }
 report.seconds=clock.Elapsed.TotalSeconds;report.passed=report.seconds>=18&&report.frames>=80&&report.restarts==0&&report.minParticles>=minimum&&report.maxParticles<=maximum&&report.maxTrackedSpeed<.4f;
 File.WriteAllText(Path.Combine(dir,"samples.csv"),"seconds,simulationTime,particles,tracked,maxSpeed,coverageMean,meanPixelChange,maxPixelChange\n"+string.Join("\n",report.samples.Select(s=>FormattableString.Invariant($"{s.seconds},{s.simulationTime},{s.particles},{s.tracked},{s.maxSpeed},{s.coverageMean},{s.meanPixelChange},{s.maxPixelChange}"))));
 File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));if(!report.passed)throw new Exception("Continuity check failed: "+JsonUtility.ToJson(report));
 return new{report.seconds,report.frames,report.initialParticles,report.minParticles,report.maxParticles,report.restarts,report.maxTrackedSpeed,report.passed,directory=dir};
 }finally{RenderTexture.active=oldActive;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;canvas.scaleFactor=oldScale;scaler.enabled=oldScaler;Canvas.ForceUpdateCanvases();camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(mask);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
}
