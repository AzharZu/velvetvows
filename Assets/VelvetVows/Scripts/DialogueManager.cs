using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using System;

namespace VelvetVows
{
    public sealed class DialogueManager : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Text[] choiceLabels;
        [SerializeField] private CharacterVoiceManager voice;
        private DialogueTrigger trigger;
        private DialogueLine[] lines;
        private DialogueChoice[] choices;
        private int next;
        private bool showingReply;
        private bool choiceReplyShown;
        private string pendingChoiceReply;
        private Action scriptedClosed;
        private Action<int> scriptedLineShown;
        private bool cinematic;
        private int lastButtonAdvanceFrame = -1;
        private int lastKeyboardAdvanceFrame = -1;
        private bool cinematicAdvanceable;
        private bool cinematicAdvanceRequested;
        private int openedFrame = -1;

        public event Action<string, string> LineShown;

        public static DialogueManager Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;
        public GameObject PanelRoot => panel;

        public void Configure(GameObject root, Text speaker, Text body, Button nextButton,
            Button[] options, Text[] labels)
        { panel = root; speakerText = speaker; bodyText = body; continueButton = nextButton; choiceButtons = options; choiceLabels = labels; }

        private void Awake()
        {
            Instance = this;
            panel.SetActive(false);
            continueButton.onClick.AddListener(Continue);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int index = i;
                choiceButtons[i].onClick.AddListener(() => Choose(index));
            }
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (!IsOpen || Keyboard.current == null) return;
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.eKey.wasPressedThisFrame)
                ContinueFromKeyboard();
        }

        public void BeginScripted(DialogueLine[] script, Action onClosed, Action<int> onLineShown = null)
        {
            if (IsOpen || script == null || script.Length == 0) return;
            trigger = null;
            scriptedClosed = onClosed;
            scriptedLineShown = onLineShown;
            cinematic = false;
            lines = script;
            choices = new DialogueChoice[0];
            next = 0; showingReply = false; choiceReplyShown = false; pendingChoiceReply = null;
            openedFrame = Time.frameCount;
            lastButtonAdvanceFrame = lastKeyboardAdvanceFrame = -1;
            panel.SetActive(true);
            EnsureUiInput();
            GameManager.Instance?.SetDialogueControl(true);
            ShowNext();
        }

        public void ShowCinematicLine(string speaker, string line) => ShowCinematicLineInternal(speaker, line, false);

        public void ShowAdvanceableCinematicLine(string speaker, string line) =>
            ShowCinematicLineInternal(speaker, line, true);

        private void ShowCinematicLineInternal(string speaker, string line, bool allowAdvance)
        {
            cinematic = true;
            cinematicAdvanceable = allowAdvance;
            cinematicAdvanceRequested = false;
            if (!panel.activeSelf) openedFrame = Time.frameCount;
            panel.SetActive(true);
            EnsureUiInput();
            speakerText.text = speaker;
            bodyText.text = line;
            voice?.PlayDialogue(speaker, line);
            LineShown?.Invoke(speaker, line);
            continueButton.gameObject.SetActive(allowAdvance);
            foreach (var button in choiceButtons) button.gameObject.SetActive(false);
        }

        public void HideCinematicLine()
        {
            if (!cinematic) return;
            cinematic = false;
            cinematicAdvanceable = cinematicAdvanceRequested = false;
            panel.SetActive(false);
            voice?.StopVoice();
        }

        public void Begin(DialogueTrigger source, DialogueLine[] script, DialogueChoice[] options)
        {
            if (IsOpen || script == null || script.Length == 0) return;
            trigger = source; scriptedClosed = null; cinematic = false;
            lines = script; choices = options ?? new DialogueChoice[0];
            next = 0; showingReply = false; choiceReplyShown = false; pendingChoiceReply = null;
            openedFrame = Time.frameCount;
            lastButtonAdvanceFrame = lastKeyboardAdvanceFrame = -1;
            panel.SetActive(true);
            EnsureUiInput();
            GameManager.Instance.SetDialogueControl(true);
            ShowNext();
        }

        private void Continue()
        {
            if (openedFrame == Time.frameCount) return;
            // Distinct pointer clicks remain valid; suppress only a keyboard/UI duplicate.
            if (lastKeyboardAdvanceFrame == Time.frameCount) return;
            lastButtonAdvanceFrame = Time.frameCount;
            Advance();
        }

        private void ContinueFromKeyboard()
        {
            if (openedFrame == Time.frameCount) return;
            if (lastButtonAdvanceFrame == Time.frameCount || lastKeyboardAdvanceFrame == Time.frameCount) return;
            lastKeyboardAdvanceFrame = Time.frameCount;
            Advance();
        }

        private void Advance()
        {
            if (cinematic)
            {
                if (cinematicAdvanceable) cinematicAdvanceRequested = true;
                return;
            }
            if (showingReply)
            {
                if (!choiceReplyShown && !string.IsNullOrEmpty(pendingChoiceReply))
                {
                    choiceReplyShown = true;
                    int divider = pendingChoiceReply.IndexOf(':');
                    string who = divider > 0 ? pendingChoiceReply.Substring(0, divider).Trim() : "";
                    string words = divider > 0 ? pendingChoiceReply.Substring(divider + 1).Trim() : pendingChoiceReply;
                    if (string.IsNullOrEmpty(who)) who = "NPC";
                    speakerText.text = who;
                    bodyText.text = words;
                    voice?.PlayDialogue(who, words);
                    LineShown?.Invoke(who, words);
                    return;
                }
                Close(); return;
            }
            if (next < lines.Length) { ShowNext(); return; }
            if (choices.Length == 0) Close();
        }

        public System.Collections.IEnumerator WaitForCinematicAdvance(float seconds)
        {
            float elapsed = 0;
            while (!cinematicAdvanceRequested && elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            cinematicAdvanceable = false;
            cinematicAdvanceRequested = false;
            continueButton.gameObject.SetActive(false);
        }

        public static void EnsureUiInput()
        {
            var events = EventSystem.current;
            if (events == null) return;
            if (!events.enabled) events.enabled = true;
            var module = events.GetComponent<InputSystemUIInputModule>();
            if (module != null)
            {
                if (!module.enabled) module.enabled = true;
                module.actionsAsset?.Enable();
            }
        }

        private void ShowNext()
        {
            var line = lines[next++];
            speakerText.text = line.speaker;
            bodyText.text = line.text;
            voice?.PlayDialogue(line.speaker, line.text);
            LineShown?.Invoke(line.speaker, line.text);
            if (trigger == null) scriptedLineShown?.Invoke(next - 1);
            bool atChoice = next >= lines.Length && choices.Length > 0;
            continueButton.gameObject.SetActive(!atChoice);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool visible = atChoice && i < choices.Length;
                choiceButtons[i].gameObject.SetActive(visible);
                if (visible) choiceLabels[i].text = choices[i].response;
            }
        }

        private void Choose(int index)
        {
            if (index >= choices.Length) return;
            var choice = choices[index];
            GameManager.Instance.AdjustSuspicion(choice.suspicionChange);
            speakerText.text = "EVELYN";
            bodyText.text = choice.response;
            voice?.PlayDialogue("EVELYN", choice.response);
            LineShown?.Invoke("EVELYN", choice.response);
            showingReply = true;
            choiceReplyShown = false;
            pendingChoiceReply = choice.reply;
            foreach (var button in choiceButtons) button.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(true);
        }

        private void Close()
        {
            panel.SetActive(false);
            voice?.StopVoice();
            GameManager.Instance.SetDialogueControl(false);
            if (trigger != null && trigger.StartCelesteRoute) GameManager.Instance.MetCeleste();
            trigger = null;
            var callback = scriptedClosed;
            scriptedClosed = null;
            scriptedLineShown = null;
            callback?.Invoke();
        }
    }
}
