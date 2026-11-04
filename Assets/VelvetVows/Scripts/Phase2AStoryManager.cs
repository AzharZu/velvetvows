using System;
using System.Collections;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VelvetVows
{
    public enum Phase2AStage
    {
        NotStarted, Opening, MissingStudent, Overhear, Overhearing,
        WatchAdrian, Watching, Errands, RibbonSearch, RibbonReturn,
        FollowCeleste, Witness, WitnessLeaving, CloseDoor, Confront, Complete
    }

    public sealed class Phase2AStoryManager : MonoBehaviour
    {
        [Header("Existing systems")]
        public GameManager game;
        public GameHUD hud;
        public DialogueManager dialogue;
        public Phase2AReputation reputation;
        public Phase2ALovesick lovesick;
        public Transform player, celeste, adrian, isabelle, amelie, clara, margot, odette, witness;
        public Phase2AActorMotion evelynMotion, celesteMotion, adrianMotion, isabelleMotion, amelieMotion, witnessMotion;
        public Phase2AFollower follower;
        public Phase2AStudioDoor studioDoor;
        public Phase2BMurderSequence phase2B;
        public Phase3ADiaryIntro secretDiary;
        public Sprite ribbonArtwork;
        [Header("Physical story props")]
        public GameObject openingBooks, adrianHeldBook, artMaterials, libraryBooks, lostPendant, blackRibbon, sketchbook;
        [Header("Story UI")]
        public Text titleText, thoughtText, pageTitle, pageBody, completeText;
        public GameObject pagePanel, completionPanel;
        public Button pagePrevious, pageNext, pageClose, restartButton;
        [Header("Tuning")]
        [Range(1.8f, 4f)] public float overhearRadius = 2.8f;
        [Range(3f, 9f)] public float observationRadius = 6f;

        public Phase2AStage Stage { get; private set; } = Phase2AStage.NotStarted;
        public bool HasBooks { get; private set; }
        public bool HasArtMaterials { get; private set; }
        public bool HasPendant { get; private set; }
        public bool HasRibbon { get; private set; }
        public bool SketchbookSeen { get; private set; }
        private bool booksDone, artDone, pendantDone, isabelleReady;
        private int sketchPage;
        private Camera gameplayCamera;
        private CinemachineBrain cameraBrain;
        private AcademyCharacterAnimation normalPlayerAnimation;
        private CharacterController playerCollider;
        private Coroutine cinematic;
        private Image ribbonIllustration;
        private bool confrontationStarting;
        private readonly string[] sketchPages =
        {
            "Evelyn read alone in the Library again. She stayed after everyone left.",
            "Evelyn sat near the tall window. She never noticed anyone watching.",
            "Lucien Morel spoke to her after mathematics. She turned away.",
            "He kept bothering her.",
            "She will never have to worry about them again."
        };

        private void Awake()
        {
            if (game == null) game = GetComponent<GameManager>();
            if (player != null)
            {
                normalPlayerAnimation = player.GetComponent<AcademyCharacterAnimation>();
                playerCollider = player.GetComponent<CharacterController>();
            }
            gameplayCamera = Camera.main;
            if (gameplayCamera != null) cameraBrain = gameplayCamera.GetComponent<CinemachineBrain>();
        }

        private void Start()
        {
            if (pagePanel != null) pagePanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (thoughtText != null) thoughtText.text = "";
            if (openingBooks != null) openingBooks.SetActive(false);
            if (adrianHeldBook != null) adrianHeldBook.SetActive(false);
            if (pagePrevious != null) pagePrevious.onClick.AddListener(() => ChangePage(-1));
            if (pageNext != null) pageNext.onClick.AddListener(() => ChangePage(1));
            if (pageClose != null) pageClose.onClick.AddListener(CloseSketchbook);
            if (restartButton != null) restartButton.onClick.AddListener(() => game.Restart());
            if (ribbonArtwork != null && dialogue != null && dialogue.PanelRoot != null)
            {
                var canvas = dialogue.PanelRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var go = new GameObject("Celeste ribbon illustration", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(canvas.transform, false);
                    var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.66f, .26f);
                    rect.anchorMax = new Vector2(.96f, .76f); rect.offsetMin = rect.offsetMax = Vector2.zero;
                    ribbonIllustration = go.GetComponent<Image>(); ribbonIllustration.sprite = ribbonArtwork;
                    ribbonIllustration.preserveAspect = true; ribbonIllustration.raycastTarget = false;
                    go.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (game == null || game.State != GameState.Phase2A || player == null) return;
            if (pagePanel != null && pagePanel.activeSelf && Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseSketchbook();
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame) ChangePage(-1);
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame) ChangePage(1);
            }
            if (Stage == Phase2AStage.Overhear && isabelleReady && !DialogueManager.IsOpen)
            {
                var center = (celeste.position + isabelle.position) * .5f;
                if (Vector3.Distance(player.position, center) < overhearRadius)
                    StartCoroutine(OverhearConversation());
            }
            else if (Stage == Phase2AStage.WatchAdrian && !DialogueManager.IsOpen)
            {
                if (Vector3.Distance(player.position, adrian.position) < observationRadius)
                    StartCoroutine(ObserveCelesteAndAdrian());
            }
            else if (Stage == Phase2AStage.FollowCeleste)
            {
                bool playerInside = player.position.x < -14.5f && player.position.x > -24.5f &&
                                    player.position.z > -8f && player.position.z < 4f;
                bool celesteInside = celeste.position.x < -17.2f && celeste.position.x > -24.5f &&
                                     celeste.position.z > -8f && celeste.position.z < 4f;
                if (playerInside && celesteInside && Vector3.Distance(player.position, celeste.position) < 4f)
                {
                    follower.Stop();
                    SetStage(Phase2AStage.Witness, "Get the Art Studio witness to leave.");
                    celesteMotion.Idle();
                }
            }
        }

        public void BeginOpening()
        {
            if (Stage != Phase2AStage.NotStarted) return;
            SetStage(Phase2AStage.Opening, "");
            cinematic = StartCoroutine(OpeningSequence());
        }

        public bool CanInteract(Phase2AAction action)
        {
            if (game == null || game.State != GameState.Phase2A || DialogueManager.IsOpen ||
                pagePanel != null && pagePanel.activeSelf) return false;
            switch (action)
            {
                case Phase2AAction.CelesteTalk: return Stage == Phase2AStage.Errands;
                case Phase2AAction.BooksPickup: return Stage == Phase2AStage.Errands && !booksDone && !HasBooks;
                case Phase2AAction.BooksDelivery: return Stage == Phase2AStage.Errands && HasBooks && !booksDone;
                case Phase2AAction.ArtPickup: return Stage == Phase2AStage.Errands && !artDone && !HasArtMaterials;
                case Phase2AAction.ArtDelivery: return Stage == Phase2AStage.Errands && HasArtMaterials && !artDone;
                case Phase2AAction.LostPickup: return Stage == Phase2AStage.Errands && !pendantDone && !HasPendant;
                case Phase2AAction.LostDelivery: return Stage == Phase2AStage.Errands && HasPendant && !pendantDone;
                case Phase2AAction.RibbonPickup: return Stage == Phase2AStage.RibbonSearch && !HasRibbon;
                case Phase2AAction.RibbonReturn: return Stage == Phase2AStage.RibbonReturn && HasRibbon;
                case Phase2AAction.Sketchbook: return (Stage == Phase2AStage.RibbonSearch || Stage == Phase2AStage.RibbonReturn) && !SketchbookSeen;
                case Phase2AAction.Witness: return Stage == Phase2AStage.Witness;
                case Phase2AAction.StudioDoor: return Stage == Phase2AStage.CloseDoor || Stage == Phase2AStage.Complete && studioDoor.Closed;
                case Phase2AAction.Confront: return Stage == Phase2AStage.Confront && studioDoor.Closed && !confrontationStarting;
                default: return false;
            }
        }

        public string Prompt(Phase2AAction action)
        {
            switch (action)
            {
                case Phase2AAction.CelesteTalk: return "Speak to Celeste";
                case Phase2AAction.BooksPickup: return "Pick up the returned books";
                case Phase2AAction.BooksDelivery: return "Return books to Clara";
                case Phase2AAction.ArtPickup: return "Take the art materials";
                case Phase2AAction.ArtDelivery: return "Deliver materials to Margot";
                case Phase2AAction.LostPickup: return "Pick up Odette's pendant";
                case Phase2AAction.LostDelivery: return "Return pendant to Odette";
                case Phase2AAction.RibbonPickup: return "Pick up Celeste's black ribbon";
                case Phase2AAction.RibbonReturn: return "Return the ribbon to Celeste";
                case Phase2AAction.Sketchbook: return "Examine the open sketchbook";
                case Phase2AAction.Witness: return "Speak to Margot";
                case Phase2AAction.StudioDoor: return Stage == Phase2AStage.Complete ? "Open studio door" : "Close studio door";
                case Phase2AAction.Confront: return "Speak to Celeste";
                default: return "Interact";
            }
        }

        public void Interact(Phase2AAction action)
        {
            if (!CanInteract(action)) return;
            switch (action)
            {
                case Phase2AAction.CelesteTalk: TalkToCeleste(); break;
                case Phase2AAction.BooksPickup:
                    HasBooks = true; libraryBooks.SetActive(false); hud.ShowClue("Clara's returned books. She is waiting nearby in the Library."); break;
                case Phase2AAction.BooksDelivery:
                    Speak(new[] { L("CLARA", "You brought them back. Thank you, Evelyn."), L("EVELYN", "I was passing by.") },
                        () => { booksDone = true; HasBooks = false; reputation.Award("library-books", 2); RefreshErrandObjective(); }); break;
                case Phase2AAction.ArtPickup:
                    HasArtMaterials = true; artMaterials.SetActive(false); hud.ShowClue("Margot is working in the Art Studio. These belong to her."); break;
                case Phase2AAction.ArtDelivery:
                    Speak(new[] { L("MARGOT", "My charcoal and paper! I thought I had lost them."), L("EVELYN", "Here you are.") },
                        () => { artDone = true; HasArtMaterials = false; reputation.Award("art-materials", 2); RefreshErrandObjective(); }); break;
                case Phase2AAction.LostPickup:
                    HasPendant = true; lostPendant.SetActive(false); hud.ShowClue("Odette's pendant. She is waiting by the Library passage."); break;
                case Phase2AAction.LostDelivery:
                    Speak(new[] { L("ODETTE", "You found my pendant! That's kind of you."), L("EVELYN", "I'm glad it found its way back.") },
                        () => { pendantDone = true; HasPendant = false; reputation.Award("odette-pendant", 1); RefreshErrandObjective(); }); break;
                case Phase2AAction.RibbonPickup:
                    HasRibbon = true; blackRibbon.SetActive(false);
                    SetStage(Phase2AStage.RibbonReturn, "Return the black ribbon to Celeste.");
                    if (ribbonIllustration != null) StartCoroutine(ShowRibbonArtwork());
                    hud.ShowClue("A black ribbon, tied for the Ball."); break;
                case Phase2AAction.RibbonReturn: ReturnRibbon(); break;
                case Phase2AAction.Sketchbook: OpenSketchbook(); break;
                case Phase2AAction.Witness: PersuadeWitness(); break;
                case Phase2AAction.StudioDoor:
                    if (Stage == Phase2AStage.Complete) StartCoroutine(studioDoor.OpenVisible());
                    else StartCoroutine(CloseStudioDoor());
                    break;
                case Phase2AAction.Confront: ConfrontCeleste(); break;
            }
        }

        private static DialogueLine L(string speaker, string text) => new DialogueLine(speaker, text);
        private IEnumerator ShowRibbonArtwork()
        {
            ribbonIllustration.gameObject.SetActive(true);
            yield return new WaitForSeconds(4f);
            if (ribbonIllustration != null) ribbonIllustration.gameObject.SetActive(false);
        }
        private void SetStage(Phase2AStage next, string objective)
        {
            Stage = next;
            game?.SetPhase2AObjective(objective);
            Debug.Log($"[Phase2A] Stage: {Stage}; reputation={reputation?.Value}", this);
        }
        private void Speak(DialogueLine[] lines, Action done = null, Action<int> onLineShown = null)
        {
            if (dialogue == null) { Debug.LogError("Phase2A dialogue reference missing", this); return; }
            hud.HideClue();
            dialogue.BeginScripted(lines, done, onLineShown);
        }

        private IEnumerator OpeningSequence()
        {
            if (gameplayCamera == null || playerCollider == null || adrian == null)
            { Debug.LogError("Phase2A opening references missing", this); yield break; }
            game.SetPhase2AControl(false);
            lovesick.Suppressed = true;
            if (normalPlayerAnimation != null) normalPlayerAnimation.enabled = false;
            evelynMotion.enabled = true;
            if (cameraBrain != null) cameraBrain.enabled = false;
            playerCollider.enabled = false;
            player.SetPositionAndRotation(new Vector3(0, .02f, -8f), Quaternion.identity);
            adrian.SetPositionAndRotation(new Vector3(-3.3f, .09f, -4.8f), Quaternion.Euler(0, 75, 0));
            playerCollider.enabled = true;
            Frame(new Vector3(2.7f, 2.0f, -9.1f), new Vector3(0, 1.1f, -5.4f), 42);
            openingBooks.SetActive(true);
            evelynMotion.Walk();
            yield return MoveActor(player, new Vector3(0, player.position.y, -5.5f), 1.1f);
            evelynMotion.Idle();
            // A physical book falls beside Evelyn. The pickup happens between camera cuts.
            openingBooks.transform.SetParent(null, true);
            openingBooks.transform.position = player.position + new Vector3(.35f, 1.05f, .28f);
            if (openingBooks.GetComponent<Collider>() == null) openingBooks.AddComponent<BoxCollider>();
            var rigidbody = openingBooks.AddComponent<Rigidbody>();
            rigidbody.mass = .35f;
            yield return new WaitForSeconds(.9f);
            // Reuse the two students who appear in the next scene. They leave before Adrian reaches the book.
            isabelle.SetPositionAndRotation(new Vector3(-1.55f, 0, -5.1f), Quaternion.Euler(0, 100, 0));
            amelie.SetPositionAndRotation(new Vector3(1.5f, 0, -5.2f), Quaternion.Euler(0, 260, 0));
            evelynMotion.Idle();
            isabelleMotion.Talk(); amelieMotion.Talk();
            Frame(new Vector3(1.35f, 1.75f, -7.9f), new Vector3(0, .9f, -5.3f), 41);
            dialogue.ShowAdvanceableCinematicLine("ISABELLE", "Nobody even wants her here.");
            yield return dialogue.WaitForCinematicAdvance(3f);
            dialogue.ShowAdvanceableCinematicLine("AMELIE", "Ha…");
            yield return dialogue.WaitForCinematicAdvance(2f);
            adrianMotion.Talk();
            dialogue.ShowAdvanceableCinematicLine("ADRIAN", "Get away.");
            yield return dialogue.WaitForCinematicAdvance(2.3f);
            dialogue.ShowAdvanceableCinematicLine("ISABELLE", "Whatever. Let’s go.");
            yield return dialogue.WaitForCinematicAdvance(2.2f);
            dialogue.HideCinematicLine();
            isabelleMotion.Idle(); amelieMotion.Idle();
            var isabelleExit = StartCoroutine(isabelleMotion.WalkRoute(
                new Vector3(-2.5f, 0, -3.5f), new Vector3(-4.1f, 0, -3.5f)));
            var amelieExit = StartCoroutine(amelieMotion.WalkRoute(
                new Vector3(2.5f, 0, -3.4f), new Vector3(4.1f, 0, -3.4f)));
            yield return isabelleExit; yield return amelieExit;
            isabelle.gameObject.SetActive(false); amelie.gameObject.SetActive(false);
            Frame(new Vector3(-1.6f, 1.7f, -7.5f), new Vector3(-.3f, 1f, -5f), 38);
            yield return adrianMotion.WalkRoute(new Vector3(-1.1f, adrian.position.y, -5.3f));
            adrianMotion.Face(player); evelynMotion.Face(adrian);
            adrianMotion.Talk();
            dialogue.ShowAdvanceableCinematicLine("ADRIAN", "Are you alright?");
            yield return dialogue.WaitForCinematicAdvance(3.1f);
            evelynMotion.Talk();
            dialogue.ShowAdvanceableCinematicLine("EVELYN", "Y-yes. Thank you.");
            yield return dialogue.WaitForCinematicAdvance(3.0f);
            // Close view of the fallen book, then a cut back to the two characters.
            Frame(new Vector3(.85f, .9f, -6.4f), new Vector3(.2f, .25f, -5.3f), 40);
            dialogue.HideCinematicLine();
            yield return new WaitForSeconds(1.0f);
            openingBooks.SetActive(false);
            if (adrianHeldBook != null) adrianHeldBook.SetActive(true);
            Frame(new Vector3(1.5f, 1.8f, -7.3f), new Vector3(-.4f, 1.1f, -5.2f), 37);
            adrianMotion.Talk();
            dialogue.ShowAdvanceableCinematicLine("ADRIAN", "Be careful, Evelyn.");
            yield return dialogue.WaitForCinematicAdvance(3.2f);
            dialogue.ShowAdvanceableCinematicLine("ADRIAN", "Evelyn, isn’t it?");
            yield return dialogue.WaitForCinematicAdvance(2.7f);
            dialogue.HideCinematicLine();
            if (secretDiary != null) yield return secretDiary.RememberedName();
            adrianMotion.Walk();
            // Leave along the open south aisle; the old diagonal ended inside
            // the waiting sofa at x=-5.6, z=-4.1.
            yield return adrianMotion.WalkRoute(
                new Vector3(-2.8f, adrian.position.y, -6.2f),
                new Vector3(-4.7f, adrian.position.y, -6.2f),
                new Vector3(-7.3f, adrian.position.y, -6.2f));
            adrian.gameObject.SetActive(false);
            evelynMotion.Face(adrian);
            thoughtText.text = "";
            lovesick.Suppressed = false; lovesick.Pulse(5f);
            yield return new WaitForSeconds(.4f);
            thoughtText.text = "Someone like him could never love someone like me.";
            yield return new WaitForSeconds(3.2f);
            thoughtText.text = "";
            if (secretDiary != null)
            {
                for (float t = 0; t < .55f; t += Time.deltaTime)
                { hud.SetFade(t / .55f); yield return null; }
                hud.SetFade(1);
                game.SetPhase3AIntro(true);
                yield return secretDiary.Play();
                game.SetPhase3AIntro(false);
            }
            else
            {
                Debug.LogError("Phase 3A diary intro reference missing", this);
                hud.SetFade(1);
            }
            playerCollider.enabled = false;
            // The hall column screens Evelyn from the two students during this exchange.
            player.SetPositionAndRotation(new Vector3(-6.9f, .02f, -6.8f), Quaternion.Euler(0, 45, 0));
            playerCollider.enabled = true;
            adrian.SetPositionAndRotation(new Vector3(-22f, .09f, 15f), Quaternion.Euler(0, 90, 0));
            adrian.gameObject.SetActive(true);
            if (adrianHeldBook != null) adrianHeldBook.SetActive(false);
            isabelle.SetPositionAndRotation(new Vector3(-1.1f, 0, -1.3f), Quaternion.Euler(0, 90, 0));
            amelie.SetPositionAndRotation(new Vector3(1.2f, 0, -1.2f), Quaternion.Euler(0, -90, 0));
            isabelle.gameObject.SetActive(true); amelie.gameObject.SetActive(true);
            if (cameraBrain != null) cameraBrain.enabled = false;
            evelynMotion.StopMotion(); evelynMotion.enabled = false;
            if (normalPlayerAnimation != null) normalPlayerAnimation.enabled = true;
            lovesick.Suppressed = false;
            hud.SetFade(0);
            hud.ShowGameplay(true);
            SetStage(Phase2AStage.MissingStudent, "Listen to Isabelle and Amelie.");
            isabelleMotion.Face(amelie); amelieMotion.Face(isabelle);
            isabelleMotion.Talk(); amelieMotion.Talk();
            FrameEavesdrop(isabelle, amelie, -1);
            Speak(new[]
            {
                L("ISABELLE", "Did you hear? Another student disappeared."),
                L("AMELIE", "Lucien Morel. Nobody has seen him since Friday."),
                L("EVELYN", "Lucien? He was annoying."),
                L("ISABELLE", "Didn't he ask you out last week?"),
                L("EVELYN", "He asked me to study mathematics with him. I said no."),
                L("AMELIE", "That's the third disappearance this semester."),
                L("EVELYN", "People leave this academy all the time.")
            }, () =>
            {
                isabelleMotion.Idle(); amelieMotion.Idle();
                if (cameraBrain != null) cameraBrain.enabled = true;
                SetStage(Phase2AStage.Overhear, "Overhear Celeste's conversation.");
                game.SetPhase2AControl(true);
                StartCoroutine(IsabelleJoinsCeleste());
            }, line => FrameEavesdrop(isabelle, amelie, line == 2 || line == 4 || line == 6 ? 2 :
                line == 1 || line == 5 ? 1 : 0));
            cinematic = null;
        }

        private IEnumerator IsabelleJoinsCeleste()
        {
            yield return isabelleMotion.WalkRoute(new Vector3(-2.5f, 0, .7f), new Vector3(-4f, 0, 3.4f));
            celesteMotion.Face(isabelle); isabelleMotion.Face(celeste);
            celesteMotion.Talk(); isabelleMotion.Talk();
            isabelleReady = true;
        }

        private IEnumerator OverhearConversation()
        {
            SetStage(Phase2AStage.Overhearing, "");
            game.SetPhase2AControl(false);
            hud.SetFade(1);
            yield return new WaitForSeconds(.2f);
            PlacePlayerBehindCover(new Vector3(-6.8f, player.position.y, 6.8f), Quaternion.Euler(0, 135, 0));
            if (cameraBrain != null) cameraBrain.enabled = false;
            FrameEavesdrop(celeste, isabelle, -1);
            celesteMotion.Talk(); isabelleMotion.Talk();
            hud.SetFade(0);
            Speak(new[]
            {
                L("CELESTE", "I'm going to ask Adrian to the Black Rose Ball."),
                L("ISABELLE", "Seriously? Tomorrow?"),
                L("CELESTE", "Why not? He's single."),
                L("ISABELLE", "What if he refuses?"),
                L("CELESTE", "Then at least I'll know."),
                L("EVELYN · THOUGHT", "She's going to confess?"),
                L("EVELYN · THOUGHT", "No. He wouldn't accept… would he?"),
                L("EVELYN · THOUGHT", "Girls like her have always had everything."),
                L("EVELYN · THOUGHT", "Friends. Money. A family."),
                L("EVELYN · THOUGHT", "And now she wants him, too."),
                L("EVELYN · THOUGHT", "<color=#761B32>But not him.</color>")
            }, () => StartCoroutine(LeaveForLibrary()), line =>
                FrameEavesdrop(celeste, isabelle, line >= 5 ? 2 : line % 2 == 0 ? 0 : 1));
            yield break;
        }

        private IEnumerator LeaveForLibrary()
        {
            lovesick.Pulse(4f);
            hud.SetFade(1);
            game.SetPhase2AControl(false);
            yield return new WaitForSeconds(.55f);
            celeste.SetPositionAndRotation(new Vector3(-17.8f, 0, 14f), Quaternion.Euler(0, -90, 0));
            celesteMotion.Idle(); isabelleMotion.Idle();
            if (cameraBrain != null) cameraBrain.enabled = true;
            hud.SetFade(0);
            SetStage(Phase2AStage.WatchAdrian, "Watch Celeste talk to Adrian in the Library.");
            game.SetPhase2AControl(true);
        }

        private IEnumerator ObserveCelesteAndAdrian()
        {
            SetStage(Phase2AStage.Watching, "");
            game.SetPhase2AControl(false);
            lovesick.Suppressed = true;
            hud.SetFade(1);
            yield return new WaitForSeconds(.2f);
            PlacePlayerBehindCover(new Vector3(-22.2f, player.position.y, 10.7f), Quaternion.Euler(0, 45, 0));
            if (cameraBrain != null) cameraBrain.enabled = false;
            FrameEavesdrop(celeste, adrian, -1);
            hud.SetFade(0);
            yield return celesteMotion.WalkRoute(new Vector3(-20.3f, 0, 14.5f));
            celesteMotion.Face(adrian); adrianMotion.Face(celeste);
            celesteMotion.Talk(); adrianMotion.Talk();
            Speak(new[]
            {
                L("CELESTE", "Adrian! Are you attending the Ball tomorrow?"),
                L("ADRIAN", "I suppose so."),
                L("CELESTE", "With someone?"),
                L("ADRIAN", "Not yet."),
                L("CELESTE", "Then perhaps we could…"),
                L("ODETTE", "Adrian? Professor Laurent is waiting."),
                L("ADRIAN", "Excuse me, Celeste."),
                L("EVELYN · THOUGHT", "He doesn't have a partner."),
                L("EVELYN · THOUGHT", "She might actually ask him.")
            }, () => StartCoroutine(FinishObservation()), line =>
                FrameEavesdrop(celeste, adrian, line >= 7 ? 2 :
                    line == 1 || line == 3 || line == 6 ? 1 : 0));
        }

        private IEnumerator FinishObservation()
        {
            game.SetPhase2AControl(false);
            adrianMotion.Idle(); celesteMotion.Idle();
            // Stop short of the west wall shelves instead of walking into them.
            yield return adrianMotion.WalkRoute(new Vector3(-22.4f, adrian.position.y, 16.7f));
            yield return celesteMotion.WalkRoute(
                new Vector3(-19.15f, celeste.position.y, 13.15f),
                new Vector3(-17.2f, celeste.position.y, 13.15f),
                new Vector3(-15f, celeste.position.y, 15f),
                new Vector3(-15f, celeste.position.y, 17f));
            if (cameraBrain != null) cameraBrain.enabled = true;
            lovesick.Suppressed = false;
            SetStage(Phase2AStage.Errands, "Help students to improve your reputation. Reach 5 points to earn their trust. (0 / 5)");
            game.SetPhase2AControl(true);
        }

        private void RefreshErrandObjective()
        {
            if (Stage != Phase2AStage.Errands) return;
            if (reputation.Value >= reputation.TrustThreshold)
                game.SetPhase2AObjective("Speak to Celeste. She may trust you now.");
            else
                game.SetPhase2AObjective($"Help students: Library books, Art Studio materials, lost pendant ({reputation.Value} / 5).");
        }

        private void TalkToCeleste()
        {
            if (reputation.Value < reputation.TrustThreshold)
            {
                Speak(new[] { L("CELESTE", "Sorry, Evelyn. We don't really know each other.") });
                return;
            }
            celesteMotion.Talk();
            Speak(new[]
            {
                L("CELESTE", "Oh, Evelyn! I've heard you've been helping everyone today."),
                L("EVELYN", "I suppose."),
                L("CELESTE", "Actually… could you help me too?"),
                L("CELESTE", "I lost my black ribbon. I wanted to wear it to the Black Rose Ball.")
            }, () => { celesteMotion.Idle(); SetStage(Phase2AStage.RibbonSearch, "Find Celeste's black ribbon."); });
        }

        private void OpenSketchbook()
        {
            SketchbookSeen = true;
            sketchPage = 0;
            hud.HideClue();
            pagePanel.SetActive(true);
            game.SetPhase2AControl(false);
            RefreshPage();
        }
        private void ChangePage(int change) { sketchPage = Mathf.Clamp(sketchPage + change, 0, sketchPages.Length - 1); RefreshPage(); }
        private void RefreshPage()
        {
            pageTitle.text = $"A. V.   ·   {sketchPage + 1} / {sketchPages.Length}";
            pageBody.text = sketchPages[sketchPage];
            if (pagePrevious != null) pagePrevious.interactable = sketchPage > 0;
            if (pageNext != null) pageNext.interactable = sketchPage < sketchPages.Length - 1;
        }
        private void CloseSketchbook()
        {
            pagePanel.SetActive(false);
            game.SetPhase2AControl(true);
            Speak(new[]
            {
                L("EVELYN · THOUGHT", "Who drew these?"),
                L("EVELYN · THOUGHT", "This is ridiculous."),
                L("EVELYN · THOUGHT", "Adrian would never…"),
                L("EVELYN · THOUGHT", "I only love Adrian.")
            });
        }

        private void ReturnRibbon()
        {
            celesteMotion.Talk();
            Speak(new[]
            {
                L("CELESTE", "You found it! Thank you, Evelyn."),
                L("EVELYN", "You're welcome."),
                L("CELESTE", "I don't know what I would've done without you."),
                L("EVELYN", "Actually… I think I saw something else of yours in the Art Studio."),
                L("CELESTE", "Really?"),
                L("EVELYN", "Would you like to come with me?"),
                L("CELESTE", "Of course.")
            }, () =>
            {
                HasRibbon = false;
                celesteMotion.Idle();
                SetStage(Phase2AStage.FollowCeleste, "Lead Celeste to the Art Studio through the Grand Hall.");
                follower.Begin();
            });
        }

        private void PersuadeWitness()
        {
            witnessMotion.Talk();
            Speak(new[]
            {
                L("EVELYN", "Professor Laurent needs you in the Gallery."),
                L("MARGOT", "Right now?"),
                L("EVELYN", "She said it was urgent."),
                L("MARGOT", "Alright. I'll go.")
            }, () => StartCoroutine(WitnessExit()));
        }
        private IEnumerator WitnessExit()
        {
            SetStage(Phase2AStage.WitnessLeaving, "Wait for Margot to leave the Art Studio.");
            witnessMotion.Idle();
            yield return witnessMotion.WalkRoute(new Vector3(-14f, 0, -2f), new Vector3(-10f, 0, -2f), new Vector3(-8f, 0, -2f));
            SetStage(Phase2AStage.CloseDoor, "Close the Art Studio door.");
        }
        private IEnumerator CloseStudioDoor()
        {
            SetStage(Phase2AStage.WitnessLeaving, "Closing the Art Studio door…");
            game.SetPhase2AControl(false);
            yield return studioDoor.CloseVisible();
            SetStage(Phase2AStage.Confront, "Speak to Celeste.");
            game.SetPhase2AControl(true);
        }
        private void ConfrontCeleste()
        {
            if (confrontationStarting) return;
            confrontationStarting = true;
            StartCoroutine(ConfrontationOpening());
        }

        private IEnumerator ConfrontationOpening()
        {
            game.SetPhase2AControl(false);
            celesteMotion.Face(player); celesteMotion.Talk();
            var towardsCeleste = celeste.position - player.position; towardsCeleste.y = 0;
            if (towardsCeleste.sqrMagnitude > .01f) player.rotation = Quaternion.LookRotation(towardsCeleste);
            if (cameraBrain != null) cameraBrain.enabled = false;
            FrameConfrontation(-1);
            yield return new WaitForSeconds(.85f);
            Speak(new[]
            {
                L("CELESTE", "Where is the other thing you wanted to show me?"),
                L("EVELYN", "Celeste… do you love Adrian?"),
                L("CELESTE", "What?"),
                L("EVELYN", "Answer me."),
                L("CELESTE", "I barely know him. I just wanted to ask him to the Ball."),
                L("EVELYN", "You don't deserve him."),
                L("CELESTE", "Evelyn, you're scaring me.")
            }, () =>
            {
                confrontationStarting = false;
                celesteMotion.Idle();
                SetStage(Phase2AStage.Complete, "");
                if (phase2B != null) phase2B.Begin();
                else
                {
                    if (cameraBrain != null) cameraBrain.enabled = true;
                    game.SetPhase2AControl(false);
                    completeText.text = "PHASE 2A COMPLETE";
                    completionPanel.SetActive(true);
                }
            }, FrameConfrontation);
        }

        private void FrameConfrontation(int line)
        {
            if (gameplayCamera == null) return;
            var between = (player.position + celeste.position) * .5f;
            var axis = celeste.position - player.position; axis.y = 0;
            if (axis.sqrMagnitude < .01f) axis = player.forward;
            axis.Normalize();
            var side = Vector3.Cross(Vector3.up, axis);
            Vector3 position, target;
            float fov;
            if (line < 0) // Establish the empty studio before Celeste speaks.
            { position = between + side * 3.8f - axis * 1.1f + Vector3.up * 2.05f;
              target = between + Vector3.up * 1.15f; fov = 49f; }
            else if (line == 0)
            { position = celeste.position - axis * 1.7f + side * .45f + Vector3.up * 1.62f;
              target = celeste.position + Vector3.up * 1.38f; fov = 39f; }
            else if (line == 1 || line == 3 || line == 5)
            { position = player.position + axis * 1.7f - side * .45f + Vector3.up * 1.62f;
              target = player.position + Vector3.up * 1.38f; fov = 39f; }
            else if (line == 2 || line == 4)
            { position = celeste.position - axis * 1.7f + side * .45f + Vector3.up * 1.62f;
              target = celeste.position + Vector3.up * 1.38f; fov = 39f; }
            else
            { position = between + side * 3.2f + Vector3.up * 1.75f;
              target = between + Vector3.up * 1.25f; fov = 45f; }
            // Keep every shot inside the Studio, including when the player begins
            // the conversation near a wall rather than at its centre.
            position.x = Mathf.Clamp(position.x, -22.7f, -13.4f);
            position.z = Mathf.Clamp(position.z, -6.7f, 2.7f);
            Frame(position, target, fov);
        }

        private void PlacePlayerBehindCover(Vector3 position, Quaternion rotation)
        {
            if (playerCollider != null) playerCollider.enabled = false;
            player.SetPositionAndRotation(position, rotation);
            if (playerCollider != null) playerCollider.enabled = true;
        }

        private void FrameEavesdrop(Transform first, Transform second, int subject)
        {
            if (gameplayCamera == null) return;
            if (Stage == Phase2AStage.MissingStudent)
            {
                if (subject == 2)
                    Frame(new Vector3(-5.1f, 1.7f, -7.25f), player.position + Vector3.up * 1.3f, 42f);
                else if (subject < 0)
                    Frame(new Vector3(-2.3f, 1.85f, -4.2f), new Vector3(0, 1.2f, -1.35f), 47f);
                else
                    Frame(new Vector3(first.position.x + (subject == 0 ? -.35f : 1.2f), 1.7f, -3.75f),
                        (subject == 0 ? first : second).position + Vector3.up * 1.32f, 40f);
                return;
            }
            if (subject == 2)
            {
                var offset = Stage == Phase2AStage.Watching ? new Vector3(1.7f, 1.7f, -1.4f) :
                    new Vector3(1.65f, 1.7f, -1.9f);
                Frame(player.position + offset, player.position + Vector3.up * 1.35f, 40f);
                return;
            }
            if (subject < 0)
            {
                var center = (first.position + second.position) * .5f;
                var fromCover = player.position - center;
                fromCover.y = 0;
                Frame(center + fromCover.normalized * 3.4f + Vector3.up * 2.05f,
                    center + Vector3.up * 1.25f, 48f);
                return;
            }
            var actor = subject == 0 ? first : second;
            var lateral = Vector3.Cross(Vector3.up, actor.forward);
            Frame(actor.position + actor.forward * 1.7f + lateral * .55f + Vector3.up * 1.65f,
                actor.position + Vector3.up * 1.38f, 38f);
        }

        private void Frame(Vector3 position, Vector3 target, float fov)
        {
            gameplayCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            gameplayCamera.fieldOfView = fov;
        }
        private static IEnumerator MoveActor(Transform actor, Vector3 destination, float speed)
        {
            while (Vector3.Distance(actor.position, destination) > .06f)
            {
                actor.position = Vector3.MoveTowards(actor.position, destination, speed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
