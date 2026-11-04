using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class CelesteSequence : MonoBehaviour, IInteractable
    {
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField, Min(.1f)] private float walkSpeed = 1.5f;
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private Transform visualRoot;
        private int waypointIndex;
        private bool walking;
        public bool Arrived { get; private set; }
        public string Prompt => "Speak to Celeste";

        public void Configure(List<Transform> route, Animator animator, Transform visual)
        {
            waypoints = route;
            modelAnimator = animator;
            visualRoot = visual;
        }

        public bool CanInteract(GameManager game) => game.State == GameState.FindCeleste && !walking;
        public void Interact(GameManager game)
        {
            var dialogue = GetComponent<DialogueTrigger>();
            if (dialogue != null && DialogueManager.Instance != null && dialogue.CanInteract(game))
                dialogue.Interact(game);
            else game.MetCeleste();
        }
        public void BeginRoute() { waypointIndex = 0; walking = true; Arrived = false; }

        private void Update()
        {
            if (!walking || waypointIndex >= waypoints.Count) return;
            Vector3 destination = waypoints[waypointIndex].position;
            destination.y = transform.position.y;
            Vector3 travel = destination - transform.position;
            if (travel.sqrMagnitude < .1f)
            {
                waypointIndex++;
                if (waypointIndex >= waypoints.Count)
                {
                    walking = false;
                    Arrived = true;
                    modelAnimator.SetFloat("Speed", 0);
                }
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, destination, walkSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(travel.normalized),
                7f * Time.deltaTime);
            modelAnimator.SetFloat("Speed", walkSpeed);
        }

        public void StageAfterEvent()
        {
            walking = false;
            modelAnimator.SetFloat("Speed", 0);
            modelAnimator.enabled = false;
            visualRoot.localRotation = Quaternion.Euler(0, 0, 90);
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }
    }
}
