using System.Collections;
using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField, Min(.1f)] private float slideDistance = 3.5f;
        [SerializeField, Min(.1f)] private float openSeconds = 1.2f;
        private bool open;
        public void Configure(Transform doorVisual) => visual = doorVisual;
        public void Open()
        {
            if (open) return;
            open = true;
            GetComponent<BoxCollider>().enabled = false;
            if (visual != null) StartCoroutine(Slide());
        }
        private IEnumerator Slide()
        {
            Vector3 start = visual.localPosition;
            Vector3 end = start + Vector3.forward * slideDistance;
            float elapsed = 0;
            while (elapsed < openSeconds)
            {
                elapsed += Time.deltaTime;
                visual.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / openSeconds));
                yield return null;
            }
            visual.localPosition = end;
        }
    }
}
