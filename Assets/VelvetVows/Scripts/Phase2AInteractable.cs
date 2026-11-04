using UnityEngine;

namespace VelvetVows
{
    public enum Phase2AAction
    {
        CelesteTalk, BooksPickup, BooksDelivery, ArtPickup, ArtDelivery,
        LostPickup, LostDelivery, RibbonPickup, RibbonReturn, Sketchbook,
        Witness, StudioDoor, Confront
    }

    [RequireComponent(typeof(Collider))]
    public sealed class Phase2AInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Phase2AStoryManager story;
        [SerializeField] private Phase2AAction action;
        public Phase2AAction Action => action;
        public string Prompt => story != null ? story.Prompt(action) : "Interact";
        public void Configure(Phase2AStoryManager owner, Phase2AAction kind) { story = owner; action = kind; }
        public bool CanInteract(GameManager game) => story != null && story.CanInteract(action);
        public void Interact(GameManager game) { if (CanInteract(game)) story.Interact(action); }
    }
}
