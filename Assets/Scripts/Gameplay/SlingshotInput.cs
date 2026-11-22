using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
namespace WinterDuel.Gameplay
{
    public sealed class SlingshotInput : MonoBehaviour
    {
        public DuelMatchController match;
        public SpriteRenderer[] trajectoryDots;
        public Vector2 readoutOffset=new Vector2(1.8f,0.85f);
        public bool IsDragging {get;private set;}
        public Vector2 PreviewVelocity {get;private set;}
        private Vector2 start,current;
        private bool touchSequence;
        private int touchId=-1;
        private readonly RaycastHit2D[] previewHits=new RaycastHit2D[16];
        private void Update()
        {
            var screen=Touchscreen.current;bool any=false;
            if(screen!=null)
            {
                TouchControl tracked=null,first=null;
                foreach(var t in screen.touches)
                {
                    var phase=t.phase.ReadValue();
                    bool active=phase==UnityEngine.InputSystem.TouchPhase.Began || phase==UnityEngine.InputSystem.TouchPhase.Moved || phase==UnityEngine.InputSystem.TouchPhase.Stationary;
                    if(active)any=true;
                    if(first==null && phase==UnityEngine.InputSystem.TouchPhase.Began)first=t;
                    if(touchSequence && t.touchId.ReadValue()==touchId)tracked=t;
                }
                if(!touchSequence && any)
                {
                    touchSequence=true;
                    if(first!=null){touchId=first.touchId.ReadValue();Begin(first.position.ReadValue());tracked=first;}
                }
                if(touchSequence)
                {
                    if(tracked!=null)
                    {
                        var phase=tracked.phase.ReadValue();current=tracked.position.ReadValue();
                        if(phase==UnityEngine.InputSystem.TouchPhase.Ended || phase==UnityEngine.InputSystem.TouchPhase.Canceled){End(phase==UnityEngine.InputSystem.TouchPhase.Canceled);touchId=-1;}
                        else if(IsDragging)DrawPreview();
                    }
                    if(!any){if(IsDragging)Cancel();touchSequence=false;touchId=-1;}
                    return;
                }
            }
            var mouse=Mouse.current;if(mouse==null)return;
            if(mouse.leftButton.wasPressedThisFrame)Begin(mouse.position.ReadValue());
            if(IsDragging){current=mouse.position.ReadValue();if(mouse.leftButton.wasReleasedThisFrame)End(false);else DrawPreview();}
        }
        private Vector2 World(Vector2 p)=>match.cameraRig.gameCamera.ScreenToWorldPoint(new Vector3(p.x,p.y,10));
        private void Begin(Vector2 p)
        {
            if(IsDragging || !match.CanAim || match.hud.BlocksGesture(p))return;
            foreach(var character in match.characters)if(character.Contains(World(p)))return;
            start=current=p;IsDragging=true;
        }
        private void End(bool cancelled)
        {
            if(!IsDragging)return;
            var own=match.characters[match.State.Active];
            var drag=(start-current)*(1080f/Mathf.Max(1,Screen.height));
            bool valid=!cancelled && drag.magnitude>=match.settings.minimumDrag && !own.Contains(World(current)) && !match.hud.BlocksGesture(current);
            var velocity=Velocity(drag,out _);Cancel();
            if(valid)match.TryLaunch(velocity);
        }
        private Vector2 Velocity(Vector2 drag,out float power)
        {
            power=Mathf.Clamp01(drag.magnitude/Mathf.Max(1,match.settings.maximumDrag));
            return drag.normalized*Mathf.Lerp(match.settings.minimumSpeed,match.settings.maximumSpeed,power);
        }
        private void DrawPreview()
        {
            if(!match.CanAim){Cancel();return;}
            var drag=(start-current)*(1080f/Mathf.Max(1,Screen.height));
            if(drag.magnitude<match.settings.minimumDrag){Hide();return;}
            PreviewVelocity=Velocity(drag,out float power);
            Vector2 origin=match.characters[match.State.Active].LaunchOrigin,p=origin,v=PreviewVelocity;
            int shown=0;float elapsed=0;int steps=0;
            while(elapsed<match.settings.previewDuration && shown<trajectoryDots.Length)
            {
                var previous=p;
                ShotMotion.Step(ref p,ref v,Time.fixedDeltaTime,match.settings.gravity,match.Wind);
                var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.AllLayers);filter.useTriggers=false;
                int hitCount=Physics2D.CircleCast(previous,match.snowPrefab.radius,(p-previous).normalized,filter,previewHits,(p-previous).magnitude);
                bool blocked=false;
                for(int h=0;h<hitCount;h++){var c=previewHits[h].collider.GetComponentInParent<DuelCharacter>();if(c==null || c.playerIndex!=match.State.Active){blocked=true;break;}}
                if(blocked)break;
                elapsed+=Time.fixedDeltaTime;steps++;
                if(Vector2.Distance(p,origin)>match.settings.previewDistance)break;
                if(steps%3!=0)continue;
                trajectoryDots[shown].gameObject.SetActive(true);trajectoryDots[shown].transform.position=new Vector3(p.x,p.y,-1);shown++;
            }
            for(int i=shown;i<trajectoryDots.Length;i++)trajectoryDots[i].gameObject.SetActive(false);
            float direction=Mathf.Sign(PreviewVelocity.x);
            match.hud.ShowAim(origin+new Vector2(direction*readoutOffset.x,readoutOffset.y),PreviewVelocity,power);
        }
        private void Hide(){if(trajectoryDots!=null)foreach(var dot in trajectoryDots)if(dot!=null)dot.gameObject.SetActive(false);if(match!=null && match.hud!=null)match.hud.aimPanel.SetActive(false);}
        public void Cancel(){IsDragging=false;PreviewVelocity=Vector2.zero;Hide();}
        private void OnDisable()=>Cancel();
    }
}
