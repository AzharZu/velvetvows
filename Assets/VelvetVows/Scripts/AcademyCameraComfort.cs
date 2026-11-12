using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
namespace VelvetVows
{
    [DefaultExecutionOrder(20000)]
    public sealed class AcademyCameraComfort : MonoBehaviour
    {
        [SerializeField] private Animator playerModel;
        private CinemachineBrain brain;
        private Transform head;
        private CharacterController playerController;
        private Camera viewCamera;
        private Renderer[] body;
        private ShadowCastingMode[] original;
        private float wallViewTime;
        private int solidLayers;
        private readonly RaycastHit[] wallHits = new RaycastHit[32];
        private readonly Collider[] nearbyWalls = new Collider[32];
        public bool AvatarHidden { get; private set; }
        public int WallRecoveryCount { get; private set; }
        public void Configure(Animator model)=>playerModel=model;
        private void Awake()
        {
            brain=GetComponent<CinemachineBrain>();
            viewCamera=GetComponent<Camera>();
            solidLayers = LayerMask.GetMask("Academy Solid");
            if(playerModel==null)return;
            head=playerModel.GetBoneTransform(HumanBodyBones.Head);
            playerController=playerModel.GetComponentInParent<CharacterController>();
            body=playerModel.GetComponentsInChildren<Renderer>(true);original=new ShadowCastingMode[body.Length];
            for(int i=0;i<body.Length;i++)original[i]=body[i].shadowCastingMode;
        }
        private void LateUpdate()
        {
            if(head==null||brain==null)return;
            RecoverWallView();
            float distance=Vector3.Distance(transform.position,head.position);
            bool hide=brain.enabled&&distance<(AvatarHidden?1.15f:.90f);
            if(hide==AvatarHidden)return;
            AvatarHidden=hide;
            for(int i=0;i<body.Length;i++)if(body[i]!=null)body[i].shadowCastingMode=hide?ShadowCastingMode.ShadowsOnly:original[i];
        }
        private void RecoverWallView()
        {
            if (!brain.enabled || DialogueManager.IsOpen || solidLayers == 0)
            { wallViewTime = 0; return; }
            var hitWall = false; var nearWall = false;
            int hitCount = Physics.SphereCastNonAlloc(transform.position, .08f, transform.forward, wallHits, 3f, solidLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = wallHits[i];
                if (Mathf.Abs(hit.normal.y) < .65f && IsWorld(hit.transform))
                { hitWall = true; if (hit.distance <= .65f) nearWall = true; }
            }
            if (!nearWall)
            {
                int nearbyCount = Physics.OverlapSphereNonAlloc(transform.position, .12f, nearbyWalls, solidLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < nearbyCount; i++)
                    if (IsWorld(nearbyWalls[i].transform)) { hitWall = nearWall = true; break; }
            }
            // Give normal obstacle avoidance a moment before changing the player's view.
            bool veryClose = Vector3.Distance(transform.position, head.position) < 1.25f;
            var viewport = viewCamera != null ? viewCamera.WorldToViewportPoint(head.position) : Vector3.zero;
            bool playerOffscreen = viewport.z < .05f || viewport.x < -.08f || viewport.x > 1.08f || viewport.y < -.1f || viewport.y > 1.1f;
            var velocity = playerController != null ? playerController.velocity : Vector3.zero; velocity.y = 0;
            bool blockedWhileMoving = hitWall && playerOffscreen && velocity.sqrMagnitude > .04f;
            wallViewTime = (nearWall && veryClose || blockedWhileMoving) ? wallViewTime + Time.deltaTime : 0;
            if (wallViewTime < .3f) return;
            var focus = head.position - Vector3.up * .18f;
            var radial = transform.position - focus; radial.y = 0;
            if (radial.sqrMagnitude < .01f) radial = -playerModel.transform.forward;
            radial.Normalize();
            foreach (float radius in new[] { 2f, 1.45f, .9f })
                foreach (float angle in new[] { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f })
                {
                    var candidate = focus + Quaternion.Euler(0, angle, 0) * radial * radius + Vector3.up * .3f;
                    if (!Phase2AStoryManager.CameraPathClear(candidate, focus)) continue;
                    transform.SetPositionAndRotation(candidate, Quaternion.LookRotation(focus - candidate));
                    WallRecoveryCount++;
                    return;
                }
        }
        private static bool IsWorld(Transform item)
        {
            for (var parent = item; parent != null; parent = parent.parent)
                if (parent.name == "ENVIRONMENT" || parent.name == "GOTHIC_ACADEMY" || parent.name == "PHASE 2B CLEANUP" ||
                    parent.GetComponent<Phase2AStudioDoor>() != null) return true;
            return false;
        }
        private void OnDisable()
        {
            wallViewTime = 0;
            if(!AvatarHidden||body==null)return;
            for(int i=0;i<body.Length;i++)if(body[i]!=null)body[i].shadowCastingMode=original[i];
            AvatarHidden=false;
        }
    }
}
