using System;
using UnityEngine;

namespace VelvetVows
{
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        public DialogueLine(string who, string says) { speaker = who; text = says; }
    }

    [Serializable]
    public struct DialogueChoice
    {
        public string response;
        [TextArea(1, 3)] public string reply;
        public float suspicionChange;
        public DialogueChoice(string text, string answer, float change = 0)
        { response = text; reply = answer; suspicionChange = change; }
    }

    [RequireComponent(typeof(Collider))]
    public sealed class DialogueTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Speak";
        [SerializeField] private DialogueLine[] beforeLines = Array.Empty<DialogueLine>();
        [SerializeField] private DialogueLine[] afterLines = Array.Empty<DialogueLine>();
        [SerializeField] private DialogueChoice[] beforeChoices = Array.Empty<DialogueChoice>();
        [SerializeField] private DialogueChoice[] afterChoices = Array.Empty<DialogueChoice>();
        [SerializeField] private bool beforeCeleste = true;
        [SerializeField] private bool afterCeleste = true;
        [SerializeField] private bool oneTimeBefore;
        [SerializeField] private bool oneTimeAfter;
        [SerializeField] private bool startCelesteRoute;
        private bool usedBefore;
        private bool usedAfter;

        public string Prompt => prompt;
        public bool StartCelesteRoute => startCelesteRoute;

        public void Configure(string label, DialogueLine[] before, DialogueChoice[] initialChoices,
            DialogueLine[] after = null, DialogueChoice[] laterChoices = null,
            bool beforeEnabled = true, bool afterEnabled = true,
            bool onceBefore = false, bool onceAfter = false, bool route = false)
        {
            prompt = label;
            beforeLines = before ?? Array.Empty<DialogueLine>();
            beforeChoices = initialChoices ?? Array.Empty<DialogueChoice>();
            afterLines = after ?? Array.Empty<DialogueLine>();
            afterChoices = laterChoices ?? Array.Empty<DialogueChoice>();
            beforeCeleste = beforeEnabled; afterCeleste = afterEnabled;
            oneTimeBefore = onceBefore; oneTimeAfter = onceAfter; startCelesteRoute = route;
        }

        private static bool IsAfter(GameState state) => state == GameState.EscapeStudio ||
            state == GameState.InvestigateGallery || state == GameState.ReachBallroom;

        public bool CanInteract(GameManager game)
        {
            if (DialogueManager.IsOpen) return false;
            if (IsAfter(game.State)) return afterCeleste && (!usedAfter || !oneTimeAfter) && afterLines.Length > 0;
            return (game.State == GameState.FindCeleste || game.State == GameState.FollowCeleste) &&
                beforeCeleste && (!usedBefore || !oneTimeBefore) && beforeLines.Length > 0;
        }

        public void Interact(GameManager game)
        {
            if (!CanInteract(game)) return;
            bool after = IsAfter(game.State);
            if (after) usedAfter = true; else usedBefore = true;
            DialogueManager.Instance?.Begin(this, after ? afterLines : beforeLines,
                after ? afterChoices : beforeChoices);
        }
    }
}
