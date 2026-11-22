using UnityEngine;
namespace WinterDuel.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D),typeof(CircleCollider2D))]
    public sealed class DuelProjectile : MonoBehaviour
    {
        public float radius=0.17f;
        private DuelMatchController match;
        private DuelSettings settings;
        private Rigidbody2D body;
        private CircleCollider2D ownCollider;
        private Vector2 velocity;
        private float wind,age;
        private int owner;
        private bool resolved;
        private readonly RaycastHit2D[] hits=new RaycastHit2D[16];
        public Vector2 Velocity => velocity;
        public void Launch(DuelMatchController controller,int player,Vector2 initialVelocity,float turnWind)
        {
            match=controller;settings=controller.settings;owner=player;velocity=initialVelocity;wind=turnWind;
            body=GetComponent<Rigidbody2D>();ownCollider=GetComponent<CircleCollider2D>();
            body.position=transform.position;
        }
        private void FixedUpdate()
        {
            if(resolved || match==null)return;
            var previous=body.position;var next=previous;
            ShotMotion.Step(ref next,ref velocity,Time.fixedDeltaTime,settings.gravity,wind);
            var delta=next-previous;
            var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.AllLayers);filter.useTriggers=false;
            int count=Physics2D.CircleCast(previous,radius,delta.normalized,filter,hits,delta.magnitude);
            RaycastHit2D closest=default;float distance=float.MaxValue;int victim=-1;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.collider==ownCollider)continue;
                var character=hit.collider.GetComponentInParent<DuelCharacter>();
                if(character!=null && character.playerIndex==owner)continue;
                if(hit.distance>=distance)continue;
                closest=hit;distance=hit.distance;victim=character==null?-1:character.playerIndex;
            }
            age+=Time.fixedDeltaTime;
            if(distance<float.MaxValue){body.position=closest.centroid;Finish(victim);return;}
            body.position=next;
            if(age>=settings.flightTimeout || next.x<settings.fieldLeft || next.x>settings.fieldRight || next.y<settings.minimumY || next.y>settings.maximumY)Finish(-1);
        }
        private void Finish(int victim)
        {
            if(resolved)return;resolved=true;
            match.FlightFinished(this,victim,velocity);
            gameObject.SetActive(false);Destroy(gameObject);
        }
    }
}
