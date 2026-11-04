using UnityEngine;

namespace VelvetVows
{
    public sealed class Phase3BDossierInteractable : MonoBehaviour, IInteractable
    {
        public Phase3BMissingStudents director;
        public string Prompt => "Examine the missing students dossier";
        public bool CanInteract(GameManager game) => director != null && director.CanOpen(game);
        public void Interact(GameManager game) { if (CanInteract(game)) director.OpenDossier(); }
    }
}
