using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VelvetVows
{
    // Reads the same PlayerInput action instance used by the Starter Assets controller.
    // Explicit polling avoids relying on a prefab's notification/event wiring.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerInput), typeof(StarterAssetsInputs), typeof(ThirdPersonController))]
    public sealed class AcademyPlayerInputRelay : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private StarterAssetsInputs inputs;
        [SerializeField, Range(1f, 3f)] private float mouseLookMultiplier = 1.65f;

        private InputAction move;
        private InputAction look;
        private InputAction sprint;
        private InputAction jump;

        private void Awake()
        {
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (inputs == null) inputs = GetComponent<StarterAssetsInputs>();
            var actions = playerInput.actions;
            move = actions?.FindAction("Player/Move", false);
            look = actions?.FindAction("Player/Look", false);
            sprint = actions?.FindAction("Player/Sprint", false);
            jump = actions?.FindAction("Player/Jump", false);
            if (move == null || look == null || sprint == null || jump == null)
            {
                Debug.LogError("Academy player is missing Player/Move, Look, Sprint or Jump actions.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            inputs.MoveInput(move.ReadValue<Vector2>());
            var lookDelta = look.ReadValue<Vector2>();
            if (playerInput.currentControlScheme == "KeyboardMouse") lookDelta *= mouseLookMultiplier;
            inputs.LookInput(lookDelta);
            inputs.SprintInput(sprint.IsPressed());
            inputs.JumpInput(jump.WasPressedThisFrame());
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (inputs == null) return;
            inputs.MoveInput(Vector2.zero);
            inputs.LookInput(Vector2.zero);
            inputs.SprintInput(false);
            inputs.JumpInput(false);
        }

        public void Configure(PlayerInput source, StarterAssetsInputs destination)
        {
            playerInput = source;
            inputs = destination;
        }

        public bool HasReferences => playerInput != null && inputs != null;
    }
}
