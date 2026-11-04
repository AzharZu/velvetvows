using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(Collider))]
    public sealed class EvidenceItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private EvidenceManager manager;
        [SerializeField] private string itemName = "Inspect evidence";
        [SerializeField, TextArea(2, 5)] private string clue;
        public string Clue => clue;
        public string Prompt => itemName;
        public void Configure(EvidenceManager owner, string label, string text)
        {
            manager = owner; itemName = label; clue = text;
        }
        public bool CanInteract(GameManager game) => game.State == GameState.InvestigateGallery || game.State == GameState.EscapeStudio;
        public void Interact(GameManager game) => manager.Collect(this);
    }
}
