using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace WinterDuel.App
{
    public sealed class DeviceRow : MonoBehaviour
    {
        public Text label;
        public Button button;
        public void Bind(DiscoveredDevice device,UnityAction select)
        {
            label.text=device.name+(device.paired?"  · сопряжено":"")+"\n"+device.address;
            button.onClick.RemoveAllListeners();button.onClick.AddListener(select);
        }
    }
}
