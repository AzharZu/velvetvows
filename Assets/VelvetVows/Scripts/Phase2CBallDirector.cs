using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VelvetVows
{
    public sealed class Phase2CBallDirector : MonoBehaviour
    {
        public GameManager game;
        public GameHUD hud;
        public DialogueManager dialogue;
        public Phase2AStoryManager story;
        public Transform player, isabelle, adrian;
        public Phase2AActorMotion isabelleMotion, adrianMotion;
        public Phase2ALovesick lovesick;
        public Phase2AReputation reputation;
        public GameObject guests;
        public GameObject ballDecor;
        public AnimationClip[] femalePoseBegin, femalePoseLoop, femalePoseStop;
        public AnimationClip[] malePoseBegin, malePoseLoop, malePoseStop;
        public AudioClip piano;
        public CharacterVoiceManager voice;
        public GameObject finalPanel;
        public Button playAgainButton, mainMenuButton;
        public Light ballLight;
        public Light portraitLight, burgundyLight;
        [Header("Final revelation artwork")]
        public Sprite adrianDiaryOne, adrianDiaryTwo, adrianDiaryThree, heartbrokenArtwork;
        public float dialogueSeconds = 2.3f;
        public string Beat { get; private set; } = "Dormant";
        public bool Resolved { get; private set; }
        public bool BodyLeftBehind { get; private set; }
        public bool Heartbroken { get; private set; }
        private Image diaryImage;
        private GameObject heartbrokenPanel;
        private Camera cameraView;
        private CinemachineBrain brain;
        private AudioSource music;
        private PlayableGraph[] graphs = new PlayableGraph[2];
        private static bool restartIntoStory;
        private bool started;
        private Coroutine cameraMove;
        private readonly Vector3 evelynPoint = new Vector3(19.0f, 0.05f, 0.15f);
        private readonly Vector3 adrianWaiting = new Vector3(22.9f, 0.05f, 0.5f);
        private readonly Vector3 isabelleWaiting = new Vector3(16.8f, 0.05f, -1.0f);
        private static readonly Vector3 CoupleCenter = new Vector3(20.1f, .05f, .1f);
        private static readonly Vector3 AdrianCamera = new Vector3(20.05f, 1.62f, -1.18f);
        private static readonly Vector3 EvelynCamera = new Vector3(20.15f, 1.62f, -1.18f);
        private static readonly Vector3 AdrianFace = new Vector3(20.48f, 1.49f, .1f);
        private static readonly Vector3 EvelynFace = new Vector3(19.72f, 1.49f, .1f);

        private void Start()
        {
            cameraView = Camera.main;
            brain = cameraView != null ? cameraView.GetComponent<CinemachineBrain>() : null;
            if (guests != null) guests.SetActive(false);
            if (ballDecor != null) ballDecor.SetActive(false);
            if (finalPanel != null) finalPanel.SetActive(false);
            if (portraitLight != null) portraitLight.enabled = false;
            if (burgundyLight != null) burgundyLight.enabled = false;
            if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(MainMenu);
            BuildEndingArtwork();
            if (restartIntoStory)
            {
                restartIntoStory = false;
                StartCoroutine(BeginAfterReload());
            }
        }
        private IEnumerator BeginAfterReload() { yield return null; game.Begin(); }
        private void OnDestroy()
        {
            for (int i = 0; i < graphs.Length; i++) if (graphs[i].IsValid()) graphs[i].Destroy();
        }
        public void BeginBall()
        {
            if (started || game == null || game.State != GameState.Phase2BOutcome) return;
            voice?.StopAll();
            started = true;
            StartCoroutine(Ball());
        }
        public void SetBodyFate(bool leftBehind)
        {
            if (started) return;
            BodyLeftBehind = leftBehind;
        }
        private IEnumerator Ball()
        {
            Beat = "Transition";
            game.BeginPhase2CBall();
            hud.SetFade(1);
            dialogue.HideCinematicLine();
            lovesick.Resolve();
            if (brain != null) brain.enabled = false;
            Place(player, evelynPoint, Quaternion.Euler(0, 90, 0));
            Place(adrian, adrianWaiting, Quaternion.Euler(0, 240, 0));
            Place(isabelle, isabelleWaiting, Quaternion.Euler(0, 65, 0));
            story?.evelynMotion?.Idle();
            isabelleMotion?.Idle();
            adrianMotion?.Idle();
            Dress(player, new Color(.30f, .055f, .10f), "Evelyn");
            Dress(adrian, new Color(.055f, .045f, .065f), "Adrian");
            if (guests != null) guests.SetActive(true);
            if (ballDecor != null) ballDecor.SetActive(true);
            if (ballLight != null) ballLight.enabled = true;
            if (portraitLight != null) portraitLight.enabled = true;
            if (burgundyLight != null) burgundyLight.enabled = true;
            SetBallLighting(false);
            StartMusic();
            Frame(new Vector3(20.1f, 3.3f, -6.4f), new Vector3(20.1f, 1.3f, 0), 52);
            yield return Fade(0, 1.1f);
            Beat = "Establishing";
            yield return new WaitForSeconds(3.2f);
            Frame(new Vector3(20.6f, 1.65f, -1.55f), player.position + Vector3.up * 1.2f, 34);
            yield return new WaitForSeconds(1.6f);

            Beat = "Isabelle";
            yield return isabelleMotion.WalkRoute(new Vector3(18.0f, 0, -0.7f));
            isabelleMotion.Face(player);
            story?.evelynMotion?.Face(isabelle);
            Frame(new Vector3(17.5f, 1.65f, -2.7f), (player.position + isabelle.position) * .5f + Vector3.up * 1.25f, 43);
            yield return Say("ISABELLE", "Evelyn! Have you seen Celeste?");
            yield return Say("EVELYN", "No. Why?");
            yield return Say("ISABELLE", "She hasn’t answered anyone since yesterday.");
            yield return Say("EVELYN", "Perhaps she went home.");
            yield return Say("ISABELLE", "Without telling anyone?");
            yield return Say("EVELYN", "You know how unpredictable she can be.");
            if (reputation != null && reputation.Value >= 5)
                yield return Say("ISABELLE", "I suppose… perhaps you’re right.");
            else
                yield return Say("ISABELLE", "Perhaps. I’ll keep looking for her.");
            dialogue.HideCinematicLine();
            yield return isabelleMotion.WalkRoute(isabelleWaiting);

            Beat = "Adrian";
            MoveFrame(new Vector3(20.25f, 1.7f, -2.5f), new Vector3(20.0f, 1.3f, .2f), 42, .8f);
            yield return adrianMotion.WalkRoute(new Vector3(20.55f, 0, 0.23f));
            adrianMotion.Face(player);
            story?.evelynMotion?.Face(adrian);
            yield return Say("ADRIAN", "I’ve been looking for you.");
            yield return Say("EVELYN", "M-me?");
            yield return Say("ADRIAN", "Who else?");
            MoveFrame(new Vector3(20.1f, 1.63f, -1.8f), new Vector3(20.1f, 1.42f, .1f), 36, .8f);
            yield return Say("ADRIAN", "May I have this dance?");
            yield return Say("EVELYN", "Yes.");
            dialogue.HideCinematicLine();

            Beat = "Dance";
            // These DancePose clips are independent solo dances: their raised arms
            // separate the leads in a two-shot. Keep both on their existing idles
            // and compose the dance from close staging and a restrained couple turn.
            story?.evelynMotion?.Idle();
            adrianMotion?.Idle();
            MoveFrame(new Vector3(20.1f, 1.68f, -1.35f), new Vector3(20.1f, 1.4f, .1f), 34, .65f);
            yield return Approach();
            lovesick.Suppressed = false;
            lovesick.CinematicPulse = true;
            lovesick.HideCinematicThoughts = true;
            lovesick.Pulse(60);
            yield return DanceTogether(8f);
            Beat = "Revelation";
            StartCoroutine(ShiftLighting(true, 1.2f));
            PlaceCouple(0);
            MoveFrame(AdrianCamera, AdrianFace, 30, .8f);
            yield return Say("ADRIAN", "Do you remember Lucien?");
            MoveFrame(EvelynCamera, EvelynFace, 32, .7f);
            yield return Say("EVELYN", "Lucien?");
            MoveFrame(AdrianCamera, AdrianFace, 30, .7f);
            yield return Say("ADRIAN", "He wouldn’t leave you alone.");
            MoveFrame(EvelynCamera, EvelynFace, 32, .7f);
            yield return Say("EVELYN", "What are you saying?");
            MoveFrame(AdrianCamera, AdrianFace, 30, .7f);
            yield return Say("ADRIAN", "Neither would the others.");
            MoveFrame(EvelynCamera, EvelynFace, 32, .7f);
            yield return Say("EVELYN", "You… knew?");
            MoveFrame(AdrianCamera, AdrianFace, 30, .7f);
            yield return Say("ADRIAN", "About Celeste? Of course.");
            if (BodyLeftBehind)
            { yield return HeartbrokenSequence(); yield break; }
            yield return SayDiary("ADRIAN", "I knew you were watching me.", adrianDiaryOne);
            yield return SayDiary("ADRIAN", "But you weren’t the only one watching.", adrianDiaryTwo);
            yield return SayDiary("ADRIAN", "I kept the things you left behind.", adrianDiaryThree);
            yield return Say("EVELYN", "You… kept my things?");
            yield return Say("ADRIAN", "Every single one.");
            yield return Say("EVELYN", "If you knew… why didn’t you tell me?");
            yield return Say("ADRIAN", "Because I was afraid.");
            yield return Say("EVELYN", "You? Afraid?");
            yield return Say("ADRIAN", "Everyone wanted my name. My fortune. My future.");
            yield return Say("ADRIAN", "But you wanted me.");
            yield return Say("ADRIAN", "I wanted you too, long before the Ball.");
            yield return Say("ADRIAN", "I wanted you to come to me willingly.");
            yield return Say("EVELYN", "You could have just asked.");
            yield return Say("ADRIAN", "And risk hearing you say no?");
            yield return Say("EVELYN", "You never had to be afraid.");
            dialogue.HideCinematicLine();
            yield return new WaitForSeconds(1.2f);
            MoveFrame(AdrianCamera, AdrianFace, 28, .75f);
            yield return Say("ADRIAN", "You did it for me.");
            yield return Say("ADRIAN", "And I did it for you.");
            yield return Say("ADRIAN", "I made sure those other boys could never pull you away.");
            lovesick.Resolve();
            Resolved = true;
            yield return new WaitForSeconds(.8f);
            StartCoroutine(ShiftLighting(false, 1.3f));
            MoveFrame(EvelynCamera, EvelynFace, 30, .85f);
            yield return Say("EVELYN", "Then we’re the same.");
            MoveFrame(new Vector3(20.1f, 1.65f, -1.8f), new Vector3(20.1f, 1.43f, .1f), 34, .9f);
            yield return Say("ADRIAN", "Perhaps we always were.");
            dialogue.HideCinematicLine();
            Beat = "FinalDance";
            yield return FinalImage();
            yield return Fade(.58f, 1.1f);
            game.EndPhase2C();
            Beat = "Ending";
            CanvasGroup endingGroup = null;
            if (finalPanel != null)
            {
                endingGroup = finalPanel.GetComponent<CanvasGroup>();
                if (endingGroup != null) endingGroup.alpha = 0;
                finalPanel.SetActive(true);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return Fade(0, .8f);
            if (endingGroup != null)
            {
                for (float t = 0; t < 1.1f; t += Time.deltaTime)
                { endingGroup.alpha = Mathf.Clamp01(t / 1.1f); yield return null; }
                endingGroup.alpha = 1;
            }
        }
        private IEnumerator Approach()
        {
            var startEvelyn = player.position;
            var startAdrian = adrian.position;
            var startEvelynRotation = player.rotation;
            var startAdrianRotation = adrian.rotation;
            const float duration = 3f;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float blend = Mathf.SmoothStep(0, 1, time / duration);
                player.SetPositionAndRotation(Vector3.Lerp(startEvelyn, CoupleCenter + Vector3.left * .38f, blend),
                    Quaternion.Slerp(startEvelynRotation, Quaternion.Euler(0, 110, 0), blend));
                adrian.SetPositionAndRotation(Vector3.Lerp(startAdrian, CoupleCenter + Vector3.right * .38f, blend),
                    Quaternion.Slerp(startAdrianRotation, Quaternion.Euler(0, 250, 0), blend));
                yield return null;
            }
            PlaceCouple(0);
        }
        private IEnumerator DanceTogether(float duration)
        {
            if (cameraMove != null) { StopCoroutine(cameraMove); cameraMove = null; }
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float progress = Mathf.Clamp01(time / duration);
                float sway = Mathf.Sin(time * 1.45f) * 7f;
                PlaceCouple(sway);
                float angle = Mathf.Lerp(-19f, 19f, progress);
                var offset = Quaternion.Euler(0, angle, 0) * new Vector3(0, .22f, -1.38f);
                Frame(CoupleCenter + Vector3.up * 1.46f + offset,
                    CoupleCenter + Vector3.up * 1.45f, 33);
                yield return null;
            }
            PlaceCouple(0);
        }
        private IEnumerator FinalImage()
        {
            if (cameraMove != null) { StopCoroutine(cameraMove); cameraMove = null; }
            const float duration = 4f;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0, 1, time / duration);
                PlaceCouple(Mathf.Sin(time * 1.2f) * 3f);
                Frame(Vector3.Lerp(new Vector3(20.1f, 1.68f, -1.45f),
                    new Vector3(20.1f, 1.68f, -2.05f), progress),
                    CoupleCenter + Vector3.up * 1.42f, Mathf.Lerp(32, 35, progress));
                yield return null;
            }
            PlaceCouple(0);
        }
        private void PlaceCouple(float turn)
        {
            var rotation = Quaternion.Euler(0, turn, 0);
            player.SetPositionAndRotation(CoupleCenter + rotation * Vector3.left * .38f,
                Quaternion.Euler(0, 110 + turn, 0));
            adrian.SetPositionAndRotation(CoupleCenter + rotation * Vector3.right * .38f,
                Quaternion.Euler(0, 250 + turn, 0));
        }
        private void SetBallLighting(bool revelation)
        {
            if (ballLight != null)
            {
                ballLight.color = revelation ? new Color(.73f, .70f, .82f) : new Color(1f, .73f, .49f);
                ballLight.intensity = revelation ? 1.15f : 2.2f;
            }
            if (portraitLight != null)
            {
                portraitLight.color = revelation ? new Color(.81f, .84f, 1f) : new Color(1f, .82f, .64f);
                portraitLight.intensity = revelation ? 1.3f : 1.65f;
            }
            if (burgundyLight != null) burgundyLight.intensity = revelation ? .65f : 1.1f;
        }
        private IEnumerator ShiftLighting(bool revelation, float duration)
        {
            if (ballLight == null || portraitLight == null || burgundyLight == null)
            { SetBallLighting(revelation); yield break; }
            Color hallColor = ballLight.color;
            Color faceColor = portraitLight.color;
            float hallIntensity = ballLight.intensity;
            float faceIntensity = portraitLight.intensity;
            float roseIntensity = burgundyLight.intensity;
            Color targetHall = revelation ? new Color(.73f, .70f, .82f) : new Color(1f, .73f, .49f);
            Color targetFace = revelation ? new Color(.81f, .84f, 1f) : new Color(1f, .82f, .64f);
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float blend = Mathf.SmoothStep(0, 1, time / duration);
                ballLight.color = Color.Lerp(hallColor, targetHall, blend);
                portraitLight.color = Color.Lerp(faceColor, targetFace, blend);
                ballLight.intensity = Mathf.Lerp(hallIntensity, revelation ? 1.15f : 2.2f, blend);
                portraitLight.intensity = Mathf.Lerp(faceIntensity, revelation ? 1.3f : 1.65f, blend);
                burgundyLight.intensity = Mathf.Lerp(roseIntensity, revelation ? .65f : 1.1f, blend);
                yield return null;
            }
            SetBallLighting(revelation);
        }
        private void MoveFrame(Vector3 location, Vector3 target, float fieldOfView, float duration)
        {
            if (cameraMove != null) StopCoroutine(cameraMove);
            cameraMove = StartCoroutine(GlideFrame(location, target, fieldOfView, duration));
        }
        private IEnumerator GlideFrame(Vector3 location, Vector3 target, float fieldOfView, float duration)
        {
            if (cameraView == null) yield break;
            var startPosition = cameraView.transform.position;
            var startRotation = cameraView.transform.rotation;
            var endRotation = Quaternion.LookRotation(target - location);
            float startFov = cameraView.fieldOfView;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float blend = Mathf.SmoothStep(0, 1, time / duration);
                cameraView.transform.SetPositionAndRotation(Vector3.Lerp(startPosition, location, blend),
                    Quaternion.Slerp(startRotation, endRotation, blend));
                cameraView.fieldOfView = Mathf.Lerp(startFov, fieldOfView, blend);
                yield return null;
            }
            Frame(location, target, fieldOfView);
            cameraMove = null;
        }
        private IEnumerator Say(string speaker, string line)
        {
            dialogue.ShowCinematicLine(speaker, line);
            // Keep each subtitle up long enough to read and let its voice finish.
            float readingSeconds = .75f + line.Length * .05f;
            float voiceSeconds = voice != null ? voice.VoiceRemainingSeconds : 0;
            float duration = dialogueSeconds < 1f ? dialogueSeconds :
                Mathf.Max(dialogueSeconds, Mathf.Max(readingSeconds, voiceSeconds));
            yield return new WaitForSeconds(duration);
        }
        private IEnumerator SayDiary(string speaker, string line, Sprite page)
        {
            if (diaryImage != null) { diaryImage.sprite = page; diaryImage.gameObject.SetActive(page != null); }
            dialogue.ShowAdvanceableCinematicLine(speaker, line);
            yield return dialogue.WaitForCinematicAdvance(3600f);
            if (diaryImage != null) diaryImage.gameObject.SetActive(false);
        }
        private IEnumerator HeartbrokenSequence()
        {
            Heartbroken = true; Beat = "Heartbroken";
            if (cameraMove != null) { StopCoroutine(cameraMove); cameraMove = null; }
            var prefect = FindFirstObjectByType<Phase2BPrefectInvestigation>();
            if (prefect != null)
            {
                Place(prefect.transform, new Vector3(20.9f, 0, -4f), Quaternion.Euler(0, 180, 0));
                for (float t = 0; t < 1.4f; t += Time.deltaTime)
                {
                    prefect.transform.position = Vector3.MoveTowards(prefect.transform.position,
                        new Vector3(20.9f, 0, -2.5f), 1.1f * Time.deltaTime);
                    if (prefect.animator != null) prefect.animator.SetFloat("Speed", 1.1f);
                    yield return null;
                }
                if (prefect.animator != null) prefect.animator.SetFloat("Speed", 0);
                prefect.FacePlayer();
            }
            MoveFrame(new Vector3(20f, 1.9f, -3.3f), new Vector3(20.4f, 1.4f, -.5f), 41, .45f);
            yield return Say("PREFECT", "Stop!");
            yield return Say("PREFECT", "Evelyn Moreau, you’re coming with me.");
            yield return Say("PREFECT", "We found Celeste’s body in the Art Studio.");
            yield return Say("PREFECT", "And the blood traces led us to you.");
            MoveFrame(EvelynCamera, EvelynFace, 32, .5f);
            yield return Say("EVELYN · THOUGHT", "He knows.");
            yield return Say("EVELYN · THOUGHT", "He knows what I’ve done.");
            yield return Say("EVELYN · THOUGHT", "He’ll never look at me the same way again.");
            yield return Say("EVELYN · THOUGHT", "He knows I’m a monster.");
            dialogue.HideCinematicLine();
            lovesick.Resolve();
            if (music != null) music.Stop();
            voice?.StopAll();
            game.EndPhase2C();
            if (heartbrokenPanel != null) heartbrokenPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            voice?.PlayGameOver();
        }
        private void BuildEndingArtwork()
        {
            var canvas = dialogue != null && dialogue.PanelRoot != null ?
                dialogue.PanelRoot.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
            {
                var go = new GameObject("Adrian diary revelation", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                go.transform.SetAsFirstSibling();
                var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.06f, .19f);
                rect.anchorMax = new Vector2(.94f, .96f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                diaryImage = go.GetComponent<Image>(); diaryImage.preserveAspect = true;
                diaryImage.raycastTarget = false; go.SetActive(false);
            }
            var root = new GameObject("HEARTBROKEN artwork ending", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var endingCanvas = root.GetComponent<Canvas>(); endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            endingCanvas.overrideSorting = true; endingCanvas.sortingOrder = 320;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var art = new GameObject("HEARTBROKEN supplied artwork", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(root.transform, false);
            var artRect = art.GetComponent<RectTransform>(); artRect.anchorMin = new Vector2(0, .12f);
            artRect.anchorMax = Vector2.one; artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            var image = art.GetComponent<Image>(); image.sprite = heartbrokenArtwork; image.preserveAspect = true;
            var footer = new GameObject("Ending controls backdrop", typeof(RectTransform), typeof(Image));
            footer.transform.SetParent(root.transform, false);
            var footerRect = footer.GetComponent<RectTransform>(); footerRect.anchorMin = Vector2.zero;
            footerRect.anchorMax = new Vector2(1, .12f); footerRect.offsetMin = footerRect.offsetMax = Vector2.zero;
            footer.GetComponent<Image>().color = new Color(.035f, .012f, .024f);
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 28);
            EndingButton(root.transform, "RETRY", font, new Vector2(.27f, .025f), new Vector2(.47f, .095f), PlayAgain);
            EndingButton(root.transform, "MAIN MENU", font, new Vector2(.53f, .025f), new Vector2(.73f, .095f), MainMenu);
            heartbrokenPanel = root; root.SetActive(false);
        }
        private static void EndingButton(Transform parent, string words, Font font, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(words, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(.19f, .06f, .09f);
            go.GetComponent<Button>().onClick.AddListener(click);
            var text = new GameObject(words + " label", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(go.transform, false);
            var tr = text.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var label = text.GetComponent<Text>(); label.text = words; label.font = font; label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter; label.color = new Color(.95f, .86f, .76f);
            label.raycastTarget = false;
        }
        private IEnumerator Fade(float target, float duration)
        {
            float initial = hud.FadeAlpha;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                hud.SetFade(Mathf.Lerp(initial, target, time / duration));
                yield return null;
            }
            hud.SetFade(target);
        }
        private void Frame(Vector3 location, Vector3 target, float fieldOfView)
        {
            if (cameraView == null) return;
            cameraView.transform.SetPositionAndRotation(location, Quaternion.LookRotation(target - location));
            cameraView.fieldOfView = fieldOfView;
        }
        private static void Place(Transform actor, Vector3 position, Quaternion rotation)
        {
            if (actor == null) return;
            var controller = actor.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            actor.SetPositionAndRotation(position, rotation);
            if (controller != null) controller.enabled = true;
            Physics.SyncTransforms();
        }
        private void PlayPose(int slot, int pose, AnimationClip[] clips)
        {
            if (clips == null || clips.Length <= pose || clips[pose] == null) return;
            var actor = slot == 0 ? player : adrian;
            var animator = actor != null ? actor.GetComponentInChildren<Animator>() : null;
            if (animator == null) return;
            if (graphs[slot].IsValid()) graphs[slot].Destroy();
            animator.applyRootMotion = false;
            graphs[slot] = PlayableGraph.Create("Black Rose Ball lead " + slot);
            var playable = AnimationClipPlayable.Create(graphs[slot], clips[pose]);
            playable.SetApplyFootIK(true);
            AnimationPlayableOutput.Create(graphs[slot], "Lead", animator).SetSourcePlayable(playable);
            graphs[slot].Play();
        }
        private void StopPose(int slot)
        {
            if (graphs[slot].IsValid()) graphs[slot].Destroy();
        }
        private void StartMusic()
        {
            if (piano == null) return;
            FindFirstObjectByType<MusicManager>()?.StopForBall();
            music = gameObject.AddComponent<AudioSource>();
            music.clip = piano;
            music.loop = true;
            music.spatialBlend = 0;
            music.volume = 0;
            music.Play();
            StartCoroutine(FadeMusic());
        }
        private IEnumerator FadeMusic()
        {
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            { if (music != null) music.volume = Mathf.Lerp(0, .19f, t / 2.5f); yield return null; }
            if (music != null) music.volume = .19f;
        }
        private static void Dress(Transform actor, Color tint, string character)
        {
            if (actor == null) return;
            foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var materials = renderer.materials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    string name = material.name.ToLowerInvariant();
                    if (!(name.Contains("uniform") || name.Contains("cloth") || name.Contains("shirt") ||
                          name.Contains("jacket") || name.Contains("skirt") || name.Contains("pants") ||
                          name.Contains("trouser") || name.Contains("suit") || name.Contains("dress"))) continue;
                    if (name.Contains("skin") || name.Contains("hair") || name.Contains("eye")) continue;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
                    else if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
                    changed = true;
                }
                if (changed) renderer.materials = materials;
            }
            Debug.Log("[Phase2C] Formal palette applied to " + character);
        }
        public void PlayAgain()
        {
            voice?.StopAll();
            restartIntoStory = true;
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        public void MainMenu()
        {
            voice?.StopAll();
            restartIntoStory = false;
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
