using System.Collections;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VelvetVows
{
    public sealed class Phase2BMurderSequence : MonoBehaviour
    {
        public GameManager game;
        public GameHUD hud;
        public Phase2ALovesick lovesick;
        public Phase2BCleanupManager cleanup;
        public CharacterVoiceManager voice;
        public Transform evelyn, celeste;
        public Animator evelynAnimator, celesteAnimator;
        public Phase2AActorMotion celesteMotion;
        public AcademyCharacterAnimation playerAnimation;
        public CharacterController celesteController;
        public AnimationClip attack, damage, death;
        public GameObject attackProp, clothingMarks, bodyInteraction, bloodPool;
        public Camera cinematicCamera;
        [Header("Right-hand prop placement")]
        public Vector3 propLocalPosition = new Vector3(0, .055f, .08f);
        public Vector3 propLocalEuler = new Vector3(0, 0, 85);
        private CinemachineBrain brain;
        private readonly PlayableGraph[] graphs = new PlayableGraph[2];
        private readonly AnimationClipPlayable[] playables = new AnimationClipPlayable[2];
        private Renderer[] bodyRenderers;
        private Vector3 corpsePosition;
        private Quaternion corpseRotation;
        private bool begun;

        private void Awake()
        {
            if (cinematicCamera == null) cinematicCamera = Camera.main;
            if (cinematicCamera != null) brain = cinematicCamera.GetComponent<CinemachineBrain>();
            if (celeste != null) bodyRenderers = celeste.GetComponentsInChildren<Renderer>(true);
            if (attackProp != null) attackProp.SetActive(false);
            if (clothingMarks != null) clothingMarks.SetActive(false);
            if (bodyInteraction != null) bodyInteraction.SetActive(false);
            if (bloodPool != null) bloodPool.SetActive(false);
        }
        private void OnDestroy()
        {
            for (int i = 0; i < graphs.Length; i++) if (graphs[i].IsValid()) graphs[i].Destroy();
        }
        public void Begin()
        {
            if (begun) return;
            begun = true;
            StartCoroutine(Sequence());
        }
        private IEnumerator Sequence()
        {
            if (game == null || evelynAnimator == null || celesteAnimator == null ||
                attack == null || damage == null || death == null || cleanup == null)
            {
                Debug.LogError("Phase 2B cinematic references are incomplete", this);
                yield break;
            }
            game.BeginPhase2BCinematic();
            hud.HideClue();
            lovesick.Suppressed = true;
            if (playerAnimation != null) playerAnimation.enabled = false;
            if (celesteMotion != null) celesteMotion.enabled = false;
            if (celesteController != null) celesteController.enabled = false;
            if (brain != null) brain.enabled = false;
            var midpoint = (evelyn.position + celeste.position) * .5f;
            Frame(midpoint + new Vector3(-.7f, 2.1f, -4.4f), midpoint + Vector3.up, 42);
            var away = celeste.position - evelyn.position; away.y = 0;
            if (away.sqrMagnitude > .01f)
            {
                celeste.rotation = Quaternion.LookRotation(-away.normalized);
                evelyn.rotation = Quaternion.LookRotation(away.normalized);
                var retreat = evelyn.position + away.normalized * 1.05f;
                for (float t = 0; t < .42f; t += Time.deltaTime)
                { celeste.position = Vector3.MoveTowards(celeste.position, retreat, .6f * Time.deltaTime); yield return null; }
            }
            AttachWeaponToPalm();
            attackProp.SetActive(true);
            yield return new WaitForSeconds(.42f);
            Play(0, evelynAnimator, attack);
            var performance = evelynAnimator.GetComponent<AcademyBodyPerformance>();
            var chest = celesteAnimator.GetBoneTransform(HumanBodyBones.Chest);
            float strikeTime = Mathf.Max(.1f, attack.length * .34f);
            for (float time = 0; time < strikeTime; time += Time.deltaTime)
            {
                if (performance != null && chest != null)
                    performance.SetHands(chest.position, null, .85f * Mathf.SmoothStep(0, 1, time / strikeTime));
                yield return null;
            }
            Play(1, celesteAnimator, damage);
            voice?.PlayImpact();
            for (float time = 0; time < .18f; time += Time.deltaTime)
            {
                if (performance != null && chest != null)
                    performance.SetHands(chest.position, null, .85f * (1 - Mathf.SmoothStep(0, 1, time / .18f)));
                yield return null;
            }
            performance?.ClearHands();
            yield return new WaitForSeconds(Mathf.Max(.1f, damage.length * .76f - .18f));
            Play(1, celesteAnimator, death);
            var presence = celesteAnimator.GetComponent<AcademyCharacterPresence>();
            if (presence != null) presence.enabled = false;
            yield return new WaitForSeconds(Mathf.Min(1.0f, death.length * .55f));
            bloodPool.transform.position = new Vector3(celeste.position.x, .045f, celeste.position.z);
            bloodPool.SetActive(true);
            clothingMarks.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(.1f, death.length - Mathf.Min(1.0f, death.length * .55f)));
            playables[1].SetTime(death.length);
            playables[1].SetSpeed(0);
            graphs[1].Evaluate(0);
            AlignCorpseToFloor();
            voice?.PlayPostKillLaugh();
            corpsePosition = celeste.position;
            corpseRotation = celeste.rotation;
            bodyInteraction.transform.position = celeste.position + Vector3.up * .45f;
            bodyInteraction.SetActive(true);
            if (graphs[0].IsValid()) graphs[0].Destroy();
            if (playerAnimation != null) playerAnimation.enabled = true;
            if (brain != null) brain.enabled = true;
            yield return new WaitForSeconds(1.6f);
            voice?.StopVoice();
            cleanup.BeginChallenge();
        }
        private void Play(int slot, Animator animator, AnimationClip clip)
        {
            if (graphs[slot].IsValid()) graphs[slot].Destroy();
            animator.applyRootMotion = false;
            graphs[slot] = PlayableGraph.Create("Phase2B " + clip.name);
            playables[slot] = AnimationClipPlayable.Create(graphs[slot], clip);
            playables[slot].SetApplyFootIK(true);
            playables[slot].SetDuration(clip.length);
            var output = AnimationPlayableOutput.Create(graphs[slot], clip.name, animator);
            output.SetSourcePlayable(playables[slot]);
            graphs[slot].Play();
        }
        private void AttachWeaponToPalm()
        {
            var hand = evelynAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            var finger = evelynAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if (hand == null) return;
            var prop = attackProp.transform;
            prop.SetParent(hand, false);
            prop.localRotation = Quaternion.Euler(propLocalEuler);
            // The palette knife's origin is below its handle. Place the handle itself
            // inside the visible palm instead of applying the old wrist-space offset.
            var handle = prop.Find("Wooden handle");
            var palm = finger != null ? Vector3.Lerp(hand.position, finger.position, .65f) : hand.TransformPoint(propLocalPosition);
            prop.position = palm;
            if (handle != null) prop.position += palm - handle.position;
        }
        private void Frame(Vector3 position, Vector3 target, float fov)
        {
            bool Inside(Vector3 p)=>p.x>-23.3f&&p.x<-12.7f&&p.z>-7.2f&&p.z<3.2f;
            bool Clear(Vector3 p)=>Inside(p)&&Phase2AStoryManager.CameraPathClear(p,target);
            if(!Clear(position))
            {
                bool found=false;
                foreach(float distance in new[]{3.1f,2.5f,1.9f})
                {
                    for(int angle=0;angle<360;angle+=30)
                    {
                        var candidate=target+Quaternion.Euler(0,angle,0)*Vector3.right*distance+Vector3.up*.85f;
                        if(!Clear(candidate))continue;
                        position=candidate;found=true;break;
                    }
                    if(found)break;
                }
                if(!found) position=new Vector3(Mathf.Clamp(position.x,-23.2f,-12.8f),Mathf.Max(position.y,1.7f),Mathf.Clamp(position.z,-7.1f,3.1f));
            }
            cinematicCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            cinematicCamera.fieldOfView = fov;
            Debug.Log("[Phase3L] Murder camera="+position+" target="+target+" clear="+Phase2AStoryManager.CameraPathClear(position,target));
        }
        private void AlignCorpseToFloor()
        {
            float bottom = float.PositiveInfinity;
            foreach (var renderer in bodyRenderers)
            {
                if (!renderer.enabled) continue;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var baked = new Mesh(); skinned.BakeMesh(baked);
                    var matrix = skinned.transform.localToWorldMatrix;
                    foreach (var vertex in baked.vertices) bottom = Mathf.Min(bottom, matrix.MultiplyPoint3x4(vertex).y);
                    Destroy(baked);
                }
                else bottom = Mathf.Min(bottom, renderer.bounds.min.y);
            }
            if (!float.IsInfinity(bottom)) celeste.position += Vector3.up * (.02f - bottom);
        }
        public void SetCorpseVisible(bool visible)
        {
            foreach (var renderer in bodyRenderers) renderer.enabled = visible;
            bodyInteraction.SetActive(visible);
        }
        public void ResetCorpse()
        {
            celeste.SetPositionAndRotation(corpsePosition, corpseRotation);
            SetCorpseVisible(true);
            bodyInteraction.transform.position = corpsePosition + Vector3.up * .45f;
            bloodPool.transform.position = new Vector3(corpsePosition.x, .045f, corpsePosition.z);
            bloodPool.SetActive(true);
            attackProp.SetActive(true);
            clothingMarks.SetActive(true);
            if (graphs[1].IsValid())
            { playables[1].SetTime(death.length); playables[1].SetSpeed(0); graphs[1].Evaluate(0); }
        }
    }
}
