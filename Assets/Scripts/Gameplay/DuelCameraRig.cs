using UnityEngine;
namespace WinterDuel.Gameplay
{
    public sealed class DuelCameraRig : MonoBehaviour
    {
        public DuelMatchController match;
        public Camera gameCamera;
        private Vector3 smoothing;
        public Vector3 ActiveView
        {
            get
            {
                var actor=match.characters[match.State.Active];
                float direction=Mathf.Sign(match.characters[1-match.State.Active].transform.position.x-actor.transform.position.x);
                return Clamp(new Vector3(actor.transform.position.x+direction*gameCamera.orthographicSize*gameCamera.aspect*0.65f,-0.25f,-10));
            }
        }
        private Vector3 Clamp(Vector3 p)
        {
            p.x=Mathf.Clamp(p.x,match.settings.cameraLeft,match.settings.cameraRight);
            p.y=Mathf.Clamp(p.y,-0.25f,match.settings.cameraTop);p.z=-10;return p;
        }
        public bool Settled => match.State!=null && Vector3.Distance(transform.position,ActiveView)<0.15f;
        public void Snap(){smoothing=Vector3.zero;transform.position=ActiveView;}
        private void LateUpdate()
        {
            if(match==null || match.State==null || match.Paused)return;
            Vector3 target=ActiveView;float smooth=match.settings.cameraSmoothTime;
            if(match.ActiveProjectile!=null){target=Clamp(match.ActiveProjectile.transform.position);smooth=0.12f;}
            else if(match.State.Phase==MatchPhase.Resolving)target=Clamp(match.ResultPosition);
            transform.position=Vector3.SmoothDamp(transform.position,target,ref smoothing,smooth,Mathf.Infinity,Time.deltaTime);
        }
    }
}
