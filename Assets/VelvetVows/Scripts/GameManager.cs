using System.Collections;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelvetVows
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private ThirdPersonController playerMovement;
        [SerializeField] private AcademyPlayerInputRelay playerInput;
        [SerializeField] private GameHUD hud;
        [SerializeField] private CelesteSequence celeste;
        [SerializeField] private SuspicionSystem suspicion;
        [SerializeField] private GameTimer timer;
        [SerializeField] private EvidenceManager evidence;
        [SerializeField] private BlackRoseSpawner roses;
        [SerializeField] private DoorController ballroomDoor;
        [SerializeField] private Transform endingEvelynPoint;
        [SerializeField] private Transform endingAdrianPoint;
        [SerializeField] private Transform adrian;
        [SerializeField] private float fadeSeconds = 0.75f;
        [SerializeField] private MusicManager music;
        [SerializeField] private Phase2AStoryManager phase2A;

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Introduction;
        public Transform Player => playerMovement != null ? playerMovement.transform : null;
        public int RoseCount { get; private set; }
        public string Objective { get; private set; } = "";
        private string dossierPreviousObjective;

        public void Configure(ThirdPersonController movement, AcademyPlayerInputRelay input, GameHUD gameHud,
            CelesteSequence celesteSequence, SuspicionSystem suspicionSystem, GameTimer gameTimer,
            EvidenceManager evidenceManager, BlackRoseSpawner roseSpawner, DoorController door)
        {
            playerMovement = movement;
            playerInput = input;
            hud = gameHud;
            celeste = celesteSequence;
            suspicion = suspicionSystem;
            timer = gameTimer;
            evidence = evidenceManager;
            roses = roseSpawner;
            ballroomDoor = door;
        }

        public void ConfigureEnding(Transform evelynPoint, Transform adrianPoint, Transform adrianActor)
        {
            endingEvelynPoint = evelynPoint;
            endingAdrianPoint = adrianPoint;
            adrian = adrianActor;
        }
        public void ConfigurePhase2A(Phase2AStoryManager story) => phase2A = story;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            State = GameState.Introduction;
            SetControl(false);
            Debug.Log("[VelvetVows] GameManager Awake", this);
        }

        private void Start()
        {
            SetControl(false);
            timer.Stop();
            suspicion.Stop();
            hud.EnforceIntroductionUI();
            hud.EnsureBeginListener(this);
            hud.SetInteraction("");
            if (phase2A != null)
            {
                hud.SetReputation(0, 5);
                hud.SetTimerVisible(false);
            }
            else hud.SetEvidence(0, evidence.Total);
            hud.SetRoses(0, roses.SpawnCount);
            hud.SetSuspicion(0);
            hud.SetTimer(timer.Remaining);
            Debug.Log("[VelvetVows] State: " + State, this);
            hud.LogStartScreenState();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (State != GameState.Introduction) return;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }

        public void Begin()
        {
            if (State != GameState.Introduction) return;
            Time.timeScale = 1;
            hud.ShowStart(false);
            hud.ShowGameplay(phase2A == null);
            if (phase2A != null)
            {
                ChangeState(GameState.Phase2A, "");
                phase2A.BeginOpening();
                music?.PlayExploration();
                return;
            }
            SetControl(true);
            ChangeState(GameState.FindCeleste, "Find Celeste before the Black Rose Ball.");
            music?.PlayExploration();
            Debug.Log("[VelvetVows] BEGIN clicked. State: " + State, this);
        }

        public void MetCeleste()
        {
            if (State != GameState.FindCeleste) return;
            ChangeState(GameState.FollowCeleste, "Follow Celeste to the Art Studio.");
            celeste.BeginRoute();
        }

        public void EnteredStudio()
        {
            if (State != GameState.FollowCeleste || !celeste.Arrived) return;
            StartCoroutine(StudioStoryEvent());
        }

        private IEnumerator StudioStoryEvent()
        {
            ChangeState(GameState.MurderEvent, "");
            SetControl(false);
            hud.SetInteraction("");
            yield return Fade(1);
            celeste.StageAfterEvent();
            yield return new WaitForSeconds(.8f);
            yield return Fade(0);
            suspicion.Begin();
            timer.Begin();
            music?.PlayInvestigation();
            ChangeState(GameState.EscapeStudio, "DON'T GET CAUGHT.");
            SetControl(true);
        }

        public void ExitedStudio()
        {
            if (State == GameState.EscapeStudio)
                ChangeState(GameState.InvestigateGallery, "Search the Restricted Gallery for evidence.");
        }

        public void AllEvidenceFound()
        {
            if (State != GameState.InvestigateGallery && State != GameState.EscapeStudio) return;
            ballroomDoor.Open();
            ChangeState(GameState.ReachBallroom, "Reach the Ballroom.");
        }

        public void EnteredBallroom()
        {
            if (State == GameState.ReachBallroom) StartCoroutine(BallroomEnding());
        }

        private IEnumerator BallroomEnding()
        {
            ChangeState(GameState.Victory, "");
            music?.PlayFinale();
            timer.Stop();
            suspicion.Stop();
            SetControl(false);
            hud.SetInteraction("");
            if (!PrepareEnding()) yield break;
            yield return null;
            string[] lines = {
                "ADRIAN: You made it.", "EVELYN: Of course.", "ADRIAN: Did she suffer?",
                "ADRIAN: Celeste.", "ADRIAN: Lucien. Thomas. Henry.",
                "ADRIAN: They kept looking at you.", "ADRIAN: So I made sure they stopped.",
                "ADRIAN: You did it for me.", "EVELYN: And you did it for me."
            };
            foreach (string line in lines)
            {
                hud.ShowDialogue(line);
                yield return new WaitForSeconds(1.7f);
            }
            hud.HideDialogue();
            yield return Fade(1);
            hud.ShowVictory();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private bool PrepareEnding()
        {
            if (endingEvelynPoint == null || endingAdrianPoint == null || adrian == null)
            {
                Debug.LogError("Ending character anchors or Adrian reference missing.", this);
                return false;
            }
            var inputs = playerMovement.GetComponent<StarterAssetsInputs>();
            inputs.MoveInput(Vector2.zero);
            inputs.LookInput(Vector2.zero);
            inputs.SprintInput(false);
            inputs.JumpInput(false);
            var character = playerMovement.GetComponent<CharacterController>();
            character.enabled = false;
            Vector3 evelynPosition = endingEvelynPoint.position;
            Vector3 adrianPosition = endingAdrianPoint.position;
            Vector3 faceAdrian = adrianPosition - evelynPosition;
            faceAdrian.y = 0;
            playerMovement.transform.SetPositionAndRotation(evelynPosition,
                Quaternion.LookRotation(faceAdrian.normalized, Vector3.up));
            character.enabled = true;
            adrian.SetPositionAndRotation(adrianPosition,
                Quaternion.LookRotation(-faceAdrian.normalized, Vector3.up));
            playerMovement.GetComponent<AcademyCharacterAnimation>().EnterCinematicIdle();
            var adrianAnimator = adrian.GetComponentInChildren<Animator>();
            if (adrianAnimator != null)
            {
                adrianAnimator.SetFloat("Speed", 0);
                adrianAnimator.CrossFade("Idle / Walk / Run", .12f);
            }
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera missing for ballroom ending.", this);
                return false;
            }
            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain != null) brain.enabled = false;
            Vector3 midpoint = (evelynPosition + adrianPosition) * .5f;
            Vector3 cameraSide = Vector3.Cross(Vector3.up, faceAdrian.normalized);
            mainCamera.transform.position = midpoint + cameraSide * 2.45f + Vector3.up * 1.6f;
            mainCamera.transform.rotation = Quaternion.LookRotation(midpoint + Vector3.up * 1.2f - mainCamera.transform.position);
            mainCamera.fieldOfView = 42;
            Physics.SyncTransforms();
            return true;
        }

        public void Defeat(string reason)
        {
            if (State == GameState.Victory || State == GameState.Defeat || State == GameState.Introduction) return;
            ChangeState(GameState.Defeat, "");
            timer.Stop();
            suspicion.Stop();
            SetControl(false);
            hud.SetInteraction("");
            hud.ShowDefeat(reason);
            FindFirstObjectByType<CharacterVoiceManager>()?.PlayGameOver();
        }

        public void RoseCollected()
        {
            RoseCount++;
            hud.SetRoses(RoseCount, roses.SpawnCount);
        }

        public void RefreshEvidence(int count) => hud.SetEvidence(count, evidence.Total);
        public void RefreshSuspicion(float value) => hud.SetSuspicionWithSource(value, suspicion != null ? suspicion.DetectionSource : null);
        public void RefreshTimer(float remaining) => hud.SetTimer(remaining);
        public void SetMusic(MusicManager value) => music = value;
        public void SetPhase2AObjective(string objective)
        {
            if (State == GameState.Phase2A) ChangeState(GameState.Phase2A, objective);
        }
        public void SetPhase2AControl(bool enabled)
        {
            if (State == GameState.Phase2A) SetControl(enabled);
        }
        public void SetPhase3AIntro(bool active)
        {
            if (active)
            {
                ChangeState(GameState.Phase3AIntro, "");
                SetControl(false);
                hud.SetInteraction("");
                hud.ShowGameplay(false);
            }
            else if (State == GameState.Phase3AIntro)
                ChangeState(GameState.Phase2A, "");
        }
        public void SetPhase3BDossier(bool active)
        {
            if (active && State == GameState.Phase2A)
            {
                dossierPreviousObjective = Objective;
                ChangeState(GameState.Phase3BDossier, "");
                SetControl(false);
                hud.SetInteraction("");
                hud.HideClue();
                hud.ShowGameplay(false);
            }
            else if (!active && State == GameState.Phase3BDossier)
            {
                ChangeState(GameState.Phase2A, dossierPreviousObjective ?? "");
                hud.ShowGameplay(true);
                SetControl(true);
            }
        }
        public void BeginPhase2BCinematic()
        {
            ChangeState(GameState.Phase2BCinematic, "");
            SetControl(false);
            hud.SetInteraction("");
        }
        public void BeginPhase2BCleanup()
        {
            ChangeState(GameState.Phase2BCleanup, "CLEAN UP BEFORE SOMEONE FINDS HER");
            hud.ShowGameplay(true);
            hud.SetTimerVisible(true);
            SetControl(true);
        }
        public void SetPhase2BObjective(string objective)
        {
            if (State == GameState.Phase2BCleanup) ChangeState(GameState.Phase2BCleanup, objective);
        }
        public void SetPhase2BControl(bool enabled)
        {
            if (State == GameState.Phase2BCleanup || State == GameState.Phase2BCinematic) SetControl(enabled);
        }
        public void EndPhase2B()
        {
            ChangeState(GameState.Phase2BOutcome, "");
            SetControl(false);
            hud.SetInteraction("");
            hud.SetTimerVisible(false);
        }
        public void BeginPhase2CBall()
        {
            ChangeState(GameState.Phase2CBall, "");
            SetControl(false);
            hud.ShowGameplay(false);
            hud.HideClue();
            hud.SetTimerVisible(false);
        }
        public void EndPhase2C()
        {
            ChangeState(GameState.Phase2CEnding, "");
            SetControl(false);
        }
        public void SetDialogueControl(bool talking)
        {
            if (State == GameState.Introduction || State == GameState.Phase3AIntro ||
                State == GameState.Phase3BDossier || State == GameState.MurderEvent ||
                State == GameState.Victory || State == GameState.Defeat) return;
            SetControl(!talking);
            if (talking) hud.SetInteraction("");
        }
        public void AdjustSuspicion(float amount) => suspicion?.Adjust(amount);
        public void ShowClue(string clue) => hud.ShowClue(clue);
        public void SetInteraction(string prompt) => hud.SetInteraction(prompt);

        public void Restart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ChangeState(GameState next, string objective)
        {
            State = next;
            Objective = objective;
            hud.SetObjective(objective);
        }

        private void SetControl(bool enabled)
        {
            var action = playerMovement != null ? playerMovement.GetComponent<AcademyActionPerformance>() : null;
            if (enabled && action != null && action.IsBusy) enabled = false;
            // StarterAssetsInputs.OnApplicationFocus reapplies this flag. Keep it aligned
            // with the menu state so refocusing the Game View cannot lock the UI cursor.
            var starterInputs = playerMovement != null ? playerMovement.GetComponent<StarterAssetsInputs>() : null;
            if (starterInputs != null) starterInputs.cursorLocked = enabled;
            if (!enabled && starterInputs != null) starterInputs.MoveInput(Vector2.zero);
            var characterAnimation = playerMovement != null ? playerMovement.GetComponent<AcademyCharacterAnimation>() : null;
            if (characterAnimation != null)
            {
                if (enabled) characterAnimation.ExitCinematicIdle();
                else characterAnimation.EnterCinematicIdle();
            }
            if (playerMovement != null) playerMovement.enabled = enabled;
            if (playerInput != null) playerInput.enabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }

        private IEnumerator Fade(float destination)
        {
            float start = hud.FadeAlpha;
            float elapsed = 0;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                hud.SetFade(Mathf.Lerp(start, destination, elapsed / fadeSeconds));
                yield return null;
            }
            hud.SetFade(destination);
        }
    }
}
