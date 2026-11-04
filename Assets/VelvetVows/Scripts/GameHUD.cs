using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace VelvetVows
{
    public sealed class GameHUD : MonoBehaviour
    {
        [SerializeField] private GameObject startPanel, gameplayPanel, cluePanel, victoryPanel, defeatPanel;
        [SerializeField] private Text objectiveText, suspicionText, timerText, evidenceText, roseText;
        [SerializeField] private Text interactionText, clueText, dialogueText, defeatReasonText;
        [SerializeField] private Image suspicionFill, fadeImage;
        [SerializeField] private Button beginButton, victoryRestartButton, defeatRestartButton;
        private Coroutine clueRoutine;
        private bool runtimeBeginListenerRegistered;

        private void OnEnable() => EnsureCanvasScale();
        private void LateUpdate() => EnsureCanvasScale();

        private void EnsureCanvasScale()
        {
            if (transform.localScale != Vector3.one) transform.localScale = Vector3.one;
        }

        public float FadeAlpha => fadeImage.color.a;

        public void Configure(GameObject start, GameObject gameplay, GameObject clue, GameObject victory, GameObject defeat,
            Text objective, Text suspicion, Text timer, Text evidence, Text roses, Text interaction,
            Text clueBody, Text dialogue, Text defeatReason, Image fill, Image fade,
            Button begin, Button victoryRestart, Button defeatRestart)
        {
            startPanel = start; gameplayPanel = gameplay; cluePanel = clue; victoryPanel = victory; defeatPanel = defeat;
            objectiveText = objective; suspicionText = suspicion; timerText = timer; evidenceText = evidence;
            roseText = roses; interactionText = interaction; clueText = clueBody; dialogueText = dialogue;
            defeatReasonText = defeatReason; suspicionFill = fill; fadeImage = fade;
            beginButton = begin; victoryRestartButton = victoryRestart; defeatRestartButton = defeatRestart;
        }

        private void Awake()
        {
            EnsureCanvasScale();
            victoryRestartButton.onClick.AddListener(() => GameManager.Instance.Restart());
            defeatRestartButton.onClick.AddListener(() => GameManager.Instance.Restart());
            EnforceIntroductionUI();
            Debug.Log("[VelvetVows] GameHUD initialized", this);
            LogStartScreenState();
        }

        public void EnforceIntroductionUI()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = true;
            gameObject.SetActive(true);
            EnsureCanvasScale();
            startPanel.SetActive(true);
            startPanel.transform.localScale = Vector3.one;
            var group = startPanel.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = 1; group.interactable = true; group.blocksRaycasts = true; }
            gameplayPanel.SetActive(false);
            cluePanel.SetActive(false);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            dialogueText.gameObject.SetActive(false);
            SetFade(0);
        }

        public void EnsureBeginListener(GameManager game)
        {
            if (runtimeBeginListenerRegistered) return;
            for (int i = 0; i < beginButton.onClick.GetPersistentEventCount(); i++)
            {
                if (beginButton.onClick.GetPersistentTarget(i) == game &&
                    beginButton.onClick.GetPersistentMethodName(i) == nameof(GameManager.Begin) &&
                    beginButton.onClick.GetPersistentListenerState(i) != UnityEngine.Events.UnityEventCallState.Off)
                {
                    Debug.Log("[VelvetVows] BEGIN listener registered (serialized)", beginButton);
                    return;
                }
            }
            beginButton.onClick.AddListener(game.Begin);
            runtimeBeginListenerRegistered = true;
            Debug.Log("[VelvetVows] BEGIN listener registered (runtime)", beginButton);
        }

        public void LogStartScreenState()
        {
            var canvas = GetComponent<Canvas>();
            Debug.Log("[VelvetVows] Canvas active: " + gameObject.activeInHierarchy +
                ", enabled: " + (canvas != null && canvas.enabled) +
                ", mode: " + (canvas != null ? canvas.renderMode.ToString() : "missing"), this);
            Debug.Log("[VelvetVows] Canvas scale: " + transform.localScale, this);
            Debug.Log("[VelvetVows] StartPanel active: " + startPanel.activeInHierarchy, startPanel);
            Debug.Log("[VelvetVows] StartPanel scale: " + startPanel.transform.localScale, startPanel);
            var eventSystem = EventSystem.current;
            var module = eventSystem != null ? eventSystem.GetComponent<InputSystemUIInputModule>() : null;
            Debug.Log("[VelvetVows] UI input: EventSystem=" + (eventSystem != null) +
                ", InputSystemUIInputModule=" + (module != null && module.enabled) +
                ", actions=" + (module != null && module.actionsAsset != null) +
                ", cursor=" + Cursor.lockState + "/visible=" + Cursor.visible, this);
        }

        public void ShowStart(bool show) => startPanel.SetActive(show);
        public void ShowGameplay(bool show) => gameplayPanel.SetActive(show);
        public void SetObjective(string value) => objectiveText.text = value;
        public void SetInteraction(string value) => interactionText.text = value;
        public void SetEvidence(int found, int total) => evidenceText.text = "EVIDENCE: " + found + "/" + total;
        public void SetReputation(int value, int threshold) => evidenceText.text = "REPUTATION  " + value + " / " + threshold;
        public void SetTimerVisible(bool visible) { if (timerText != null) timerText.gameObject.SetActive(visible); }
        public void SetRoses(int found, int total) => roseText.text = "ROSES: " + found + "/" + total;
        public void SetSuspicion(float value)
        {
            suspicionText.text = "SUSPICION " + Mathf.CeilToInt(value) + "%";
            suspicionFill.fillAmount = Mathf.Clamp01(value / 100f);
        }
        public void SetTimer(float remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            timerText.text = "BALL BEGINS IN: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        public void SetCleanupTimer(float remaining)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
            timerText.text = "CLEANUP  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        public void SetFade(float alpha)
        {
            var color = fadeImage.color;
            color.a = Mathf.Clamp01(alpha);
            fadeImage.color = color;
            fadeImage.raycastTarget = alpha > .9f;
        }
        public void ShowClue(string clue)
        {
            if (clueRoutine != null) StopCoroutine(clueRoutine);
            clueText.text = clue;
            cluePanel.SetActive(true);
            clueRoutine = StartCoroutine(HideClueLater());
        }
        public void HideClue()
        {
            if (clueRoutine != null) StopCoroutine(clueRoutine);
            clueRoutine = null;
            cluePanel.SetActive(false);
        }
        private IEnumerator HideClueLater()
        {
            yield return new WaitForSeconds(4.5f);
            cluePanel.SetActive(false);
            clueRoutine = null;
        }
        public void ShowDialogue(string line)
        {
            dialogueText.text = line;
            dialogueText.gameObject.SetActive(true);
        }
        public void HideDialogue() => dialogueText.gameObject.SetActive(false);
        public void ShowVictory()
        {
            gameplayPanel.SetActive(false);
            victoryPanel.SetActive(true);
            SetFade(0);
        }
        public void ShowDefeat(string reason)
        {
            gameplayPanel.SetActive(false);
            defeatReasonText.text = reason;
            defeatPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
