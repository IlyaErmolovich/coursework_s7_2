using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using WinterDuel.App;
using WinterDuel.UI;
public static class VerifyMenuAspects
{
 static void SetAspect(int width,int height){
  var asm=typeof(Editor).Assembly;var t=asm.GetType("UnityEditor.GameViewSizes");var single=typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance").GetValue(null);var kind=Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),EditorUserBuildSettings.activeBuildTarget==BuildTarget.Android?"Android":"Standalone");var group=t.GetMethod("GetGroup").Invoke(single,new[]{kind});var gt=group.GetType();int n=(int)gt.GetMethod("GetTotalCount").Invoke(group,null),index=-1;
  for(int i=0;i<n;i++){var s=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});if((int)s.GetType().GetProperty("width").GetValue(s)==width&&(int)s.GetType().GetProperty("height").GetValue(s)==height)index=i;}
  if(index<0){var type=asm.GetType("UnityEditor.GameViewSize");var s=Activator.CreateInstance(type,new object[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"),0),width,height,"Menu check "+width+":"+height});gt.GetMethod("AddCustomSize").Invoke(group,new[]{s});index=n;}
  var viewType=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,index);view.Show();view.Repaint();
 }
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static Vector3 ImagePoint(MenuBackdropFit fit,float x,float y){var b=fit.artwork.sprite.bounds;return fit.artwork.transform.TransformPoint(new Vector3(b.min.x+x/1844*b.size.x,b.max.y-y/853*b.size.y,0));}
 public static async Task<object> Main(){
  Check(Application.isPlaying,"Run MainMenu in Play Mode");var output=new List<object>();
  try{
   foreach(var aspect in new[]{new Vector2Int(13,6),new Vector2Int(16,9),new Vector2Int(20,9)}){
    SetAspect(aspect.x,aspect.y);await Task.Delay(350);Canvas.ForceUpdateCanvases();
    var camera=Camera.main;var fit=camera.GetComponent<MenuBackdropFit>();fit.Refresh();var ui=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();var column=ui.GetComponent<MenuColumnFit>();column.Refresh();
    Check(Mathf.Abs(camera.aspect-(float)aspect.x/aspect.y)<.006f,"Game View ratio did not update");
    Check(Mathf.Abs(fit.artwork.transform.localScale.x-fit.artwork.transform.localScale.y)<.00001f,"Background distorted");
    var b=fit.artwork.bounds;var low=camera.WorldToViewportPoint(b.min);var high=camera.WorldToViewportPoint(b.max);Check(low.x<=.001f&&low.y<=.001f&&high.x>=.999f&&high.y>=.999f,"Background does not cover viewport");
    foreach(var button in ui.mainPanel.GetComponentsInChildren<Button>()){
     var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);foreach(var c in corners){var p=RectTransformUtility.WorldToScreenPoint(null,c);Check(p.x>8&&p.x<Screen.width-8&&p.y>8&&p.y<Screen.height-8,"Button outside safe frame");}
    }
    var logo=ui.mainPanel.transform.Find("Menu Column/Logo - Snowball Duel").GetComponent<RectTransform>();
    foreach(var source in new[]{new Vector2(17,66),new Vector2(1640,921)}){
     var rect=logo.rect;var p=RectTransformUtility.WorldToScreenPoint(null,logo.TransformPoint(new Vector3(rect.xMin+source.x/1672*rect.width,rect.yMax-source.y/941*rect.height,0)));
     Check(p.x>8&&p.x<Screen.width-8&&p.y>8&&p.y<Screen.height-8,"Visible logo cropped");
    }
    // Source-image landmarks covering both characters, the house and chimney.
    foreach(var point in new[]{new Vector2(740,303),new Vector2(1080,725),new Vector2(1370,323),new Vector2(1670,727),new Vector2(1492,214)}){
     var p=camera.WorldToViewportPoint(ImagePoint(fit,point.x,point.y));Check(p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1,"Important illustration landmark cropped");
    }
    var pipe=RectTransformUtility.WorldToScreenPoint(camera,ImagePoint(fit,1492,214));var smoke=RectTransformUtility.WorldToScreenPoint(camera,fit.chimneyEmitter.position);Check(Vector2.Distance(pipe,smoke)<.01f,"Smoke detached from chimney");
    var composite=camera.GetComponent<MenuSmokeComposite>();composite.RefreshAndRender();
    Check(!composite.coverageCamera.enabled&&composite.coverageCamera.cullingMask==(1<<fit.chimneyEmitter.gameObject.layer)&&(camera.cullingMask&(1<<fit.chimneyEmitter.gameObject.layer))==0,"Particle mask isolated from main camera");
    foreach(var corner in new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,.5f,0)}){
     var projected=composite.coverageCamera.WorldToViewportPoint(composite.display.transform.TransformPoint(corner));
     Check(Vector2.Distance(new Vector2(projected.x,projected.y),new Vector2(corner.x+.5f,corner.y+.5f))<.001f,"Unified smoke projection detached from illustration");
    }
    output.Add(new{aspect=aspect.x+":"+aspect.y,screen=Screen.width+"x"+Screen.height,cameraAspect=camera.aspect,columnScale=column.column.localScale.x,smokePixelError=Vector2.Distance(pipe,smoke),passed=true});
   }
   return output;
  }finally{SetAspect(13,6);}
 }
}
