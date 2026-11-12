using StarterAssets;
using UnityEngine;

namespace VelvetVows
{
    // Keeps the imported humanoid Animator on the visible model while Starter Assets
    // owns movement on the invisible CharacterController root.
    [RequireComponent(typeof(CharacterController), typeof(ThirdPersonController))]
    public sealed class AcademyCharacterAnimation : MonoBehaviour
    {
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private ThirdPersonController movement;
        [SerializeField] private CharacterController character;
        private bool cinematicIdle;
        private float actionSpeed;
        public void SetActionSpeed(float speed) => actionSpeed = speed;

        public void Configure(Animator visualAnimator, ThirdPersonController controller, CharacterController capsule)
        {
            modelAnimator = visualAnimator;
            movement = controller;
            character = capsule;
        }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<ThirdPersonController>();
            if (character == null) character = GetComponent<CharacterController>();
            if (modelAnimator == null || movement == null || character == null)
            {
                Debug.LogError("Evelyn animation references are incomplete.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (cinematicIdle)
            {
                modelAnimator.SetFloat("Speed", actionSpeed, .12f, Time.deltaTime);
                modelAnimator.SetBool("Grounded", true);
                modelAnimator.SetBool("Jump", false);
                modelAnimator.SetBool("FreeFall", false);
                return;
            }
            Vector3 velocity = character.velocity;
            float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            bool grounded = movement.Grounded;
            modelAnimator.SetFloat("Speed", horizontalSpeed, 0.12f, Time.deltaTime);
            modelAnimator.SetBool("Grounded", grounded);
            modelAnimator.SetBool("Jump", !grounded && velocity.y > 0.1f);
            modelAnimator.SetBool("FreeFall", !grounded && velocity.y <= 0.1f);
        }

        public void EnterCinematicIdle()
        {
            cinematicIdle = true;
            modelAnimator.SetFloat("Speed", 0);
            modelAnimator.SetBool("Grounded", true);
            modelAnimator.SetBool("Jump", false);
            modelAnimator.SetBool("FreeFall", false);
            modelAnimator.CrossFade("Idle / Walk / Run", .12f);
        }

        public void ExitCinematicIdle()
        {
            cinematicIdle = false;
            actionSpeed = 0;
            modelAnimator.SetFloat("Speed", 0);
        }
    }
}
