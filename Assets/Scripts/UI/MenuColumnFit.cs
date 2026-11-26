using UnityEngine;
namespace WinterDuel.UI
{
    /// <summary>Keeps the saved left column clear of the illustration on narrower phones.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MenuColumnFit : MonoBehaviour
    {
        public RectTransform safeArea, column;
        public float referenceAspect=13f/6f;
        [Range(.75f,1)] public float scaleAt16By9=.9f;
        [Range(.65f,1)] public float minimumScale=.84f;
        private void OnEnable()=>Refresh();
        private void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(safeArea==null||column==null||safeArea.rect.height<=0)return;
            float aspect=safeArea.rect.width/safeArea.rect.height;
            float slope=(1-scaleAt16By9)/Mathf.Max(.01f,referenceAspect-16f/9f);
            float scale=Mathf.Clamp(1+(aspect-referenceAspect)*slope,minimumScale,1);
            var value=new Vector3(scale,scale,1);
            if(column.localScale!=value)column.localScale=value;
        }
    }
}
