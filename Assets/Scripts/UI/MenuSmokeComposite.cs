using UnityEngine;
namespace WinterDuel.UI
{
    /// <summary>Unions particle masks before applying opacity, avoiding overlapping circular bands.</summary>
    [ExecuteAlways, DefaultExecutionOrder(200), DisallowMultipleComponent]
    public sealed class MenuSmokeComposite : MonoBehaviour
    {
        public Transform artworkSpace, chimneyEmitter;
        public Camera coverageCamera;
        public MeshRenderer display;
        public RenderTexture coverage;
        public Vector2 sourceCenterOffset = new Vector2(.35f,.60f);
        [Min(.2f)] public float sourceHalfSize = 1.1f;
        private bool rendering;
        private void LateUpdate()=>RefreshAndRender();
        public void RefreshAndRender()
        {
            if(rendering||artworkSpace==null||chimneyEmitter==null||coverageCamera==null||display==null||coverage==null)return;
            var center=chimneyEmitter.localPosition+(Vector3)sourceCenterOffset;
            coverageCamera.transform.localPosition=new Vector3(center.x,center.y,-5);
            var scale=artworkSpace.lossyScale;
            coverageCamera.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            coverageCamera.orthographicSize=sourceHalfSize*scale.y;
            coverageCamera.aspect=1;
            display.transform.localPosition=new Vector3(center.x,center.y,chimneyEmitter.localPosition.z-.02f);
            display.transform.localScale=new Vector3(sourceHalfSize*2,sourceHalfSize*2,1);
            if(!coverage.IsCreated())coverage.Create();
            coverageCamera.targetTexture=coverage;
            try{rendering=true;coverageCamera.Render();}finally{rendering=false;}
        }
        private void OnDisable(){if(coverage!=null&&coverage.IsCreated())coverage.Release();}
    }
}
