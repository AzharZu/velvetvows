using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class Phase2AFollower : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Phase2AActorMotion motion;
        [SerializeField, Range(1f, 3f)] private float followDistance = 1.8f;
        [SerializeField, Range(.7f, 4f)] private float speed = 2.25f;
        [SerializeField, Range(.25f, 1f)] private float breadcrumbSpacing = .45f;
        private readonly Queue<Vector3> breadcrumbs = new Queue<Vector3>();
        private CharacterController character;
        private Vector3 lastBreadcrumb;
        public bool Following { get; private set; }
        public void Configure(Transform follow, Phase2AActorMotion actor) { target = follow; motion = actor; }
        private void Awake() => character = GetComponent<CharacterController>();
        public void Begin()
        {
            if (target == null) return;
            breadcrumbs.Clear(); breadcrumbs.Enqueue(target.position);
            lastBreadcrumb = target.position;
            // The player has already cleared each point on this trail. A second controller
            // catches on decorative doorway colliders, so follow the recorded path directly.
            if (character != null) character.enabled = false;
            Following = true;
        }
        public void Stop() { Following = false; breadcrumbs.Clear(); if (character != null) character.enabled = true; motion?.Idle(); }
        private void Update()
        {
            if (!Following || target == null) return;
            if (HorizontalDistance(lastBreadcrumb, target.position) > breadcrumbSpacing)
            { breadcrumbs.Enqueue(target.position); lastBreadcrumb = target.position; }
            if (Vector3.Distance(transform.position, target.position) < followDistance && breadcrumbs.Count < 5)
            { motion?.Idle(); return; }
            while (breadcrumbs.Count > 1 && HorizontalDistance(transform.position, breadcrumbs.Peek()) < .55f) breadcrumbs.Dequeue();
            var next = breadcrumbs.Peek();
            var delta = new Vector3(next.x - transform.position.x, 0, next.z - transform.position.z);
            if (delta.sqrMagnitude < .03f) { motion?.Idle(); return; }
            var direction = delta.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
            var step = Vector3.MoveTowards(transform.position,
                new Vector3(next.x, transform.position.y, next.z), speed * Time.deltaTime);
            transform.position = step;
            motion?.Walk();
        }
        private static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
