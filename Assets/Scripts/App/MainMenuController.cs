using UnityEngine;
using UnityEngine.UI;
namespace WinterDuel.App
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public GameObject mainPanel,connectionPanel,lobbyPanel;
        public Text notice,connectionTitle,connectionStatus,lobbyStatus,readyLabel;
        public Button startButton,visibilityButton;
        public RectTransform deviceContent;
        public DeviceRow deviceRowPrefab;
        private int revision=-1;
        private GameSession Session => GameSession.Instance;
        public void CreateGame()=>Session.CreateGame();
        public void JoinGame()=>Session.JoinGame();
        public void LocalGame()=>Session.StartLocal();
        public void Quit()=>Session.Quit();
        public void Back()=>Session.Cancel();
        public void Retry()=>Session.Retry();
        public void Visibility()=>Session.Discoverable();
        public void Ready()=>Session.ToggleReady();
        public void StartMatch()=>Session.StartMatch();
        private void Update()
        {
            var s=Session;if(s==null)return;
            mainPanel.SetActive(s.Page==MenuPage.Main);connectionPanel.SetActive(s.Page==MenuPage.Connection);lobbyPanel.SetActive(s.Page==MenuPage.Lobby);
            notice.text=s.Status;notice.gameObject.SetActive(s.Page==MenuPage.Main&&!string.IsNullOrEmpty(s.Status)&&s.Status!="Выберите режим игры");connectionTitle.text=s.Mode==SessionMode.Host?"Ожидание второго игрока":"Подключиться к игре";connectionStatus.text=s.Status;
            visibilityButton.gameObject.SetActive(s.Mode==SessionMode.Host);
            deviceContent.parent.parent.gameObject.SetActive(s.Mode==SessionMode.Client);
            readyLabel.text=s.LocalReady?"Отменить готовность":"Я готов";
            lobbyStatus.text="Вы: "+(s.LocalReady?"готовы":"не готовы")+"\nСоперник: "+(s.PeerReady?"готов":"не готов")+"\n"+(s.Mode==SessionMode.Host?"Вы создаёте игру":"Матч запускает хост");
            startButton.gameObject.SetActive(s.Mode==SessionMode.Host);startButton.interactable=s.CanStart;
            if(revision==s.DeviceRevision)return;revision=s.DeviceRevision;
            foreach(Transform child in deviceContent)Destroy(child.gameObject);
            foreach(var device in s.Devices){var row=Instantiate(deviceRowPrefab,deviceContent);row.Bind(device,()=>s.SelectDevice(device.address));}
        }
    }
}
