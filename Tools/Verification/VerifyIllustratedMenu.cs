using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using WinterDuel.App;
using WinterDuel.UI;
using TMPro;
public static class VerifyIllustratedMenu
{
 static int checks;
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);checks++;}
 public static async Task<object> Main(){
  checks=0;Check(Application.isPlaying,"Run MainMenu in Play Mode");
  var ui=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();var session=GameSession.Instance;
  Check(ui!=null&&session!=null&&ui.mainPanel.activeInHierarchy,"Main menu opens");
  var buttons=ui.mainPanel.GetComponentsInChildren<Button>(true);Check(buttons.Length==3,"Exactly three main buttons");
  string[] names={"Create Game","Join Game","Quit"},methods={"CreateGame","JoinGame","Quit"},labels={"Создать игру","Подключиться","Выход"};
  Check(ui.mainPanel.transform.Find("Local Game")==null,"Local mode removed from product menu");
  foreach(int i in Enumerable.Range(0,3)){
   var b=buttons.Single(x=>x.name==names[i]);Check(b.onClick.GetPersistentEventCount()==1&&b.onClick.GetPersistentTarget(0)==ui&&b.onClick.GetPersistentMethodName(0)==methods[i],"Saved callback "+names[i]);
   var t=b.GetComponentInChildren<TMP_Text>();Check(t.text==labels[i]&&t.font.faceInfo.familyName=="Balsamiq Sans"&&t.font.faceInfo.styleName=="Bold"&&t.fontSize==58,"Shared real Bold face and text");
   Check(Mathf.Abs(t.fontSharedMaterial.GetFloat("_FaceDilate")-.075f)<.0001f&&!t.fontSharedMaterial.IsKeywordEnabled("UNDERLAY_ON")&&t.fontSharedMaterial.GetFloat("_OutlineWidth")==0,"Slight material stroke without shadow");
   Check(t.text.All(c=>t.font.HasCharacter(c)),"Cyrillic glyphs present");Check(t.preferredWidth<t.rectTransform.rect.width-20,"Label fits without auto-shrinking");
   var hits=new List<RaycastResult>();var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,t.transform.position)};EventSystem.current.RaycastAll(pointer,hits);
   Check(hits.Any(h=>h.gameObject.GetComponentInParent<Button>()==b),"Touch target reachable");
  }
  var create=buttons.Single(x=>x.name=="Create Game");var positions=buttons.Select(b=>b.transform.position).ToArray();
  var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
  ExecuteEvents.Execute(create.gameObject,data,ExecuteEvents.pointerEnterHandler);ExecuteEvents.Execute(create.gameObject,data,ExecuteEvents.pointerDownHandler);await Task.Delay(180);
  float pressed=create.image.canvasRenderer.GetColor().r;Check(pressed<.95f,"Press visibly darkens artwork");
  Check(buttons.Select((b,i)=>Vector3.Distance(b.transform.position,positions[i])).All(d=>d<.001f),"Press does not move siblings");
  ExecuteEvents.Execute(create.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(create.gameObject,data,ExecuteEvents.pointerExitHandler);EventSystem.current.SetSelectedGameObject(null);await Task.Delay(150);
  Check(create.image.canvasRenderer.GetColor().r>.98f,"Release restores brightness");
  var backdrop=Camera.main.GetComponent<MenuBackdropFit>();var ps=backdrop.chimneyEmitter.GetComponent<ParticleSystem>();
  var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);var before=particles.Take(count).ToDictionary(p=>p.randomSeed,p=>p.position);await Task.Delay(400);count=ps.GetParticles(particles);
  int moved=particles.Take(count).Count(p=>before.ContainsKey(p.randomSeed)&&Vector3.Distance(p.position,before[p.randomSeed])>.01f);
  Check(moved>0&&count>0&&count<32,"Smoke moves without dense particle count");
  Check(ps.main.simulationSpace==ParticleSystemSimulationSpace.Local&&ps.main.scalingMode==ParticleSystemScalingMode.Hierarchy,"Smoke follows source illustration");
  var bounds=backdrop.artwork.sprite.bounds;var local=backdrop.chimneyEmitter.localPosition;var uv=new Vector2((local.x-bounds.min.x)/bounds.size.x,(local.y-bounds.min.y)/bounds.size.y);
  Check(Vector2.Distance(uv,new Vector2(1492f/1844,1-214f/853))<.00001f,"Emitter is at the chimney mouth");
  foreach(string name in new[]{"Create Game","Join Game","Create Game"}){
   buttons.Single(x=>x.name==name).onClick.Invoke();await Task.Delay(120);
   Check(session.Page==MenuPage.Connection&&ui.connectionPanel.activeInHierarchy&&!ui.mainPanel.activeInHierarchy,"Real connection screen opens");
   Check(session.Mode==(name=="Create Game"?SessionMode.Host:SessionMode.Client),"Correct connection role");
   ui.connectionPanel.transform.Find("Cancel").GetComponent<Button>().onClick.Invoke();await Task.Delay(120);
   Check(session.Page==MenuPage.Main&&ui.mainPanel.activeInHierarchy&&!session.Networked,"Back cancels and restores main menu");
  }
  Check(UnityEngine.Object.FindObjectsByType<GameSession>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"No duplicated session or EventSystem");
  Check(!ui.notice.gameObject.activeSelf,"Default helper text absent from artwork");
  return new{checks,passed=true,pressedBrightness=pressed,movingSmokeParticles=moved,smokeParticles=count,androidRadioTested=false,androidQuitTested=false};
 }
}
