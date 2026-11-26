using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WinterDuel.Gameplay;
using WinterDuel.Environment;
using WinterDuel.Networking;
namespace WinterDuel.App
{
    public enum SessionMode { Local, Host, Client }
    public enum MenuPage { Main, Connection, Lobby }
    public sealed class DiscoveredDevice { public string address,name; public bool paired; }

    [DisallowMultipleComponent,RequireComponent(typeof(ClassicBluetoothTransport))]
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance {get;private set;}
        public ClassicBluetoothTransport transport;
        public string menuScene="MainMenu",gameScene="WinterField";
        [Min(5)] public float connectionTimeout=12;
        [Min(.03f)] public float snapshotInterval=.0667f;
        public SessionMode Mode {get;private set;}
        public MenuPage Page {get;private set;}
        public string Status {get;private set;}="Выберите режим игры";
        public bool Networked => Mode!=SessionMode.Local;
        public int LocalPlayer => Mode==SessionMode.Client?1:0;
        public bool Handshake {get;private set;}
        public bool LocalReady {get;private set;}
        public bool PeerReady {get;private set;}
        public bool MatchReady {get;private set;}
        public bool InMatch {get;private set;}
        public bool ReplayReady {get;private set;}
        public bool PeerReplayReady {get;private set;}
        public bool PendingAction => pendingAction>0;
        public int Round {get;private set;}
        public readonly List<DiscoveredDevice> Devices=new List<DiscoveredDevice>();
        public int DeviceRevision {get;private set;}
        public bool CanStart => Mode==SessionMode.Host&&Handshake&&LocalReady&&PeerReady&&!InMatch;
        private DuelMatchController match;
        private string sessionId;
        private HostCommandGate gate=new HostCommandGate();
        private long outgoingSequence,pendingAction;
        private float receivedAt,pingAt,snapshotAt;
        private bool peerLoaded,leaving,connectingDevice,connectionHasError;
        private void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            if(transport==null)transport=GetComponent<ClassicBluetoothTransport>();
            transport.Received+=OnNative;
        }
        private void OnDestroy(){if(Instance==this){transport.Received-=OnNative;Instance=null;}}
        public void CreateGame()=>BeginConnection(SessionMode.Host);
        public void JoinGame()=>BeginConnection(SessionMode.Client);
        private void BeginConnection(SessionMode mode)
        {
            ResetConnection();Mode=mode;Page=MenuPage.Connection;
            Status=mode==SessionMode.Host?"Проверка Bluetooth… Разрешите видимость телефона.":"Проверка разрешений и поиск устройств…";
            transport.Begin(mode==SessionMode.Host);
        }
        public void Retry(){if(Mode==SessionMode.Host)CreateGame();else JoinGame();}
        public void SelectDevice(string address)
        {
            if(Mode!=SessionMode.Client||Handshake)return;
            connectingDevice=true;connectionHasError=false;Status="Подключение… Подтвердите сопряжение на обоих телефонах.";transport.Connect(address);
        }
        public void Discoverable(){if(Mode==SessionMode.Host&&!Handshake)transport.MakeDiscoverable();}
        public void Cancel()=>Leave("Выберите режим игры");
        public void StartLocal()
        {
            ResetConnection();Mode=SessionMode.Local;InMatch=true;MatchReady=true;
            SceneManager.LoadScene(gameScene);
        }
        public void Quit(){ResetConnection();Application.Quit();}
        private void ResetConnection()
        {
            transport.Cancel();Handshake=false;LocalReady=false;PeerReady=false;MatchReady=false;InMatch=false;peerLoaded=false;
            ReplayReady=PeerReplayReady=false;pendingAction=0;Round=0;outgoingSequence=0;sessionId=null;gate=new HostCommandGate();
            Devices.Clear();DeviceRevision++;match=null;connectingDevice=connectionHasError=false;Time.timeScale=1;
        }
        public void Leave(string reason)
        {
            if(leaving)return;leaving=true;
            try{
                if(Networked&&Handshake)Send(new DuelMessage{type="leave"});
                ResetConnection();Mode=SessionMode.Local;Page=MenuPage.Main;Status=reason;
                if(SceneManager.GetActiveScene().name!=menuScene)SceneManager.LoadScene(menuScene);
            }finally{leaving=false;}
        }
        private void OnApplicationPause(bool paused)
        {
            if(paused&&Networked&&Handshake)Leave("Приложение ушло в фон. Соединение закрыто; подключитесь снова.");
        }
        private void OnApplicationQuit(){if(transport!=null)transport.Cancel();}
        private void OnNative(BluetoothEvent e)
        {
            // A retained adapter receiver must never interrupt an offline/local match.
            if(!Networked)return;
            switch(e.type)
            {
                case "device":
                    if(Mode!=SessionMode.Client||Handshake)return;
                    var d=Devices.Find(x=>x.address==e.address);
                    if(d==null){d=new DiscoveredDevice{address=e.address};Devices.Add(d);}
                    d.name=e.text;d.paired=e.paired;DeviceRevision++;break;
                case "connected":
                    connectingDevice=false;receivedAt=pingAt=Time.unscaledTime;Status="Проверка версии игры…";
                    if(Mode==SessionMode.Host){sessionId=Guid.NewGuid().ToString("N");Send(new DuelMessage{type="hello"});}
                    break;
                case "message":Receive(e.text);break;
                case "disconnected":Leave(e.text);break;
                case "error":connectionHasError=true;if(InMatch||Handshake)Leave(e.text);else Status=e.text;break;
                case "scanDone":if(Mode==SessionMode.Client&&!Handshake&&!transport.Connected&&!connectingDevice&&!connectionHasError)Status=e.text;break;
                case "status":if(!Handshake&&!connectionHasError)Status=e.text;break;
            }
        }
        private long Send(DuelMessage message)
        {
            message.session=sessionId;message.sequence=++outgoingSequence;message.round=Round;
            transport.Send(JsonUtility.ToJson(message));return message.sequence;
        }
        private void Receive(string json)
        {
            try
            {
                if(string.IsNullOrEmpty(json)||json.Length>65536){Leave("Неверное сетевое сообщение.");return;}
                var message=JsonUtility.FromJson<DuelMessage>(json);
                if(message.protocol!=1){Leave("Версии игры не совпадают. Установите одинаковый APK.");return;}
                if(message.type=="hello"&&Mode==SessionMode.Client&&!Handshake)
                {
                    if(string.IsNullOrEmpty(message.session))return;
                    sessionId=message.session;gate=new HostCommandGate();
                    if(!gate.AcceptSequence(message.sequence))return;
                    receivedAt=Time.unscaledTime;Handshake=true;Page=MenuPage.Lobby;Status="Соединение установлено. Подтвердите готовность.";Send(new DuelMessage{type="helloAck"});return;
                }
                if(message.session!=sessionId||string.IsNullOrEmpty(sessionId)||!gate.AcceptSequence(message.sequence))return;
                receivedAt=Time.unscaledTime;
                if(message.type=="helloAck"&&Mode==SessionMode.Host&&!Handshake){Handshake=true;Page=MenuPage.Lobby;Status="Оба телефона подключены. Подтвердите готовность.";SendLobby();return;}
                if(!Handshake)return;
                switch(message.type)
                {
                    case "ping":break;
                    case "leave":Leave("Другой игрок вышел. Можно создать новый матч.");break;
                    case "ready":if(Mode==SessionMode.Host&&!InMatch){PeerReady=message.ready;SendLobby();}break;
                    case "lobby":if(Mode==SessionMode.Client&&!InMatch){PeerReady=message.ready;LocalReady=message.peerReady;}break;
                    case "start":
                        if(Mode!=SessionMode.Client||message.round!=Round+1)return;
                        Round=message.round;InMatch=true;MatchReady=false;ReplayReady=PeerReplayReady=false;pendingAction=0;
                        if(match==null)SceneManager.LoadScene(gameScene);else{match.InitializeReplica();Send(new DuelMessage{type="loaded"});}
                        break;
                    case "loaded":if(Mode==SessionMode.Host&&message.round==Round&&InMatch){peerLoaded=true;SendSetup();}break;
                    case "setup":
                        if(Mode!=SessionMode.Client||message.round!=Round||match==null||message.field==null||message.state==null)return;
                        JsonUtility.FromJsonOverwrite(message.balance,match.settings);
                        message.field.Apply(match.GetComponent<FixedDuelFieldLayout>());
                        match.InitializeReplica();match.ApplySnapshot(message.state);match.cameraRig.Snap();
                        Send(new DuelMessage{type="setupAck"});break;
                    case "setupAck":if(Mode==SessionMode.Host&&message.round==Round&&InMatch){MatchReady=true;PublishSnapshot();}break;
                    case "snapshot":
                        if(Mode!=SessionMode.Client||message.round!=Round||match==null||message.state==null)return;
                        MatchReady=message.matchReady;match.ApplySnapshot(message.state);
                        if(message.acknowledged>=pendingAction)pendingAction=0;
                        break;
                    case "action":
                        if(Mode!=SessionMode.Host||!MatchReady||match==null)return;
                        if(gate.Allows(message,Round,match.State,match.settings))match.ApplyAuthoritativeAction(1,message.action,message.velocity);
                        PublishSnapshot();break;
                    case "replay":
                        if(Mode==SessionMode.Host&&message.round==Round&&match!=null&&match.State.Phase==MatchPhase.Finished){PeerReplayReady=message.ready;ReplayChanged();}break;
                    case "replayState":
                        if(Mode==SessionMode.Client&&message.round==Round){PeerReplayReady=message.ready;ReplayReady=message.peerReady;match?.hud.ShowVictory();}break;
                }
            }
            catch(Exception e){Debug.LogWarning("Bluetooth protocol: "+e.GetType().Name);Leave("Не удалось согласовать состояние игры. Установите одинаковый APK и подключитесь снова.");}
        }
        public void ToggleReady()
        {
            if(!Handshake||InMatch)return;LocalReady=!LocalReady;
            if(Mode==SessionMode.Host)SendLobby();else Send(new DuelMessage{type="ready",ready=LocalReady});
        }
        private void SendLobby()=>Send(new DuelMessage{type="lobby",ready=LocalReady,peerReady=PeerReady});
        public void StartMatch(){if(CanStart)BeginRound();}
        private void BeginRound()
        {
            Round++;InMatch=true;MatchReady=false;peerLoaded=false;ReplayReady=PeerReplayReady=false;pendingAction=0;
            Send(new DuelMessage{type="start"});
            if(match==null)SceneManager.LoadScene(gameScene);else match.NewBattle();
        }
        public void AttachMatch(DuelMatchController controller)
        {
            match=controller;
            // Network balance is a runtime copy; the host cannot overwrite project assets.
            if(Networked){match.settings=Instantiate(match.settings);match.settings.hideFlags=HideFlags.DontSave;}
            if(Mode==SessionMode.Client){match.InitializeReplica();Send(new DuelMessage{type="loaded"});}
            else{match.NewBattle();if(Networked&&peerLoaded)SendSetup();}
        }
        private void SendSetup()
        {
            if(match==null||!peerLoaded)return;
            Send(new DuelMessage{type="setup",balance=JsonUtility.ToJson(match.settings),field=FieldSetup.Capture(match.GetComponent<FixedDuelFieldLayout>()),state=match.CaptureSnapshot()});
        }
        public bool Submit(DuelAction action,Vector2 velocity)
        {
            if(!Networked||!MatchReady||match==null||PendingAction||match.State.Active!=LocalPlayer||match.State.Phase!=MatchPhase.Ready)return false;
            if(Mode==SessionMode.Host){bool accepted=match.ApplyAuthoritativeAction(0,action,velocity);PublishSnapshot();return accepted;}
            pendingAction=Send(new DuelMessage{type="action",turn=match.State.Turn,action=action,velocity=velocity});return true;
        }
        public void PublishSnapshot()
        {
            if(Mode!=SessionMode.Host||match==null||!Handshake||!MatchReady)return;
            Send(new DuelMessage{type="snapshot",matchReady=MatchReady,state=match.CaptureSnapshot(),acknowledged=gate.LastSequence});snapshotAt=Time.unscaledTime;
        }
        public void RequestReplay()
        {
            if(!Networked){match?.NewBattle();return;}
            if(match==null||match.State.Phase!=MatchPhase.Finished)return;
            ReplayReady=true;if(Mode==SessionMode.Host)ReplayChanged();else Send(new DuelMessage{type="replay",ready=true});
            match.hud.ShowVictory();
        }
        private void ReplayChanged()
        {
            Send(new DuelMessage{type="replayState",ready=ReplayReady,peerReady=PeerReplayReady});
            if(ReplayReady&&PeerReplayReady)BeginRound();else match.hud.ShowVictory();
        }
        private void Update()
        {
            if(!Networked||!transport.Connected)return;
            float now=Time.unscaledTime;
            if(now-receivedAt>connectionTimeout){Leave("Другой телефон не отвечает. Подключитесь заново.");return;}
            if(now-pingAt>1&&Handshake){Send(new DuelMessage{type="ping"});pingAt=now;}
            if(Mode==SessionMode.Host&&MatchReady&&match!=null&&now-snapshotAt>=snapshotInterval)PublishSnapshot();
        }
    }
}
