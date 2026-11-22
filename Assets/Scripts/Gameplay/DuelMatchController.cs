using UnityEngine;
namespace WinterDuel.Gameplay
{
    public sealed class DuelMatchController : MonoBehaviour
    {
        public DuelSettings settings;
        public DuelCharacter[] characters;
        public DuelProjectile snowPrefab,icePrefab;
        public DuelCameraRig cameraRig;
        public SlingshotInput input;
        public DuelHud hud;
        public ParticleSystem snowfall;
        public DuelState State {get;private set;}
        public DuelProjectile ActiveProjectile {get;private set;}
        public float Wind {get;private set;}
        public bool Paused {get;private set;}
        public string Status {get;private set;}
        public Vector3 ResultPosition {get;private set;}
        public bool CanAim => State!=null && State.Phase==MatchPhase.Ready && !Paused && cameraRig.Settled;
        public bool CanAct => CanAim && (input==null || !input.IsDragging);
        private System.Random random;
        private float resolving,flightStart;
        private void Start() => NewBattle();
        public void NewBattle()
        {
            Time.timeScale=1;Paused=false;
            if(input!=null)input.Cancel();
            if(ActiveProjectile!=null){ActiveProjectile.gameObject.SetActive(false);Destroy(ActiveProjectile.gameObject);}
            ActiveProjectile=null;State=new DuelState(settings);random=new System.Random(settings.windSeed);
            foreach(var c in characters)c.Place(c.initialX);
            RollWind();Status="";resolving=0;cameraRig.Snap();hud.ClosePanels();hud.RefreshUI();
        }
        private void Update()
        {
            if(State==null)return;
            if(State.Phase==MatchPhase.InFlight && !Paused && (ActiveProjectile==null || Time.time-flightStart>settings.flightTimeout+1))
            {
                if(ActiveProjectile!=null){ActiveProjectile.gameObject.SetActive(false);Destroy(ActiveProjectile.gameObject);}
                ActiveProjectile=null;State.ResolveFlight(-1);BeginResult("Полёт завершён",cameraRig.transform.position);
            }
            if(State.Phase==MatchPhase.Resolving && !Paused)
            {
                resolving-=Time.deltaTime;
                if(resolving<=0 && State.AdvanceTurn()){Status="";RollWind();}
            }
            hud.RefreshUI();
        }
        private void RollWind()
        {
            Wind=((float)random.NextDouble()*2-1)*Mathf.Max(0,settings.maximumWind);
            if(snowfall!=null)
            {
                var v=snowfall.velocityOverLifetime;
                float speed=settings.maximumWind>0?Wind/settings.maximumWind*settings.snowWindSpeed:0;
                v.x=new ParticleSystem.MinMaxCurve(speed-0.045f,speed+0.045f);
            }
        }
        public void ChooseSnowball(){if(CanAct)State.SelectSnow();}
        public void PrepareOrSelectIce()
        {
            if(!CanAct)return;bool hadIce=State.Players[State.Active].IceReady;
            if(State.PrepareOrSelectIce() && !hadIce)BeginResult("Ледяной снежок подготовлен",cameraRig.transform.position);
        }
        public void RecoverWarmth()
        {
            if(CanAct && State.RecoverWarmth())BeginResult("Тепло восстановлено",cameraRig.transform.position);
        }
        public bool TryLaunch(Vector2 velocity)
        {
            if(!CanAim || ActiveProjectile!=null || float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsInfinity(velocity.x) || float.IsInfinity(velocity.y))return false;
            if(velocity.magnitude<0.1f || velocity.magnitude>settings.maximumSpeed+0.01f)return false;
            var prefab=State.Selected==ProjectileKind.Ice?icePrefab:snowPrefab;
            if(prefab==null || !State.BeginThrow())return false;
            ActiveProjectile=Instantiate(prefab,characters[State.Active].LaunchOrigin,Quaternion.identity);
            ActiveProjectile.Launch(this,State.Active,velocity,Wind);flightStart=Time.time;Status="Снежок в полёте";
            return true;
        }
        public void FlightFinished(DuelProjectile projectile,int target,Vector2 velocity)
        {
            if(projectile!=ActiveProjectile || State.Phase!=MatchPhase.InFlight)return;
            var impact=projectile.transform.position;ActiveProjectile=null;
            if(!State.ResolveFlight(target))return;
            if(target>=0 && target!=State.Active)
            {
                var actor=characters[target];float direction=Mathf.Sign(velocity.x);
                if(direction==0)direction=Mathf.Sign(actor.transform.position.x-characters[State.Active].transform.position.x);
                float x=Mathf.Clamp(actor.transform.position.x+direction*settings.knockback,settings.playerLeftLimit,settings.playerRightLimit);
                float other=characters[1-target].transform.position.x;
                x=actor.transform.position.x>other?Mathf.Max(x,other+settings.minimumPlayerDistance):Mathf.Min(x,other-settings.minimumPlayerDistance);
                actor.Place(x);Status="Попадание";
            }
            else Status="Промах";
            if(State.Phase==MatchPhase.Finished){if(input!=null)input.Cancel();hud.ShowVictory();}
            else BeginResult(Status,impact);
            hud.RefreshUI();
        }
        private void BeginResult(string text,Vector3 point){Status=text;ResultPosition=point;resolving=settings.resultDelay;}
        public void ToggleMenu()
        {
            if(State==null || State.Phase==MatchPhase.Finished)return;
            if(Paused){Resume();return;}
            if(input!=null)input.Cancel();Paused=true;Time.timeScale=0;hud.menuPanel.SetActive(true);
        }
        public void Resume(){Paused=false;Time.timeScale=1;hud.menuPanel.SetActive(false);}
        private void OnDestroy(){Time.timeScale=1;}
    }
}
