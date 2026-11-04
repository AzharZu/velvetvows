using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace VelvetVows
{
    /// <summary>An isolated Play Mode rig and partner-compatibility test.</summary>
    public sealed class AnimationTestDirector : MonoBehaviour
    {
        public GameObject attackEvelyn, attackCeleste;
        public GameObject[] danceFemales, danceMales;
        public AnimationClip attack, damage, death;
        public AnimationClip[] femaleSteps, maleSteps;
        public AnimationClip[] femalePoseBegin, femalePoseLoop, femalePoseStop;
        public AnimationClip[] malePoseBegin, malePoseLoop, malePoseStop;
        public bool autoRun = true;
        public float dancePreviewSeconds = 4f;

        private const float FloorY = 0f;
        private Coroutine routine;
        private PlayableGraph[] graphs;
        private AnimationClipPlayable[] playables;
        private int selected = -1;
        private string status = "Ready";
        private readonly string[] labels = { "Attack Test", "Dance01", "Dance02", "Dance03", "Dance04", "Dance05", "Dance06", "DancePose01", "DancePose02" };

        private void Start()
        {
            graphs = new PlayableGraph[8];
            playables = new AnimationClipPlayable[8];
            Select(0);
            if (autoRun) StartCoroutine(AutoRun());
        }

        private void OnDestroy() => StopAllGraphs();

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame) Select(0);
            if (k.digit2Key.wasPressedThisFrame) Select(1);
            if (k.digit3Key.wasPressedThisFrame) Select(2);
            if (k.digit4Key.wasPressedThisFrame) Select(3);
            if (k.digit5Key.wasPressedThisFrame) Select(4);
            if (k.digit6Key.wasPressedThisFrame) Select(5);
            if (k.digit7Key.wasPressedThisFrame) Select(6);
            if (k.digit8Key.wasPressedThisFrame) Select(7);
            if (k.digit9Key.wasPressedThisFrame) Select(8);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 236, 342), "ANIMATION TEST  |  1–9");
            for (int i = 0; i < labels.Length; i++)
            {
                if (GUI.Button(new Rect(23, 43 + i * 29, 212, 26), (selected == i ? "▶  " : "") + labels[i])) Select(i);
            }
            GUI.Label(new Rect(23, 312, 212, 38), status);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= labels.Length) return;
            if (routine != null) StopCoroutine(routine);
            StopAllGraphs();
            selected = index;
            status = labels[index];
            bool attacking = index == 0;
            attackEvelyn.SetActive(attacking);
            attackCeleste.SetActive(attacking);
            foreach (var actor in danceFemales) actor.SetActive(!attacking);
            foreach (var actor in danceMales) actor.SetActive(!attacking);
            if (attacking) routine = StartCoroutine(AttackSequence());
            else if (index <= 6) StartDance(index - 1);
            else routine = StartCoroutine(PoseSequence(index - 7));
        }

        private IEnumerator AttackSequence()
        {
            var a = attackEvelyn.GetComponentInChildren<Animator>();
            var c = attackCeleste.GetComponentInChildren<Animator>();
            attackEvelyn.transform.SetPositionAndRotation(new Vector3(-0.65f, 0, 0), Quaternion.Euler(0, 90, 0));
            attackCeleste.transform.SetPositionAndRotation(new Vector3(0.65f, 0, 0), Quaternion.Euler(0, -90, 0));
            Play(0, a, attack, false);
            status = "Attack → reaction";
            yield return new WaitForSeconds(Mathf.Max(.1f, attack.length * .34f));
            Play(1, c, damage, false);
            yield return new WaitForSeconds(Mathf.Max(.1f, damage.length * .76f));
            Play(1, c, death, false);
            status = "Death → hold last pose";
            yield return new WaitForSeconds(death.length + .04f);
            playables[1].SetTime(death.length);
            playables[1].SetSpeed(0);
            graphs[1].Evaluate(0);
            AlignBottomToFloor(attackCeleste);
            status = "Death pose held; floor aligned";
            routine = null;
        }

        private void StartDance(int step)
        {
            if (femaleSteps == null || maleSteps == null || step >= femaleSteps.Length || step >= maleSteps.Length) return;
            for (int i = 0; i < danceFemales.Length; i++)
            {
                Play(i * 2, danceFemales[i].GetComponentInChildren<Animator>(), femaleSteps[step], true);
                Play(i * 2 + 1, danceMales[i].GetComponentInChildren<Animator>(), maleSteps[step], true);
            }
            status = $"Dance{step + 1:00}: same-time M/F loops";
        }

        private IEnumerator PoseSequence(int pose)
        {
            for (int i = 0; i < danceFemales.Length; i++)
            {
                Play(i * 2, danceFemales[i].GetComponentInChildren<Animator>(), femalePoseBegin[pose], false);
                Play(i * 2 + 1, danceMales[i].GetComponentInChildren<Animator>(), malePoseBegin[pose], false);
            }
            yield return new WaitForSeconds(Mathf.Max(femalePoseBegin[pose].length, malePoseBegin[pose].length));
            for (int i = 0; i < danceFemales.Length; i++)
            {
                Play(i * 2, danceFemales[i].GetComponentInChildren<Animator>(), femalePoseLoop[pose], true);
                Play(i * 2 + 1, danceMales[i].GetComponentInChildren<Animator>(), malePoseLoop[pose], true);
            }
            status = $"Pose{pose + 1:00}: partner loop";
            // The Stop clips are exposed to the script and played after the preview interval.
            yield return new WaitForSeconds(dancePreviewSeconds);
            for (int i = 0; i < danceFemales.Length; i++)
            {
                Play(i * 2, danceFemales[i].GetComponentInChildren<Animator>(), femalePoseStop[pose], false);
                Play(i * 2 + 1, danceMales[i].GetComponentInChildren<Animator>(), malePoseStop[pose], false);
            }
            status = $"Pose{pose + 1:00}: stop";
            routine = null;
        }

        private void Play(int slot, Animator animator, AnimationClip clip, bool loop)
        {
            if (animator == null || clip == null) { Debug.LogError("Animation test missing Animator/clip", this); return; }
            if (graphs[slot].IsValid()) graphs[slot].Destroy();
            animator.applyRootMotion = false;
            graphs[slot] = PlayableGraph.Create("AnimationTest_" + slot);
            graphs[slot].SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            playables[slot] = AnimationClipPlayable.Create(graphs[slot], clip);
            playables[slot].SetApplyFootIK(true);
            playables[slot].SetDuration(clip.length);
            var output = AnimationPlayableOutput.Create(graphs[slot], "Actor" + slot, animator);
            output.SetSourcePlayable(playables[slot]);
            graphs[slot].Play();
            Debug.Log($"[AnimationTest] Slot {slot}: {clip.name}; humanoid={clip.humanMotion}; duration={clip.length:F2}; loop={loop}", this);
        }

        private void StopAllGraphs()
        {
            if (graphs == null) return;
            for (int i = 0; i < graphs.Length; i++) if (graphs[i].IsValid()) graphs[i].Destroy();
        }

        private static void AlignBottomToFloor(GameObject actor)
        {
            var renderers = actor.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            float bottom = float.PositiveInfinity;
            foreach (var r in renderers)
            {
                if (!r.enabled) continue;
                if (r is SkinnedMeshRenderer skinned)
                {
                    // SkinnedRenderer.bounds is conservative and can include the standing
                    // pose even when the animated body is horizontal. Bake the actual pose.
                    var baked = new Mesh();
                    skinned.BakeMesh(baked);
                    var matrix = skinned.transform.localToWorldMatrix;
                    foreach (var vertex in baked.vertices)
                        bottom = Mathf.Min(bottom, matrix.MultiplyPoint3x4(vertex).y);
                    Destroy(baked);
                }
                else bottom = Mathf.Min(bottom, r.bounds.min.y);
            }
            if (!float.IsInfinity(bottom))
            {
                actor.transform.position += Vector3.up * (FloorY + .02f - bottom);
                Debug.Log($"[AnimationTest] Death mesh bottom before alignment={bottom:F3}; root y={actor.transform.position.y:F3}");
            }
        }

        private IEnumerator AutoRun()
        {
            var folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "AnimationTestCaptures");
            Directory.CreateDirectory(folder);
            yield return new WaitForSeconds(attack.length * .34f + damage.length * .76f + death.length + .6f);
            yield return Capture(folder, "00_AttackDeath_GameView.png");
            for (int i = 1; i < labels.Length; i++)
            {
                Select(i);
                yield return new WaitForSeconds(i <= 6 ? dancePreviewSeconds : Mathf.Max(femalePoseBegin[i - 7].length, malePoseBegin[i - 7].length) + 1f);
                yield return Capture(folder, $"{i:00}_{labels[i]}_GameView.png");
            }
            Debug.Log("[AnimationTest] Auto-run complete: " + folder, this);
        }

        private static IEnumerator Capture(string folder, string filename)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, filename));
            Debug.Log("[AnimationTest] Captured " + filename);
            yield return new WaitForSeconds(.15f);
        }
    }
}
