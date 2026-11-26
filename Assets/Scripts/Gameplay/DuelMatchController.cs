using UnityEngine;
using WinterDuel.App;
using WinterDuel.Networking;
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
        public bool ResultFollowsFlight {get;private set;}
        public bool Networked => GameSession.Instance!=null && GameSession.Instance.Networked && GameSession.Instance.InMatch;
        public bool IsReplica => Networked && GameSession.Instance.Mode==SessionMode.Client;
        public bool Mirrored => Networked && GameSession.Instance.LocalPlayer==1;
        public int ViewPlayer => Networked?GameSession.Instance.LocalPlayer:(State==null?0:State.Active);
        public bool SimulationPaused => Paused && !Networked;
        public bool CanAim => State!=null && State.Phase==MatchPhase.Ready && !Paused && cameraRig.Settled &&
            (!Networked || (GameSession.Instance.MatchReady && !GameSession.Instance.PendingAction && State.Active==GameSession.Instance.LocalPlayer));
        public bool CanAct => CanAim && (input==null || !input.IsDragging);
        private System.Random random;
        private float resolving,flightStart;
        private Vector2 replicaTarget;
        private void Start()
        {
            if(GameSession.Instance!=null)GameSession.Instance.AttachMatch(this);else NewBattle();
        }
        public void InitializeReplica()
        {
            Time.timeScale=1;Paused=false;ResultFollowsFlight=false;input.Cancel();ClearProjectile();State=new DuelState(settings);
            foreach(var c in characters)c.Place(c.initialX);
            Wind=0;Status="Согласование матча…";hud.ClosePanels();cameraRig.Snap();hud.RefreshUI();
        }
        private void ClearProjectile()
        {
            if(ActiveProjectile!=null){ActiveProjectile.gameObject.SetActive(false);Destroy(ActiveProjectile.gameObject);}ActiveProjectile=null;
        }
        public DuelSnapshot CaptureSnapshot()
        {
            return new DuelSnapshot{active=State.Active,turn=State.Turn,winner=State.Winner,phase=State.Phase,selected=State.Selected,flyingKind=State.FlyingKind,
                health=new[]{State.Players[0].Health,State.Players[1].Health},warmth=new[]{State.Players[0].Warmth,State.Players[1].Warmth},
                ice=new[]{State.Players[0].IceReady,State.Players[1].IceReady},x=new[]{characters[0].transform.position.x,characters[1].transform.position.x},wind=Wind,
                projectile=ActiveProjectile!=null,projectilePosition=ActiveProjectile!=null?(Vector2)ActiveProjectile.transform.position:Vector2.zero,
                projectileVelocity=ActiveProjectile!=null?ActiveProjectile.Velocity:Vector2.zero,resultPosition=ResultPosition,focusResult=ResultFollowsFlight,status=Status};
        }
        public void ApplySnapshot(DuelSnapshot snapshot)
        {
            if(!IsReplica)return;
            State.ApplySnapshot(snapshot);Wind=snapshot.wind;Status=snapshot.status;ResultPosition=snapshot.resultPosition;ResultFollowsFlight=snapshot.focusResult;
            for(int i=0;i<2;i++)if(Mathf.Abs(characters[i].transform.position.x-snapshot.x[i])>.0001f)characters[i].Place(snapshot.x[i]);
            ApplySnowWind();
            if(snapshot.projectile)
            {
                if(ActiveProjectile==null)
                {
                    ActiveProjectile=Instantiate(snapshot.flyingKind==ProjectileKind.Ice?icePrefab:snowPrefab,snapshot.projectilePosition,Quaternion.identity);
                    ActiveProjectile.enabled=false;ActiveProjectile.GetComponent<Collider2D>().enabled=false;ActiveProjectile.GetComponent<Rigidbody2D>().simulated=false;
                }
                replicaTarget=snapshot.projectilePosition;
            }
            else ClearProjectile();
            if(State.Phase==MatchPhase.Finished){input.Cancel();hud.ShowVictory();}
            hud.RefreshUI();
        }
        public bool ApplyAuthoritativeAction(int player,DuelAction action,Vector2 velocity)
        {
            if(IsReplica || State==null || State.Active!=player || State.Phase!=MatchPhase.Ready)return false;
            if(action==DuelAction.Throw)return LaunchAuthority(velocity);
            if(action==DuelAction.Snow)return State.SelectSnow();
            if(action==DuelAction.Ice){bool prepared=State.Players[player].IceReady;bool ok=State.PrepareOrSelectIce();if(ok&&!prepared)BeginResult("Ледяной снежок подготовлен",cameraRig.transform.position);return ok;}
            if(action==DuelAction.Warmth){bool ok=State.RecoverWarmth();if(ok)BeginResult("Тепло восстановлено",cameraRig.transform.position);return ok;}
            return false;
        }
        private bool Submit(DuelAction action,Vector2 velocity)
        {
            if(Networked)return GameSession.Instance.Submit(action,velocity);
            return ApplyAuthoritativeAction(State.Active,action,velocity);
        }
        public void RequestReplay(){if(GameSession.Instance!=null)GameSession.Instance.RequestReplay();else NewBattle();}
        public void ExitMatch(){if(GameSession.Instance!=null)GameSession.Instance.Leave("Вы вышли из матча");else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");}
        public void NewBattle()
        {
            if(IsReplica){InitializeReplica();return;}
            Time.timeScale=1;Paused=false;ResultFollowsFlight=false;
            if(input!=null)input.Cancel();
            if(ActiveProjectile!=null){ActiveProjectile.gameObject.SetActive(false);Destroy(ActiveProjectile.gameObject);}
            ActiveProjectile=null;State=new DuelState(settings);random=new System.Random(settings.windSeed);
            foreach(var c in characters)c.Place(c.initialX);
            RollWind();Status="";resolving=0;cameraRig.Snap();hud.ClosePanels();hud.RefreshUI();
        }
        private void Update()
        {
            if(State==null)return;
            if(IsReplica){if(ActiveProjectile!=null)ActiveProjectile.transform.position=Vector2.Lerp(ActiveProjectile.transform.position,replicaTarget,1-Mathf.Exp(-22*Time.deltaTime));hud.RefreshUI();return;}
            if(State.Phase==MatchPhase.InFlight && !SimulationPaused && (ActiveProjectile==null || Time.time-flightStart>settings.flightTimeout+1))
            {
                if(ActiveProjectile!=null){ActiveProjectile.gameObject.SetActive(false);Destroy(ActiveProjectile.gameObject);}
                ActiveProjectile=null;State.ResolveFlight(-1);BeginResult("Полёт завершён",cameraRig.transform.position,true);
            }
            if(State.Phase==MatchPhase.Resolving && !SimulationPaused)
            {
                resolving-=Time.deltaTime;
                if(resolving<=0 && State.AdvanceTurn()){Status="";RollWind();}
            }
            hud.RefreshUI();
        }
        private void RollWind()
        {
            Wind=((float)random.NextDouble()*2-1)*Mathf.Max(0,settings.maximumWind);
            ApplySnowWind();
        }
        private void ApplySnowWind()
        {
            if(snowfall!=null)
            {
                var v=snowfall.velocityOverLifetime;
                float speed=settings.maximumWind>0?Wind/settings.maximumWind*settings.snowWindSpeed:0;
                v.x=new ParticleSystem.MinMaxCurve(speed-0.045f,speed+0.045f);
            }
        }
        public void ChooseSnowball(){if(CanAct)Submit(DuelAction.Snow,Vector2.zero);}
        public void PrepareOrSelectIce(){if(CanAct)Submit(DuelAction.Ice,Vector2.zero);}
        public void RecoverWarmth(){if(CanAct)Submit(DuelAction.Warmth,Vector2.zero);}
        public bool TryLaunch(Vector2 velocity){return CanAim && Submit(DuelAction.Throw,velocity);}
        private bool LaunchAuthority(Vector2 velocity)
        {
            if(IsReplica || ActiveProjectile!=null || State.Phase!=MatchPhase.Ready || float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsInfinity(velocity.x) || float.IsInfinity(velocity.y))return false;
            if(velocity.magnitude<settings.minimumSpeed-.01f || velocity.magnitude>settings.maximumSpeed+.01f)return false;
            var prefab=State.Selected==ProjectileKind.Ice?icePrefab:snowPrefab;
            if(prefab==null || !State.BeginThrow())return false;
            ActiveProjectile=Instantiate(prefab,characters[State.Active].LaunchOrigin,Quaternion.identity);
            ActiveProjectile.Launch(this,State.Active,velocity,Wind);flightStart=Time.time;Status="Снежок в полёте";return true;
        }
        public void FlightFinished(DuelProjectile projectile,int target,Vector2 velocity)
        {
            if(IsReplica || projectile!=ActiveProjectile || State.Phase!=MatchPhase.InFlight)return;
            var impact=projectile.transform.position;ResultPosition=impact;ResultFollowsFlight=true;ActiveProjectile=null;
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
            else BeginResult(Status,impact,true);
            hud.RefreshUI();
        }
        private void BeginResult(string text,Vector3 point,bool followFlight=false){Status=text;ResultPosition=point;ResultFollowsFlight=followFlight;resolving=settings.resultDelay;}
        public void ToggleMenu()
        {
            if(State==null || State.Phase==MatchPhase.Finished)return;
            if(Paused){Resume();return;}
            if(input!=null)input.Cancel();Paused=true;if(!Networked)Time.timeScale=0;hud.menuPanel.SetActive(true);
        }
        public void Resume(){Paused=false;Time.timeScale=1;hud.menuPanel.SetActive(false);}
        private void OnDestroy(){Time.timeScale=1;if(settings!=null && (settings.hideFlags&HideFlags.DontSave)!=0)Destroy(settings);}
    }
}
