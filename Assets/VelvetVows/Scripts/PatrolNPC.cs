using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class PatrolNPC : MonoBehaviour
    {
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField, Min(.1f)] private float walkSpeed = 1.25f;
        [SerializeField] private Animator modelAnimator;
        private int next;
        private float pauseUntil;
        [SerializeField, Min(0)] private float pauseAtWaypoint = .8f;

        public void Configure(List<Transform> route, Animator animator)
        {
            waypoints = route;
            modelAnimator = animator;
        }

        public void ResetRoute() { next = 0; pauseUntil = 0; }

        private void Update()
        {
            if (waypoints.Count < 2 || modelAnimator == null) return;
            if (Time.time < pauseUntil)
            { modelAnimator.SetFloat("Speed", 0, .15f, Time.deltaTime); return; }
            Vector3 destination = waypoints[next].position;
            destination.y = transform.position.y;
            Vector3 delta = destination - transform.position;
            if (delta.sqrMagnitude < .16f)
            {
                next = (next + 1) % waypoints.Count;
                pauseUntil = Time.time + pauseAtWaypoint;
                modelAnimator.SetFloat("Speed", 0, .15f, Time.deltaTime);
                return;
            }
            float turn = Vector3.Angle(transform.forward, delta);
            float speed = walkSpeed * (turn > 40 ? .35f : 1f);
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), 5f * Time.deltaTime);
            modelAnimator.SetFloat("Speed", speed, .12f, Time.deltaTime);
        }
    }
}
