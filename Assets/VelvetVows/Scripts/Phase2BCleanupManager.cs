using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace VelvetVows
{
    public sealed class Phase2BCleanupManager : MonoBehaviour
    {
        public GameManager game;
        public GameHUD hud;
        public DialogueManager dialogue;
        public Phase2BMurderSequence murder;
        public Phase2BBloodEvidence blood;
        public Phase2BPrefectInvestigation prefect;
        public Phase2AStudioDoor studioDoor;
        public Transform player, celeste, wardrobe, wardrobeLeftDoor, wardrobeRightDoor;
        public GameObject worldCloth, heldCloth, clothStain, attackProp, clothingMarks;
        public Text checklist, outcomeTitle, outcomeSubtitle;
        public GameObject checklistPanel, outcomePanel;
        public Button retryButton;
        public Phase2CBallDirector ball;
        public Phase3BMissingStudents missingStudents;
        public CharacterVoiceManager voice;
        [Min(30)] public float challengeSeconds = 120;
        public float Remaining { get; private set; }
        public bool Active { get; private set; }
        public bool BodyHidden { get; private set; }
        public bool BloodCleaned => blood != null && blood.Cleaned;
        public bool EvidenceDisposed { get; private set; }
        public bool ClothingCleaned { get; private set; }
        public bool HasCloth { get; private set; }
        public bool StainedCloth { get; private set; }
        public bool IsCleaningBlood { get; private set; }
        public bool ChoiceMade { get; private set; }
        public bool BodyLeftBehind { get; private set; }
        private bool busy;
        private GameObject choicePanel;
        private JapaneseSegmentPlayer evelynSegments;
        private bool emotionalNarrationActive;
        private Camera gameCamera;
        private CinemachineBrain brain;
        private Vector3 playerStart;
        private Quaternion playerStartRotation;
        private Quaternion leftClosed, rightClosed;
        private bool warned;
        private void Awake()
        {
            gameCamera = Camera.main;
            if (gameCamera != null) brain = gameCamera.GetComponent<CinemachineBrain>();
            if (wardrobeLeftDoor != null) leftClosed = wardrobeLeftDoor.localRotation;
            if (wardrobeRightDoor != null) rightClosed = wardrobeRightDoor.localRotation;
            if (checklistPanel != null) checklistPanel.SetActive(false);
            if (outcomePanel != null) outcomePanel.SetActive(false);
            if (heldCloth != null) heldCloth.SetActive(false);
            if (clothStain != null) clothStain.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            evelynSegments = gameObject.AddComponent<JapaneseSegmentPlayer>();
            BuildBodyChoice();
        }
        public void BeginChallenge()
        {
            playerStart = player.position; playerStartRotation = player.rotation;
            Remaining = challengeSeconds;
            Active = true; busy = false; warned = false;
            ChoiceMade = BodyLeftBehind = IsCleaningBlood = false;
            if (choicePanel != null) choicePanel.SetActive(false);
            ball?.SetBodyFate(false);
            BodyHidden = false; EvidenceDisposed = false; ClothingCleaned = false;
            HasCloth = false; StainedCloth = false;
            checklistPanel.SetActive(true); outcomePanel.SetActive(false);
            worldCloth.SetActive(true); heldCloth.SetActive(false); clothStain.SetActive(false);
            blood.ResetEvidence();
            RefreshUI();
            game.BeginPhase2BCleanup();
            hud.SetCleanupTimer(Remaining);
            prefect.BeginInvestigation();
            Debug.Log("[Phase2B] Cleanup challenge began at 120 seconds", this);
        }
        private void Update()
        {
            if (!Active) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            hud.SetCleanupTimer(Remaining);
            prefect?.Tick(challengeSeconds - Remaining);
            if (!warned && Remaining <= 30f) { warned = true; RefreshUI(); }
            if (Remaining <= 0) Fail("Time ran out before the Studio was clean.");
        }
        public bool CanInteract(Phase2BAction action)
        {
            if (!Active || busy || game == null || game.State != GameState.Phase2BCleanup || DialogueManager.IsOpen) return false;
            switch (action)
            {
                case Phase2BAction.Body: return !BodyHidden;
                case Phase2BAction.Cloth: return !HasCloth && !EvidenceDisposed;
                case Phase2BAction.Blood: return HasCloth && !BloodCleaned;
                case Phase2BAction.Disposal: return StainedCloth && !EvidenceDisposed && attackProp.activeSelf;
                case Phase2BAction.Clothing: return !ClothingCleaned && clothingMarks.activeSelf;
                case Phase2BAction.Door: return studioDoor.Closed;
                default: return false;
            }
        }
        public string Prompt(Phase2BAction action)
        {
            switch (action)
            {
                case Phase2BAction.Body: return "Hide Celeste in the wardrobe";
                case Phase2BAction.Cloth: return "Take a cleaning cloth";
                case Phase2BAction.Blood: return "Clean the blood";
                case Phase2BAction.Disposal: return "Dispose of blade and stained cloth";
                case Phase2BAction.Clothing: return "WASH CLOTHES";
                case Phase2BAction.Door: return "Open the Studio door";
                default: return "Interact";
            }
        }
        public void Interact(Phase2BAction action)
        {
            if (!CanInteract(action)) return;
            switch (action)
            {
                case Phase2BAction.Body: StartCoroutine(HideBody()); break;
                case Phase2BAction.Cloth:
                    HasCloth = true; worldCloth.SetActive(false); heldCloth.SetActive(true);
                    hud.ShowClue("A cloth from the Studio storage. The blood is still visible."); break;
                case Phase2BAction.Blood: StartCoroutine(CleanBlood()); break;
                case Phase2BAction.Disposal:
                    attackProp.SetActive(false); heldCloth.SetActive(false);
                    EvidenceDisposed = true; RefreshUI(); CheckSuccess(); break;
                case Phase2BAction.Clothing: StartCoroutine(CleanClothing()); break;
                case Phase2BAction.Door: StartCoroutine(studioDoor.OpenVisible()); break;
            }
        }
        private IEnumerator HideBody()
        {
            busy = true; game.SetPhase2BControl(false); hud.SetInteraction(""); hud.HideClue();
            voice?.PlayCleaning();
            yield return FadeScreen(1, .45f);
            if (!Active) { voice?.StopSfx(); yield break; }
            murder.SetCorpseVisible(false);
            if (wardrobeLeftDoor != null) wardrobeLeftDoor.localRotation = leftClosed;
            if (wardrobeRightDoor != null) wardrobeRightDoor.localRotation = rightClosed;
            yield return new WaitForSeconds(.45f);
            voice?.StopSfx();
            yield return FadeScreen(0, .45f);
            if (!Active) yield break;
            game.SetPhase2BControl(true); busy = false; BodyHidden = true;
            RefreshUI(); CheckSuccess();
        }
        private IEnumerator FadeScreen(float target, float duration)
        {
            float start = hud.FadeAlpha;
            for (float time = 0; time < duration; time += Time.deltaTime)
            { if (!Active) yield break; hud.SetFade(Mathf.Lerp(start, target, time / duration)); yield return null; }
            if (!Active) yield break;
            hud.SetFade(target);
        }
        private IEnumerator RotateWardrobe(bool open)
        {
            if (wardrobeLeftDoor == null || wardrobeRightDoor == null) yield break;
            var a = wardrobeLeftDoor.localRotation; var b = wardrobeRightDoor.localRotation;
            var aEnd = leftClosed * Quaternion.Euler(0, open ? -100 : 0, 0);
            var bEnd = rightClosed * Quaternion.Euler(0, open ? 100 : 0, 0);
            for (float t = 0; t < .7f; t += Time.deltaTime)
            { wardrobeLeftDoor.localRotation = Quaternion.Slerp(a, aEnd, t / .7f);
              wardrobeRightDoor.localRotation = Quaternion.Slerp(b, bEnd, t / .7f); yield return null; }
            wardrobeLeftDoor.localRotation = aEnd; wardrobeRightDoor.localRotation = bEnd;
        }
        private IEnumerator CleanBlood()
        {
            busy = true; game.SetPhase2BControl(false); hud.SetInteraction(""); hud.HideClue();
            IsCleaningBlood = true;
            voice?.PlayCleaning();
            if (brain != null) brain.enabled = false;
            Frame(blood.transform.position + new Vector3(-1.4f, 1.35f, -1.7f), blood.transform.position + Vector3.up * .05f, 48);
            yield return blood.CleanVisible(2.2f);
            if (!Active) yield break;
            IsCleaningBlood = false;
            voice?.StopSfx();
            clothStain.SetActive(true); StainedCloth = true;
            if (brain != null) brain.enabled = true;
            game.SetPhase2BControl(true); busy = false;
            RefreshUI(); CheckSuccess();
        }
        private IEnumerator CleanClothing()
        {
            busy = true; game.SetPhase2BControl(false); hud.SetInteraction(""); hud.HideClue();
            voice?.PlayCleaning();
            yield return FadeScreen(1, .45f);
            if (!Active) yield break;
            yield return new WaitForSeconds(4.1f);
            if (!Active) yield break;
            yield return FadeScreen(0, .45f);
            if (!Active) yield break;
            voice?.StopSfx();
            clothingMarks.SetActive(false);
            ClothingCleaned = true; busy = false;
            game.SetPhase2BControl(true);
            hud.ShowClue("The stains are gone. No one will notice.");
            RefreshUI(); CheckSuccess();
        }
        private void Frame(Vector3 position, Vector3 target, float fov)
        {
            gameCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            gameCamera.fieldOfView = fov;
        }
        private void RefreshUI()
        {
            checklist.text = $"{Mark(BodyHidden)} Hide Celeste's body\n{Mark(BloodCleaned)} Clean the blood\n{Mark(EvidenceDisposed)} Dispose of the evidence\n{Mark(ClothingCleaned)} Clean your uniform" +
                (warned ? "\nPREFECT APPROACHING" : "");
            game.SetPhase2BObjective("CLEAN UP BEFORE SOMEONE FINDS HER");
        }
        private static string Mark(bool complete) => complete ? "[✓]" : "[ ]";
        private bool AllClean => BodyHidden && BloodCleaned && EvidenceDisposed && ClothingCleaned;
        private void CheckSuccess() { if (Active && AllClean) StartCoroutine(Succeed()); }
        public void Fail(string reason)
        {
            if (!Active) return;
            Active = false; busy = true; IsCleaningBlood = false;
            StopAllCoroutines();
            emotionalNarrationActive = false; evelynSegments?.Stop();
            voice?.StopAll();
            StartCoroutine(FailureSequence(reason));
        }
        private IEnumerator FailureSequence(string reason)
        {
            game.EndPhase2B(); prefect.StopInvestigation(); checklistPanel.SetActive(false);
            hud.SetFade(0);
            hud.ShowGameplay(false); hud.HideClue();
            if (brain != null) brain.enabled = false;
            Frame(prefect.transform.position + new Vector3(-.55f, 1.65f, -1.2f), prefect.transform.position + Vector3.up * 1.25f, 55);
            prefect.FacePlayer();
            if (reason == "Celeste was found." || reason == "The blood was discovered.")
                voice?.QueuePrefectIncident(true);
            else if (reason == "The Prefect saw evidence on Evelyn.")
                voice?.QueuePrefectIncident(false);
            dialogue.ShowCinematicLine("PREFECT", "Miss Moreau. Explain yourself.");
            yield return new WaitForSeconds(3.2f);
            dialogue.ShowCinematicLine("PREFECT", "You are coming with me. The Chancellor will hear about this.");
            yield return new WaitForSeconds(3.1f);
            dialogue.HideCinematicLine();
            voice?.PlayGameOver();
            outcomeTitle.text = "THE FALLEN ROSE";
            outcomeSubtitle.text = "Some vows are never meant to be kept.\n" + reason;
            retryButton.gameObject.SetActive(true);
            outcomePanel.SetActive(true);
        }
        private IEnumerator Succeed()
        {
            Active = false; busy = true;
            voice?.StopSfx();
            game.EndPhase2B(); prefect.StopInvestigation(); checklistPanel.SetActive(false);
            hud.HideClue(); hud.ShowGameplay(false);
            if (brain != null) brain.enabled = false;
            Frame(player.position + new Vector3(-2.0f, 1.8f, 1.5f), player.position + Vector3.up, 48);
            var diary = FindFirstObjectByType<Phase3ADiaryIntro>();
            emotionalNarrationActive = diary != null &&
                evelynSegments.TryPlay(diary.unverifiedJapaneseNarration, diary.diarySegment);
            if (emotionalNarrationActive) StartCoroutine(RepeatEvelynNarration(diary));
            yield return EmotionalLine("It's over.");
            yield return EmotionalLine("Someone loves me.");
            yield return EmotionalLine("Tomorrow… Adrian will be mine.");
            dialogue.HideCinematicLine();
            emotionalNarrationActive = false; evelynSegments.Stop();
            for (float t = 0; t < .65f; t += Time.deltaTime) { hud.SetFade(t / .65f); yield return null; }
            hud.SetFade(1);
            if (missingStudents != null) yield return missingStudents.PlayMonologue();
            else Debug.LogError("Phase 3B monologue reference missing", this);
            hud.SetFade(0);
            choicePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            while (!ChoiceMade) yield return null;
            choicePanel.SetActive(false);
            ball?.SetBodyFate(BodyLeftBehind);
            if (!BodyLeftBehind)
            {
                hud.SetFade(1);
                outcomeTitle.text = "THAT NIGHT";
                outcomeSubtitle.text = "That night, Evelyn returned to the Art Studio.";
                outcomePanel.SetActive(true);
                yield return new WaitForSeconds(2.3f);
                outcomeSubtitle.text = "By morning, there was nothing left to find.";
                yield return new WaitForSeconds(2.3f);
                outcomePanel.SetActive(false);
            }
            outcomeTitle.text = "THE NEXT EVENING";
            outcomeSubtitle.text = "";
            retryButton.gameObject.SetActive(false);
            outcomePanel.SetActive(true);
            yield return new WaitForSeconds(2.6f);
            outcomePanel.SetActive(false);
            voice?.StopAll();
            if (ball != null) ball.BeginBall();
            else Debug.LogError("The Black Rose Ball director is missing.", this);
        }
        public void Retry()
        {
            if (Active) return;
            StopAllCoroutines();
            emotionalNarrationActive = false; evelynSegments?.Stop();
            voice?.StopAll();
            dialogue.HideCinematicLine(); outcomePanel.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
            hud.SetFade(0); hud.ShowGameplay(true); hud.HideClue();
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.SetPositionAndRotation(playerStart, playerStartRotation);
            if (controller != null) controller.enabled = true;
            murder.ResetCorpse(); blood.ResetEvidence();
            wardrobeLeftDoor.localRotation = leftClosed; wardrobeRightDoor.localRotation = rightClosed;
            prefect.ResetInvestigation();
            if (brain != null) brain.enabled = true;
            BeginChallenge();
        }

        private IEnumerator EmotionalLine(string words)
        {
            dialogue.ShowAdvanceableCinematicLine("EVELYN · THOUGHT", words);
            voice?.StopVoice();
            yield return dialogue.WaitForCinematicAdvance(8f);
        }
        private IEnumerator RepeatEvelynNarration(Phase3ADiaryIntro diary)
        {
            while (emotionalNarrationActive)
            {
                while (emotionalNarrationActive && evelynSegments.IsPlaying) yield return null;
                if (!emotionalNarrationActive) yield break;
                yield return new WaitForSecondsRealtime(3f);
                if (emotionalNarrationActive)
                    evelynSegments.TryPlay(diary.unverifiedJapaneseNarration, diary.diarySegment);
            }
        }

        public void ChooseBodyFate(bool leaveBody)
        {
            if (ChoiceMade || choicePanel == null || !choicePanel.activeInHierarchy) return;
            BodyLeftBehind = leaveBody;
            ChoiceMade = true;
        }

        private void BuildBodyChoice()
        {
            var root = new GameObject("Body fate choice", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = 260;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var background = new GameObject("Burgundy veil", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            var bg = background.GetComponent<RectTransform>(); bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
            bg.offsetMin = bg.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(.035f, .014f, .026f, .97f);
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 32);
            TextLabel(root.transform, "WHAT SHOULD I DO WITH HER BODY?", font, 52, new Vector2(.12f, .70f), new Vector2(.88f, .86f));
            ChoiceButton(root.transform, "BURN THE EVIDENCE\nErase every trace.", font,
                new Vector2(.16f, .38f), new Vector2(.48f, .60f), () => ChooseBodyFate(false));
            ChoiceButton(root.transform, "LEAVE THE BODY\nI’ve done enough. No one will look inside.", font,
                new Vector2(.52f, .38f), new Vector2(.84f, .60f), () => ChooseBodyFate(true));
            choicePanel = root; root.SetActive(false);
        }
        private static void TextLabel(Transform parent, string words, Font font, int size, Vector2 min, Vector2 max)
        {
            var go = new GameObject(words, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = go.GetComponent<Text>(); label.text = words; label.font = font; label.fontSize = size;
            label.color = new Color(.95f, .85f, .75f); label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }
        private static void ChoiceButton(Transform parent, string words, Font font, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(words, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.20f, .065f, .095f, .98f);
            go.GetComponent<Button>().onClick.AddListener(click);
            TextLabel(go.transform, words, font, 29, new Vector2(.05f, .08f), new Vector2(.95f, .92f));
        }
    }
}
