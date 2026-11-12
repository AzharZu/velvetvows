using StarterAssets;
using UnityEngine;

namespace VelvetVows
{
    // The stock overlap probe includes Default, which is also the player's capsule layer.
    // Resolve real downward support before movement and again before visible animation.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ThirdPersonController))]
    public sealed class AcademyGrounding : MonoBehaviour
    {
        private ThirdPersonController movement;
        private readonly RaycastHit[] hits=new RaycastHit[32];
        public Vector3 SupportNormal { get; private set; }=Vector3.up;
        private void Awake()=>movement=GetComponent<ThirdPersonController>();
        private void Update()=>Check();
        private void LateUpdate()=>Check();
        private void Check()
        {
            if(movement==null||!movement.enabled)return;
            float radius=Mathf.Max(.08f,movement.GroundedRadius*.8f);
            float distance=Mathf.Max(.1f,.5f+movement.GroundedOffset+movement.GroundedRadius-radius);
            int count=Physics.SphereCastNonAlloc(transform.position+Vector3.up*.5f,radius,Vector3.down,hits,distance,
                movement.GroundLayers,QueryTriggerInteraction.Ignore);
            bool grounded=false;float closest=float.PositiveInfinity;SupportNormal=Vector3.up;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(hit.collider==null||hit.collider.transform.IsChildOf(transform)||hit.normal.y<.55f||
                    hit.collider.GetComponentInParent<CharacterController>()!=null)continue;
                grounded=true;
                if(hit.distance<closest){closest=hit.distance;SupportNormal=hit.normal;}
            }
            movement.Grounded=grounded;
        }
    }
}
