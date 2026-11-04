using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VelvetVows
{
    public sealed class Phase2CBallGuest : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip clip;
        public float phase;
        private PlayableGraph graph;
        private void OnEnable()
        {
            if (animator == null || clip == null) return;
            animator.applyRootMotion = false;
            graph = PlayableGraph.Create("Black Rose Ball guest " + name);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            playable.SetTime(Mathf.Repeat(phase, 1f) * clip.length);
            AnimationPlayableOutput.Create(graph, "Guest", animator).SetSourcePlayable(playable);
            graph.Play();
        }
        private void OnDisable() { if (graph.IsValid()) graph.Destroy(); }
        private void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
    }
}
