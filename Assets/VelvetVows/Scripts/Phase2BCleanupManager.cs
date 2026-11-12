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
        public bool InteractionBusy => busy;
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
            var performance = player != null ? player.GetComponent<AcademyActionPerformance>() : null;
            if (performance != null && performance.IsBusy) return false;
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
                    Animate(ActionKind.Pickup, worldCloth.transform, () =>
                    { HasCloth = true; worldCloth.SetActive(false); heldCloth.SetActive(true);
                      hud.ShowClue("A cloth from the Studio storage. The blood is still visible."); }); break;
                case Phase2BAction.Blood: StartCoroutine(CleanBlood()); break;
                case Phase2BAction.Disposal:
                    Animate(ActionKind.DisposeEvidence, FindActionTarget(Phase2BAction.Disposal), () =>
                    { attackProp.SetActive(false); heldCloth.SetActive(false);
                      EvidenceDisposed = true; RefreshUI(); CheckSuccess(); }); break;
                case Phase2BAction.Clothing: StartCoroutine(CleanClothing()); break;
                case Phase2BAction.Door: Animate(ActionKind.Door, studioDoor.transform, () => StartCoroutine(studioDoor.OpenVisible())); break;
            }
        }
        private Transform FindActionTarget(Phase2BAction action)
        {
            foreach (var item in FindObjectsByType<Phase2BInteractable>(FindObjectsSortMode.None)) if (item.Action == action) return item.transform;
            return player;
        }
        private void Animate(ActionKind kind, Transform target, System.Action impact = null)
        {
            var performance = player.GetComponent<AcademyActionPerformance>();
            if (performance == null) impact?.Invoke();
            else performance.PlayAction(kind, target, impact, null);
        }
        private IEnumerator HideBody()
        {
            busy = true; game.SetPhase2BControl(false); hud.SetInteraction(""); hud.HideClue();
            voice?.PlayCleaning();
            Animate(ActionKind.HideBody, celeste);
            bool restoreBrain = brain != null && brain.enabled;
            if (TryFrameBodyAction() && brain != null) brain.enabled = false;
            yield return RotateWardrobe(true);
            if (!Active) { voice?.StopSfx(); yield break; }
            var from = celeste.position; var to = wardrobe.position;
            to.y = from.y;
            var controller = player.GetComponent<CharacterController>();
            var corpseAnimator = celeste.GetComponentInChildren<Animator>(true);
            var chest = corpseAnimator != null && corpseAnimator.isHuman ? corpseAnimator.GetBoneTransform(HumanBodyBones.Chest) : null;
            float nextCameraCheck = 0;
            Vector3 dragDirection = to - from; dragDirection.y = 0;
            if (dragDirection.sqrMagnitude < .001f) dragDirection = player.forward;
            dragDirection.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dragDirection);
            if (Vector3.Dot(player.position - (chest != null ? chest.position : from), side) < 0) side = -side;
            // Reach the body before it starts moving; otherwise it appears to slide away by itself.
            for (float t = 0; t < 1.3f && controller != null && controller.enabled; t += Time.deltaTime)
            {
                if (!Active) yield break;
                Vector3 gripPosition = (chest != null ? chest.position : celeste.position) - dragDirection * .22f + side * .32f;
                Vector3 follow = gripPosition - player.position; follow.y = 0;
                if (follow.magnitude <= .08f) break;
                controller.Move(follow.normalized * Mathf.Min(follow.magnitude - .08f, Time.deltaTime * 2f));
                if (Time.time >= nextCameraCheck) { TryFrameBodyAction(); nextCameraCheck = Time.time + .25f; }
                yield return null;
            }
            // The body can only travel as far as the player's capsule actually moved.
            for (float t = 0; t < 4.2f; t += Time.deltaTime)
            {
                if (!Active) yield break;
                var remaining = to - celeste.position; remaining.y = 0;
                if (remaining.magnitude <= .06f) break;
                if (controller == null || !controller.enabled) break;
                var before = player.position;
                controller.Move(remaining.normalized * Mathf.Min(remaining.magnitude, 2f * Time.deltaTime));
                var travelled = player.position - before; travelled.y = 0;
                celeste.position += travelled;
                if (Time.time >= nextCameraCheck) { TryFrameBodyAction(); nextCameraCheck = Time.time + .25f; }
                yield return null;
            }
            if (Vector3.Distance(celeste.position, to) > .12f)
            {
                player.GetComponent<AcademyActionPerformance>()?.Cancel();
                yield return RotateWardrobe(false);
                voice?.StopSfx();
                if (brain != null && restoreBrain) brain.enabled = true;
                busy = false; game.SetPhase2BControl(true);
                hud.ShowClue("The way to the wardrobe is blocked. Move closer and try again.");
                yield break;
            }
            celeste.position = to;
            murder.SetCorpseVisible(false);
            yield return RotateWardrobe(false);
            var performance = player.GetComponent<AcademyActionPerformance>();
            while (Active && performance != null && performance.IsBusy) yield return null;
            voice?.StopSfx();
            if (!Active) yield break;
            if (brain != null && restoreBrain) brain.enabled = true;
            game.SetPhase2BControl(true); busy = false; BodyHidden = true;
            RefreshUI(); CheckSuccess();
        }
        private bool TryFrameBodyAction()
        {
            if (gameCamera == null || player == null || celeste == null) return false;
            Vector3 actor = player.position + Vector3.up * .85f;
            Vector3 body = celeste.position + Vector3.up * .4f;
            Vector3 focus = Vector3.Lerp(actor, body, .55f);
            bool Inside(Vector3 p) => p.x > -23.3f && p.x < -12.7f && p.z > -7.2f && p.z < 3.2f;
            bool Clear(Vector3 p) => Inside(p) && Phase2AStoryManager.CameraPathClear(p, actor) &&
                Phase2AStoryManager.CameraPathClear(p, body);
            var position = gameCamera.transform.position;
            if (Vector3.Distance(position, focus) > 4f || !Clear(position))
            {
                bool found = false;
                foreach (float distance in new[] { 3f, 2.4f, 3.6f })
                {
                    for (int angle = 0; angle < 360; angle += 30)
                    {
                        var candidate = focus + Quaternion.Euler(0, angle, 0) * Vector3.right * distance + Vector3.up * 1.15f;
                        if (!Clear(candidate)) continue;
                        position = candidate; found = true; break;
                    }
                    if (found) break;
                }
                if (!found) return false;
            }
            Frame(position, focus, 52);
            return true;
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
            Animate(ActionKind.CleanFloor, blood.transform);
            var action = player.GetComponent<AcademyActionPerformance>();
            while (Active && action != null && action.IsBusy && !action.Prepared) yield return null;
            if (!Active) yield break;
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
            Animate(ActionKind.WashClothes, FindActionTarget(Phase2BAction.Clothing));
            yield return new WaitForSeconds(4.9f);
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
            player.GetComponent<AcademyActionPerformance>()?.Cancel();
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
            prefect.FacePlayer();
            var prefectFocus = prefect.transform.position + Vector3.up * 1.35f;
            foreach (var offset in new[] { prefect.transform.forward * 1.8f, -prefect.transform.forward * 1.8f,
                prefect.transform.right * 1.8f, -prefect.transform.right * 1.8f })
            {
                var position = prefect.transform.position + offset + Vector3.up * 1.65f;
                if (!Phase2AStoryManager.CameraPathClear(position, prefectFocus)) continue;
                Frame(position, prefectFocus, 48); break;
            }
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
            player.GetComponent<AcademyActionPerformance>()?.Cancel();
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
            ChoiceButton(root.transform, "BURN THE EVIDENCE\nReturn tonight and destroy the body.", font,
                new Vector2(.16f, .38f), new Vector2(.48f, .60f), () => ChooseBodyFate(false));
            ChoiceButton(root.transform, "LEAVE THE BODY\nLeave her hidden. She could be found.", font,
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
