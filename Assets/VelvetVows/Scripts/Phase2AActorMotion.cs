using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VelvetVows
{
    public sealed class Phase2AActorMotion : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip idleA, idleB, talk, walk;
        [SerializeField] private AnimationClip turnLeft, turnRight;
        [SerializeField] private bool variedIdle = true;
        [SerializeField, Min(.1f)] private float walkSpeed = 1.35f;
        private PlayableGraph graph;
        private string current;
        private AnimationMixerPlayable mixer;
        private AnimationClipPlayable outgoing, incoming;
        private float blendTime;
        private bool blending;
        private Transform facing;
        private Vector3 previousPosition;
        public void ConfigureTurns(AnimationClip left, AnimationClip right) { turnLeft = left; turnRight = right; }
        public bool IsBlending => blending;
        public void Configure(Animator actor, AnimationClip idle1, AnimationClip idle2, AnimationClip talking, AnimationClip walking)
        { animator = actor; idleA = idle1; idleB = idle2; talk = talking; walk = walking; }
        private void OnEnable() { previousPosition = transform.position; if (animator != null && idleA != null) Idle(); }
        private void OnDisable() => StopMotion();
        private void OnDestroy() => StopMotion();

        public void Idle()
        {
            var clip = variedIdle && idleB != null && Random.value > .5f ? idleB : idleA;
            Play(clip, "Idle", true);
        }
        public void Talk() => Play(talk != null ? talk : idleA, "Talk", true);
        public void Walk() => Play(walk != null ? walk : idleA, "Walk", true);
        public void StopMotion()
        {
            if (graph.IsValid()) graph.Destroy();
            current = null; blending = false; facing = null;
        }
        private void Play(AnimationClip clip, string state, bool randomOffset)
        {
            if (clip == null || animator == null || current == state) return;
            animator.applyRootMotion = false;
            if (!graph.IsValid())
            {
                graph = PlayableGraph.Create("Phase2A " + name);
                mixer = AnimationMixerPlayable.Create(graph, 2);
                var output = AnimationPlayableOutput.Create(graph, "Actor motion", animator);
                output.SetSourcePlayable(mixer);
                graph.Play();
            }
            if (outgoing.IsValid()) { mixer.DisconnectInput(0); graph.DestroyPlayable(outgoing); }
            if (incoming.IsValid())
            {
                mixer.DisconnectInput(1); outgoing = incoming;
                graph.Connect(outgoing, 0, mixer, 0); mixer.SetInputWeight(0, 1);
            }
            incoming = AnimationClipPlayable.Create(graph, clip);
            incoming.SetApplyFootIK(true);
            if (randomOffset && state == "Idle" && clip.length > .1f) incoming.SetTime(Random.value * clip.length);
            graph.Connect(incoming, 0, mixer, 1);
            blending = outgoing.IsValid(); blendTime = 0;
            mixer.SetInputWeight(1, blending ? 0 : 1);
            current = state;
        }

        private void Update()
        {
            float speed = Vector3.Distance(previousPosition, transform.position) / Mathf.Max(.001f, Time.deltaTime);
            previousPosition = transform.position;
            if (current == "Walk" && incoming.IsValid()) incoming.SetSpeed(Mathf.Clamp(speed / 1.35f, .25f, 1.6f));
            if (blending && graph.IsValid())
            {
                blendTime += Time.deltaTime;
                float weight = Mathf.SmoothStep(0, 1, blendTime / .18f);
                mixer.SetInputWeight(0, 1 - weight); mixer.SetInputWeight(1, weight);
                if (weight >= 1)
                {
                    mixer.DisconnectInput(0); graph.DestroyPlayable(outgoing);
                    outgoing = default; blending = false;
                }
            }
            if (facing != null)
            {
                var direction = facing.position - transform.position; direction.y = 0;
                if (direction.sqrMagnitude > .01f)
                {
                    float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                    if ((current == "Idle" || current == "Turn") && Mathf.Abs(angle) > 25)
                        Play(angle > 0 ? turnRight : turnLeft, "Turn", false);
                    else if (current == "Turn" && Mathf.Abs(angle) < 8) Idle();
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 240 * Time.deltaTime);
                }
            }
        }

        public IEnumerator WalkRoute(params Vector3[] points)
        {
            facing = null;
            Walk();
            foreach (var point in points)
            {
                while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(point.x, 0, point.z)) > .15f)
                {
                    var destination = new Vector3(point.x, transform.position.y, point.z);
                    var direction = destination - transform.position;
                    if (direction.sqrMagnitude > .001f)
                    {
                        float angle = Vector3.Angle(transform.forward, direction);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction.normalized), Time.deltaTime * 200);
                        transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * (angle > 50 ? .35f : 1) * Time.deltaTime);
                    }
                    yield return null;
                }
            }
            Idle();
        }

        public void Face(Transform other)
        {
            if (other == null) return;
            var delta = other.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude > .01f) facing = other;
        }
    }
}
