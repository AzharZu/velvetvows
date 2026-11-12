using UnityEngine;

namespace VelvetVows
{
    public enum Phase2BAction { Body, Cloth, Blood, Disposal, Clothing, Door }

    [RequireComponent(typeof(Collider))]
    public sealed class Phase2BInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Phase2BCleanupManager cleanup;
        [SerializeField] private Phase2BAction action;
        public Phase2BAction Action => action;
        public void Configure(Phase2BCleanupManager owner, Phase2BAction kind) { cleanup = owner; action = kind; }
        public string Prompt => cleanup != null ? cleanup.Prompt(action) : "Interact";
        public bool CanInteract(GameManager game) => cleanup != null && cleanup.CanInteract(action);
        public void Interact(GameManager game) { if (CanInteract(game)) cleanup.Interact(action); }
    }
}
