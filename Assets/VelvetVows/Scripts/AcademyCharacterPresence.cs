using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    // Small facial motion and bounded attention layered after the imported humanoid animation.
    [DefaultExecutionOrder(10100)]
    public sealed class AcademyCharacterPresence : MonoBehaviour
    {
        [SerializeField] private string characterName;
        [SerializeField] private bool attentive = true;
        private Animator model;
        private Transform head, attention;
        private DialogueManager dialogue;
        private readonly List<(SkinnedMeshRenderer mesh, int index, float original)> eyelids = new();
        private float blinkAt, blinkStart = -10, gaze;
        private Vector3 previousPosition;
        private Quaternion baseRotation, appliedRotation;
        private bool hasAppliedRotation;
        public int BlinkCount { get; private set; }
        public float GazeWeight => gaze;
        public string CharacterName => characterName;
        public void Configure(string identity, bool canLook = true) { characterName = identity; attentive = canLook; }

        private void Awake()
        {
            model = GetComponent<Animator>();
            if (model == null || !model.isHuman) { enabled = false; return; }
            head = model.GetBoneTransform(HumanBodyBones.Head);
            foreach (var mesh in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int index = mesh.sharedMesh.GetBlendShapeIndex("blink");
                if (index < 0) index = mesh.sharedMesh.GetBlendShapeIndex("Fcl_EYE_Close");
                if (index >= 0) eyelids.Add((mesh, index, mesh.GetBlendShapeWeight(index)));
            }
            previousPosition = transform.position;
            blinkAt = Time.time + Random.Range(.7f, 3.7f);
        }
        private void Update()
        {
            // Restore only our own offset before the Animator writes the next pose.
            if (hasAppliedRotation && head != null && Quaternion.Angle(head.localRotation, appliedRotation) < .01f)
                head.localRotation = baseRotation;
            hasAppliedRotation = false;
            if (dialogue != DialogueManager.Instance)
            {
                if (dialogue != null) dialogue.LineShown -= OnLine;
                dialogue = DialogueManager.Instance;
                if (dialogue != null) dialogue.LineShown += OnLine;
            }
        }
        private void OnLine(string speaker, string line)
        {
            attention = null;
            if (!attentive || string.IsNullOrEmpty(speaker) || speaker.Contains("THOUGHT") || speaker == characterName) return;
            float nearest = 64;
            foreach (var other in FindObjectsByType<AcademyCharacterPresence>(FindObjectsSortMode.None))
            {
                if (other == this || other.head == null || other.characterName != speaker) continue;
                float distance = (other.head.position - head.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; attention = other.head; }
            }
        }
        private void LateUpdate()
        {
            if (Time.time >= blinkAt)
            { blinkStart = Time.time; blinkAt = Time.time + Random.Range(3.2f, 6.2f); BlinkCount++; }
            float blink = Mathf.Clamp01(1 - Mathf.Abs((Time.time - blinkStart) / .09f - 1));
            foreach (var lid in eyelids) lid.mesh.SetBlendShapeWeight(lid.index, Mathf.Lerp(lid.original, 100, blink));
            if (head == null) return;
            float speed = Vector3.Distance(previousPosition, transform.position) / Mathf.Max(Time.deltaTime, .001f);
            previousPosition = transform.position;
            Vector3 direction = attention != null ? attention.position - head.position : Vector3.zero;
            bool canLook = attentive && attention != null && attention.gameObject.activeInHierarchy &&
                DialogueManager.IsOpen && speed < .2f && direction.sqrMagnitude < 64 &&
                Vector3.Angle(transform.forward, direction) < 65;
            gaze = Mathf.MoveTowards(gaze, canLook ? 1 : 0, Time.deltaTime * 3);
            if (gaze <= 0 || direction.sqrMagnitude < .01f) return;
            baseRotation = head.localRotation;
            var turn = Quaternion.FromToRotation(transform.forward, direction.normalized);
            turn = Quaternion.RotateTowards(Quaternion.identity, turn, 22);
            head.rotation = Quaternion.Slerp(Quaternion.identity, turn, gaze * .7f) * head.rotation;
            appliedRotation = head.localRotation; hasAppliedRotation = true;
        }
        private void OnDisable()
        {
            if (dialogue != null) dialogue.LineShown -= OnLine;
            dialogue = null; attention = null; gaze = 0;
            if (hasAppliedRotation && head != null) head.localRotation = baseRotation;
            hasAppliedRotation = false;
            foreach (var lid in eyelids) if (lid.mesh != null) lid.mesh.SetBlendShapeWeight(lid.index, lid.original);
        }
    }
}
