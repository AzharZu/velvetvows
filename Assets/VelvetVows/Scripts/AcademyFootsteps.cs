using System.Linq;
using StarterAssets;
using UnityEngine;

namespace VelvetVows
{
    // Driven by travelled ground distance, so imported clips without foot events remain audible.
    public sealed class AcademyFootsteps : MonoBehaviour
    {
        [SerializeField] private AudioClip[] clips;
        private Transform actor;
        private ThirdPersonController player;
        private AcademyCharacterPresence presence;
        private AudioSource source;
        private AudioLowPassFilter lowPass;
        private Renderer[] rugs;
        private Vector3 previous;
        private float distance, lastStep = -10;
        private int clipIndex;
        private Transform leftFoot, rightFoot;
        private float leftHeight, rightHeight;
        private bool leftContact, rightContact;
        public int StepCount { get; private set; }
        public void Configure(AudioClip[] sounds) { clips = sounds; }
        private void Awake()
        {
            player = GetComponentInParent<ThirdPersonController>();
            presence = GetComponent<AcademyCharacterPresence>();
            actor = player != null ? player.transform : transform;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 1;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f; source.maxDistance = 17; source.dopplerLevel = 0;
            lowPass = gameObject.AddComponent<AudioLowPassFilter>(); lowPass.cutoffFrequency = 22000;
            rugs = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r =>
                r.name.ToLowerInvariant().Contains("rug") || r.name.ToLowerInvariant().Contains("carpet")).ToArray();
            previous = actor.position;
            var animator = GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            { leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot); rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot); }
        }
        private void LateUpdate()
        {
            Vector3 delta = actor.position - previous; previous = actor.position;
            float travel = new Vector2(delta.x, delta.z).magnitude;
            bool grounded = player != null ? player.Grounded : Physics.Raycast(actor.position + Vector3.up * .2f,
                Vector3.down, .5f, 257, QueryTriggerInteraction.Ignore);
            if (!grounded || travel > .5f || (presence != null && !presence.enabled)) { distance = 0; return; }
            if (travel < .0005f) { distance = 0; return; }
            distance += travel;
            float speed = travel / Mathf.Max(Time.deltaTime, .001f);
            bool contact = Contact(leftFoot, ref leftHeight, ref leftContact) | Contact(rightFoot, ref rightHeight, ref rightContact);
            if (contact && distance > .12f) { distance = 0; Step(speed); }
            // A fallback for non-humanoid/culled clips; normal cadence comes from foot contact.
            else if (distance > (speed > 3 ? 1.55f : .95f)) { distance = 0; Step(speed); }
        }
        private bool Contact(Transform foot, ref float previousHeight, ref bool wasContact)
        {
            if (foot == null) return false;
            float height = foot.position.y - actor.position.y;
            bool contact = height < .15f && height <= previousHeight + .002f;
            bool landed = contact && !wasContact;
            wasContact = contact; previousHeight = height; return landed;
        }
        private void Step(float speed)
        {
            if (clips == null || clips.Length == 0 || Time.time - lastStep < .17f) return;
            lastStep = Time.time;
            bool soft = false;
            foreach (var rug in rugs)
            {
                if (rug == null || !rug.gameObject.activeInHierarchy) continue;
                var bounds = rug.bounds;
                if (actor.position.x >= bounds.min.x && actor.position.x <= bounds.max.x &&
                    actor.position.z >= bounds.min.z && actor.position.z <= bounds.max.z &&
                    Mathf.Abs(actor.position.y - bounds.max.y) < .35f) { soft = true; break; }
            }
            clipIndex = (clipIndex + Random.Range(1, Mathf.Max(2, clips.Length))) % clips.Length;
            bool blocked = player == null && Camera.main != null && Physics.Linecast(Camera.main.transform.position,
                actor.position + Vector3.up, 256, QueryTriggerInteraction.Ignore);
            lowPass.cutoffFrequency = blocked ? 1800 : soft ? 7500 : 22000;
            source.pitch = Random.Range(.95f, 1.04f) * (soft ? .92f : 1);
            source.PlayOneShot(clips[clipIndex], (player != null ? .20f : .27f) * (soft ? .38f : 1) * (speed > 3 ? 1.2f : 1) * (blocked ? .32f : 1));
            StepCount++;
        }
        // Receive events from imported clips; distance cadence above avoids double footsteps.
        private void OnFootstep(AnimationEvent e) { if (distance > .12f) { distance = 0; Step(1); } }
        private void OnLand(AnimationEvent e) { if (player != null && player.Grounded && distance > .12f) { distance = 0; Step(1); } }
        private void OnEnable() { if (actor != null) previous = actor.position; distance = 0; }
        private void OnDisable() { if (source != null) source.Stop(); }
    }
}
