using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(Collider))]
    public sealed class InspectableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string objectTitle;
        [SerializeField] private string prompt = "Examine";
        [SerializeField, TextArea(2, 8)] private string interactionText;
        [SerializeField, TextArea(1, 4)] private string evelynReaction;
        [SerializeField] private bool beforeCeleste = true;
        [SerializeField] private bool afterCeleste = true;
        [SerializeField] private bool oneTime;
        private bool used;

        public string Prompt => prompt + " " + objectTitle;

        public void Configure(string title, string action, string text, string reaction,
            bool before = true, bool after = true, bool once = false)
        {
            objectTitle = title; prompt = action; interactionText = text;
            evelynReaction = reaction; beforeCeleste = before; afterCeleste = after; oneTime = once;
        }

        public bool CanInteract(GameManager game)
        {
            if (used && oneTime) return false;
            bool after = game.State == GameState.EscapeStudio || game.State == GameState.InvestigateGallery ||
                         game.State == GameState.ReachBallroom;
            return after ? afterCeleste : beforeCeleste &&
                (game.State == GameState.FindCeleste || game.State == GameState.FollowCeleste);
        }

        public void Interact(GameManager game)
        {
            if (!CanInteract(game)) return;
            used = true;
            string text = interactionText;
            if (!string.IsNullOrEmpty(evelynReaction))
                text += "\n\nEvelyn: “" + evelynReaction + "”";
            game.ShowClue(text);
        }
    }
}
