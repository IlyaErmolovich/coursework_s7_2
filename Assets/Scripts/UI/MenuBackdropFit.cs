using UnityEngine;
namespace WinterDuel.UI
{
    /// <summary>Uniform cover fit for saved artwork. Smoke shares the artwork's coordinates.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class MenuBackdropFit : MonoBehaviour
    {
        public Camera viewCamera;
        public SpriteRenderer artwork;
        public Transform chimneyEmitter;
        [Tooltip("Chimney mouth in source-image coordinates, measured from bottom left.")]
        public Vector2 chimneyUV = new Vector2(1492f/1844f, 1f-214f/853f);
        public float artworkDistance = 10;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if(viewCamera==null||artwork==null||artwork.sprite==null)return;
            var bounds=artwork.sprite.bounds;
            float height=viewCamera.orthographicSize*2;
            float scale=Mathf.Max(height/bounds.size.y,height*viewCamera.aspect/bounds.size.x);
            var size=new Vector3(scale,scale,1);
            var center=new Vector3(0,0,artworkDistance);
            if(artwork.transform.localScale!=size)artwork.transform.localScale=size;
            if(artwork.transform.localPosition!=center)artwork.transform.localPosition=center;
            if(chimneyEmitter!=null)
            {
                var position=new Vector3(bounds.min.x+chimneyUV.x*bounds.size.x,bounds.min.y+chimneyUV.y*bounds.size.y,-.1f);
                if(chimneyEmitter.localPosition!=position)chimneyEmitter.localPosition=position;
            }
        }
    }
}
