using System.Collections;
using UnityEngine;

namespace VelvetVows
{
    public sealed class Phase2AStudioDoor : MonoBehaviour
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private Collider solidCollider;
        [SerializeField, Range(.25f, 3f)] private float closeSeconds = 1.1f;
        [SerializeField] private float openYaw = 95f;
        public bool Closed { get; private set; }
        public void Configure(Transform doorHinge, Collider collision) { hinge = doorHinge; solidCollider = collision; }
        private void Awake()
        {
            if (solidCollider != null) solidCollider.enabled = false;
            // The visible door is two metres wide inside a six metre arch opening.
            // Fill only the unused sides and upper gap; retain the passage at z -4..-2.
            var stone = GameObject.Find("Art Studio arch")?.GetComponentInChildren<Renderer>()?.sharedMaterial;
            FramePiece("Studio door left jamb", new Vector3(-12.05f, 3f, -4.5f), new Vector3(.38f, 6f, 1f), stone);
            FramePiece("Studio door right jamb", new Vector3(-12.05f, 3f, -.5f), new Vector3(.38f, 6f, 3f), stone);
            FramePiece("Studio door lintel", new Vector3(-12.05f, 4.5f, -3f), new Vector3(.38f, 3f, 2f), stone);
        }
        private void FramePiece(string name, Vector3 position, Vector3 size, Material stone)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name; piece.transform.SetParent(transform, true);
            piece.transform.SetPositionAndRotation(position, Quaternion.identity);
            piece.transform.localScale = size;
            if (stone != null) piece.GetComponent<Renderer>().sharedMaterial = stone;
        }
        public IEnumerator CloseVisible()
        {
            if (hinge == null) yield break;
            var start = hinge.localRotation;
            var end = Quaternion.identity;
            for (float t = 0; t < closeSeconds; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(start, end, t / closeSeconds);
                yield return null;
            }
            hinge.localRotation = end;
            if (solidCollider != null) solidCollider.enabled = true;
            Closed = true;
        }
        public IEnumerator OpenVisible()
        {
            if (hinge == null) yield break;
            if (solidCollider != null) solidCollider.enabled = false;
            var start = hinge.localRotation;
            var end = Quaternion.Euler(0, openYaw, 0);
            for (float t = 0; t < closeSeconds; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(start, end, t / closeSeconds);
                yield return null;
            }
            hinge.localRotation = end;
            Closed = false;
        }
    }
}
