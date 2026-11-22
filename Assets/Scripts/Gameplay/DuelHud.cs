using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using WinterDuel.UI;
namespace WinterDuel.Gameplay
{
    public sealed class DuelHud : MonoBehaviour
    {
        public DuelMatchController match;
        public HudActionSelection actions;
        public Slider localHealth,localWarmth,opponentHealth,opponentWarmth;
        public Text turnText,iceStateText,enemyPointer,aimText,winnerText;
        public GameObject menuPanel,victoryPanel,aimPanel;
        public RectTransform[] gestureBlockers;
        private readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        public bool BlocksGesture(Vector2 screen)
        {
            foreach(var rect in gestureBlockers)
                if(rect!=null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect,screen,null))return true;
            if(EventSystem.current==null)return false;
            uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},uiHits);
            return uiHits.Count>0;
        }
        public void ClosePanels(){menuPanel.SetActive(false);victoryPanel.SetActive(false);aimPanel.SetActive(false);}
        public void ShowVictory()
        {
            winnerText.text=match.State.Winner==0?"Победил\nбирюзовый игрок":"Победил\nкрасный игрок";
            menuPanel.SetActive(false);victoryPanel.SetActive(true);
        }
        public void RefreshUI()
        {
            if(match.State==null)return;var s=match.State;var own=s.Players[s.Active];var other=s.Players[1-s.Active];
            localHealth.maxValue=opponentHealth.maxValue=match.settings.maxHealth;
            localWarmth.maxValue=opponentWarmth.maxValue=match.settings.maxWarmth;
            localHealth.SetValueWithoutNotify(own.Health);localWarmth.SetValueWithoutNotify(own.Warmth);
            opponentHealth.SetValueWithoutNotify(other.Health);opponentWarmth.SetValueWithoutNotify(other.Warmth);
            actions.selectedAction=s.Selected==ProjectileKind.Ice?HudActionSelection.Action.IceSnowball:HudActionSelection.Action.Snowball;
            actions.Refresh();
            actions.snowballButton.interactable=actions.warmthButton.interactable=match.CanAct;
            actions.iceButton.interactable=match.CanAct && (own.IceReady || own.Warmth>=match.settings.iceCost);
            iceStateText.text=own.IceReady?"ГОТОВ":own.Warmth<match.settings.iceCost?"НЕТ ТЕПЛА":"−"+match.settings.iceCost+" Т";
            string who=s.Active==0?"бирюзового":"красного";
            turnText.text=s.Phase==MatchPhase.Finished?"Бой окончен":s.Phase==MatchPhase.Ready?(match.CanAim?"Ход "+who+" игрока":"Переход камеры…"):match.Status;
        }
        private void LateUpdate()
        {
            if(match.State==null)return;
            var target=match.characters[1-match.State.Active].transform.position+Vector3.up*1.5f;
            var p=match.cameraRig.gameCamera.WorldToViewportPoint(target);
            bool outside=p.x<0 || p.x>1 || p.y<0 || p.y>1;
            enemyPointer.gameObject.SetActive(outside && match.State.Phase!=MatchPhase.Finished && !match.Paused);
            if(!outside)return;
            var rt=enemyPointer.rectTransform;string arrow;
            Vector2 anchor;
            if(p.x<0){arrow="←";anchor=new Vector2(0,0.58f);rt.pivot=new Vector2(0,0.5f);}
            else if(p.x>1){arrow="→";anchor=new Vector2(1,0.58f);rt.pivot=new Vector2(1,0.5f);}
            else {arrow=p.y<0?"↓":"↑";anchor=new Vector2(0.5f,p.y<0?0:1);rt.pivot=new Vector2(0.5f,p.y<0?0:1);}
            rt.anchorMin=rt.anchorMax=anchor;rt.anchoredPosition=Vector2.zero;
            float distance=Vector2.Distance(match.characters[0].transform.position,match.characters[1].transform.position);
            enemyPointer.text=arrow+" "+Mathf.RoundToInt(distance)+" м";
        }
        public void ShowAim(Vector2 world,Vector2 velocity,float power)
        {
            aimPanel.SetActive(true);
            aimText.text=Mathf.RoundToInt(Mathf.Atan2(velocity.y,Mathf.Abs(velocity.x))*Mathf.Rad2Deg)+"° · "+Mathf.RoundToInt(power*100)+"%";
            Vector2 screen=match.cameraRig.gameCamera.WorldToScreenPoint(world);
            var rt=(RectTransform)aimPanel.transform;var parent=(RectTransform)rt.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,null,out var local);
            rt.localPosition=new Vector3(Mathf.Clamp(local.x,parent.rect.xMin+120,parent.rect.xMax-120),Mathf.Clamp(local.y,parent.rect.yMin+30,parent.rect.yMax-30),0);
        }
    }
}
