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
        [SerializeField] private bool variedIdle = true;
        [SerializeField, Min(.1f)] private float walkSpeed = 1.35f;
        private PlayableGraph graph;
        private string current;
        public void Configure(Animator actor, AnimationClip idle1, AnimationClip idle2, AnimationClip talking, AnimationClip walking)
        { animator = actor; idleA = idle1; idleB = idle2; talk = talking; walk = walking; }
        private void OnEnable() { if (animator != null && idleA != null) Idle(); }
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
            current = null;
        }
        private void Play(AnimationClip clip, string state, bool randomOffset)
        {
            if (clip == null || animator == null || current == state) return;
            StopMotion();
            animator.applyRootMotion = false;
            graph = PlayableGraph.Create("Phase2A " + name + " " + state);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            if (randomOffset && clip.length > .1f) playable.SetTime(Random.value * clip.length);
            var output = AnimationPlayableOutput.Create(graph, state, animator);
            output.SetSourcePlayable(playable);
            graph.Play();
            current = state;
        }

        public IEnumerator WalkRoute(params Vector3[] points)
        {
            Walk();
            foreach (var point in points)
            {
                while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(point.x, 0, point.z)) > .15f)
                {
                    var destination = new Vector3(point.x, transform.position.y, point.z);
                    var direction = destination - transform.position;
                    if (direction.sqrMagnitude > .001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), Time.deltaTime * 7f);
                        transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * Time.deltaTime);
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
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(delta.normalized);
        }
    }
}
