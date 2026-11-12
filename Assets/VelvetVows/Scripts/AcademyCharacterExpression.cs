using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    // Uses the supplied face rig; mouth motion follows the real recording, including its pauses.
    [DefaultExecutionOrder(10200)]
    [RequireComponent(typeof(AcademyCharacterPresence))]
    public sealed class AcademyCharacterExpression : MonoBehaviour
    {
        private sealed class Shape
        {
            public SkinnedMeshRenderer mesh;
            public int index;
            public float original;
            public int kind;
        }
        private readonly List<Shape> shapes = new();
        private AcademyCharacterPresence presence;
        private CharacterVoiceManager voice;
        private DialogueManager dialogue;
        private float emotion, targetEmotion, mouth;
        private int expression;
        private bool released;
        public float MouthWeight => mouth;
        public float ExpressionWeight => emotion;
        private void Awake()
        {
            presence = GetComponent<AcademyCharacterPresence>();
            foreach (var mesh in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Add(mesh, 0, "Fcl_MTH_A", "open_mouth");
                Add(mesh, 1, "Fcl_MTH_I", "open_mouth2");
                Add(mesh, 2, "Fcl_MTH_U", "o_mouth");
                Add(mesh, 3, "Fcl_MTH_E", "open_mouth.001");
                Add(mesh, 4, "Fcl_MTH_O", "o_mouth");
                Add(mesh, 10, "Fcl_BRW_Sorrow", "raise_eyebrows");
                Add(mesh, 10, "Fcl_EYE_Sorrow", "tired_face");
                Add(mesh, 11, "Fcl_BRW_Angry", "angry_face");
                Add(mesh, 11, "Fcl_EYE_Angry", "squint");
                Add(mesh, 12, "Fcl_BRW_Joy", "raise_eyebrows2");
                Add(mesh, 12, "Fcl_MTH_Fun", "slight_smile");
                Add(mesh, 13, "Fcl_BRW_Sorrow", "sad");
                Add(mesh, 13, "Fcl_MTH_Sorrow", "sad_mouth");
            }
        }
        private void Add(SkinnedMeshRenderer mesh, int kind, params string[] names)
        {
            if (mesh.sharedMesh == null) return;
            foreach (string name in names)
            {
                int i = mesh.sharedMesh.GetBlendShapeIndex(name);
                if (i < 0) continue;
                var existing = shapes.Find(s => s.mesh == mesh && s.index == i);
                if (existing != null) existing.kind |= 1 << kind;
                else shapes.Add(new Shape { mesh = mesh, index = i, original = mesh.GetBlendShapeWeight(i), kind = 1 << kind });
                return;
            }
        }
        private void Update()
        {
            if (voice == null) voice = FindFirstObjectByType<CharacterVoiceManager>();
            if (dialogue == DialogueManager.Instance) return;
            if (dialogue != null) dialogue.LineShown -= OnLine;
            dialogue = DialogueManager.Instance;
            if (dialogue != null) dialogue.LineShown += OnLine;
        }
        private void OnLine(string speaker, string line)
        {
            targetEmotion = 0;
            if (speaker == null || !speaker.StartsWith(presence.CharacterName) || speaker.Contains("THOUGHT")) return;
            string text = (line ?? "").ToLowerInvariant();
            if (text.Contains("sorry") || text.Contains("missing") || text.Contains("gone")) expression = 13;
            else if (text.Contains("afraid") || text.Contains("scared") || text.Contains("blood") || text.Contains("body") || text.Contains("what?")) expression = 10;
            else if (text.Contains("stop") || text.Contains("leave") || text.Contains("don't") || text.Contains("quiet") || text.Contains("rules")) expression = 11;
            else if (text.Contains("thank") || text.Contains("glad") || text.Contains("love") || text.Contains("beautiful")) expression = 12;
            else return;
            targetEmotion = expression == 12 ? 12 : 18;
        }
        private void LateUpdate()
        {
            // The death sequence disables presence. Its authored face must take precedence.
            if (presence == null || !presence.enabled)
            { if (!released) Restore(); released = true; return; }
            released = false;
            bool speaking = voice != null && voice.VoicePlaying && voice.ActiveSpeaker != null &&
                voice.SpeakingActor == presence;
            mouth = Mathf.MoveTowards(mouth, speaking ? voice.SpeechLevel * 48 : 0, Time.unscaledDeltaTime * 320);
            emotion = Mathf.MoveTowards(emotion, DialogueManager.IsOpen ? targetEmotion : 0, Time.unscaledDeltaTime * 55);
            foreach (var s in shapes)
            {
                float offset = (s.kind & (1 << expression)) != 0 && expression >= 10 ? emotion : 0;
                if (speaking) for (int i = 0; i < 5; i++) if ((s.kind & (1 << i)) != 0) offset += mouth * voice.VowelWeight(i);
                s.mesh.SetBlendShapeWeight(s.index, Mathf.Clamp(s.original + offset, 0, 100));
            }
        }
        private void Restore()
        {
            mouth = emotion = 0;
            foreach (var s in shapes) if (s.mesh != null) s.mesh.SetBlendShapeWeight(s.index, s.original);
        }
        private void OnDisable()
        {
            if (dialogue != null) dialogue.LineShown -= OnLine;
            dialogue = null;
            Restore();
        }
    }
}
