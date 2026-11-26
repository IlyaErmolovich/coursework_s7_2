using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
namespace WinterDuel.Networking
{
    [Serializable] public sealed class BluetoothEvent
    { public string type, text, address; public bool paired; }

    /// <summary>Unity main-thread event pump for the native, asynchronous RFCOMM transport.</summary>
    public sealed class ClassicBluetoothTransport : MonoBehaviour
    {
        public event Action<BluetoothEvent> Received;
        public bool Connected { get; private set; }
#if UNITY_EDITOR
        // Observe serialized packets for protocol tests; this never replaces the native Android transport.
        public event Action<string> ObservedSend;
#endif
        private int attempt;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject native;
        private void EnsureNative()
        {
            if(native!=null)return;
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                native=new AndroidJavaObject("com.winterduel.bluetooth.ClassicBluetooth",activity);
        }
        private void Native(string method,params object[] args)
        {
            try{EnsureNative();native.Call(method,args);}
            catch(Exception){Raise("error","Ошибка Bluetooth. Проверьте разрешения и повторите подключение.");}
        }
#endif
        private void Raise(string type,string text) => Received?.Invoke(new BluetoothEvent{type=type,text=text});
        public void Begin(bool host)
        {
            Cancel();int token=attempt;
#if UNITY_ANDROID && !UNITY_EDITOR
            try{EnsureNative();if(native.Call<string>("state")=="unsupported"){Raise("error","Устройство не поддерживает Bluetooth Classic. Доступна локальная игра.");return;}}catch(Exception){Raise("error","Не удалось проверить Bluetooth. Повторите попытку.");return;}
            using(var version=new AndroidJavaClass("android.os.Build$VERSION"))
            {
                string[] permissions=version.GetStatic<int>("SDK_INT")>=31?
                    new[]{"android.permission.BLUETOOTH_SCAN","android.permission.BLUETOOTH_CONNECT","android.permission.BLUETOOTH_ADVERTISE"}:
                    host?new string[0]:new[]{Permission.FineLocation};
                Action go=()=>{if(token==attempt)StartAvailable(host);};
                if(Array.TrueForAll(permissions,Permission.HasUserAuthorizedPermission)){go();return;}
                var callbacks=new PermissionCallbacks();bool finished=false;
                callbacks.PermissionGranted+=_=>{if(!finished&&Array.TrueForAll(permissions,Permission.HasUserAuthorizedPermission)){finished=true;go();}};
                callbacks.PermissionDenied+=_=>{if(finished||token!=attempt)return;finished=true;if(!host)Native("paired");Raise("error","Разрешение отклонено. Нужен доступ к ближайшим устройствам (Android 12+) или геолокации для поиска (Android 11 и ниже). Сопряжённое устройство можно выбрать из списка.");};
                callbacks.PermissionDeniedAndDontAskAgain+=_=>{if(finished||token!=attempt)return;finished=true;if(!host)Native("paired");Raise("error","Доступ к Bluetooth запрещён. Разрешите ближайшие устройства в настройках приложения и повторите.");};
                Permission.RequestUserPermissions(permissions,callbacks);
            }
#else
            Raise("error","Bluetooth Classic доступен в Android APK. В редакторе используйте локальную игру.");
#endif
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        private void StartAvailable(bool host)
        {
            try{
                EnsureNative();string state=native.Call<string>("state");
                if(state=="unsupported"){Raise("error","Устройство не поддерживает Bluetooth Classic. Доступна локальная игра.");return;}
                if(state=="disabled"){Raise("error","Bluetooth выключен. Подтвердите включение, затем нажмите «Повторить».");Native("enable");return;}
                if(state!="ready"){Raise("error","Нет разрешения Bluetooth. Проверьте настройки приложения.");return;}
                Native(host?"host":"scan");
            }catch(Exception){Raise("error","Не удалось запустить Bluetooth. Повторите попытку.");}
        }
#endif
        public void Connect(string address)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Native("connect",address);
#else
            Raise("error","Подключение доступно только на Android.");
#endif
        }
        public void MakeDiscoverable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Native("discoverable");
#endif
        }
        public void Send(string json)
        {
#if UNITY_EDITOR
            ObservedSend?.Invoke(json);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            Native("send",json);
#endif
        }
        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native==null)return;
            for(int i=0;i<128;i++){
                string json=native.Call<string>("poll");if(string.IsNullOrEmpty(json))break;
                try{var e=JsonUtility.FromJson<BluetoothEvent>(json);if(e.type=="connected")Connected=true;if(e.type=="disconnected")Connected=false;Received?.Invoke(e);}
                catch(Exception){Cancel();Raise("error","Неверные данные Bluetooth. Подключитесь заново.");break;}
            }
#endif
        }
        public void Cancel()
        {
            attempt++;Connected=false;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native!=null)native.Call("close");
#endif
        }
        private void OnDestroy()
        {
            Cancel();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native!=null){native.Call("dispose");native.Dispose();native=null;}
#endif
        }
    }
}
