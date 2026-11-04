using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class AmbientNPC : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField, Min(.1f)] private float walkSpeed = 1.1f;
        [SerializeField] private bool walkAfterCeleste;
        private int next;

        public void Configure(Animator actor, List<Transform> route, bool afterCeleste = false)
        {
            animator = actor; waypoints = route; walkAfterCeleste = afterCeleste;
        }

        private void Update()
        {
            bool active = waypoints.Count > 1 && (!walkAfterCeleste ||
                (GameManager.Instance != null && GameManager.Instance.State >= GameState.EscapeStudio));
            if (!active) { if (animator != null) animator.SetFloat("Speed", 0); return; }
            Vector3 target = waypoints[next].position;
            target.y = transform.position.y;
            Vector3 delta = target - transform.position;
            if (delta.sqrMagnitude < .16f) { next = (next + 1) % waypoints.Count; return; }
            transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(delta.normalized), 5f * Time.deltaTime);
            if (animator != null) animator.SetFloat("Speed", walkSpeed);
        }
    }
}
