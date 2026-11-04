using UnityEngine;
using UnityEngine.InputSystem;

namespace VelvetVows
{
    public interface IInteractable
    {
        bool CanInteract(GameManager game);
        string Prompt { get; }
        void Interact(GameManager game);
    }

    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Min(.5f)] public float interactionDistance = 2.2f;
        [SerializeField] private LayerMask interactionLayers = 1;
        private readonly Collider[] nearby = new Collider[64];
        private bool interactWasHeld;

        private void Update()
        {
            bool interactHeld = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            bool interactPressed = interactHeld && !interactWasHeld;
            interactWasHeld = interactHeld;
            var game = GameManager.Instance;
            if (DialogueManager.IsOpen) { game?.SetInteraction(""); return; }
            if (game == null || game.State == GameState.Introduction || game.State == GameState.MurderEvent ||
                game.State == GameState.Victory || game.State == GameState.Defeat ||
                game.State == GameState.Phase2BCinematic || game.State == GameState.Phase2BOutcome ||
                game.State == GameState.Phase3BDossier) return;
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up,
                interactionDistance, nearby, interactionLayers, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float closest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var collider = nearby[i];
                if (collider == null || collider.transform.IsChildOf(transform)) continue;
                foreach (var behaviour in collider.GetComponentsInParent<MonoBehaviour>())
                {
                    if (!(behaviour is IInteractable candidate) || !candidate.CanInteract(game)) continue;
                    float distance = (collider.transform.position - transform.position).sqrMagnitude;
                    if (distance < closest) { closest = distance; best = candidate; }
                }
            }
            game.SetInteraction(best == null ? "" : "[E] " + best.Prompt);
            if (best != null && interactPressed)
                best.Interact(game);
        }
    }
}
