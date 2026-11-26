using UnityEngine;
using WinterDuel.Environment;
namespace WinterDuel.Gameplay
{
    public sealed class DuelCharacter : MonoBehaviour
    {
        public int playerIndex;
        public float initialX;
        public SnowTerrain ground;
        public Transform throwOrigin;
        public BoxCollider2D hitbox;
        public Vector2 leftFoot, rightFoot;
        [Header("Optional flat platform limits")]
        public bool constrainPlacement;
        public float placementMinX, placementMaxX;
        public Vector2 LaunchOrigin => throwOrigin.position;
        public bool Contains(Vector2 point) => hitbox!=null && hitbox.OverlapPoint(point);
        public void Place(float x)
        {
            if(constrainPlacement) x=Mathf.Clamp(x,placementMinX,placementMaxX);
            var a=leftFoot;var b=rightFoot;
            if(a.x>b.x){var t=a;a=b;b=t;}
            float angle=0,y=0;
            for(int i=0;i<5;i++)
            {
                var q=Quaternion.Euler(0,0,angle);var ra=q*(Vector3)a;var rb=q*(Vector3)b;
                float ya=ground.GetWorldSurfacePoint(x+ra.x).y,yb=ground.GetWorldSurfacePoint(x+rb.x).y;
                angle=(Mathf.Atan2(yb-ya,rb.x-ra.x)-Mathf.Atan2(b.y-a.y,b.x-a.x))*Mathf.Rad2Deg;
                y=(ya+yb)*0.5f;
            }
            transform.SetPositionAndRotation(new Vector3(x,y,0),Quaternion.Euler(0,0,angle));
            Physics2D.SyncTransforms();
        }
    }
}
