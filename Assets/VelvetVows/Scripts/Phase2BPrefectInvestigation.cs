using System.Collections;
using System.Linq;
using UnityEngine;

namespace VelvetVows
{
    public sealed class Phase2BPrefectInvestigation : MonoBehaviour
    {
        public Phase2BCleanupManager cleanup;
        public PatrolNPC normalPatrol;
        public Transform player, body, bloodPool;
        public Phase2AStudioDoor studioDoor;
        public GameObject attackProp, clothingMarks;
        public Animator animator;
        [Min(.1f)] public float approachSpeed = .9f;
        [Min(2f)] public float sightDistance = 7f;
        [Range(10, 130)] public float viewAngle = 100f;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool investigating, approaching;
        private bool warningActive, firstWarningShown;
        private bool footstepsWarning, doorKnock;
        private float nextBumpAt;
        private GameManager game;
        private Phase2AStoryManager story;
        private DialogueManager dialogue;
        private GameHUD hud;
        public bool FirstWarningShown => firstWarningShown;
        public bool WarningActive => warningActive;
        private int waypointIndex;
        private static readonly Vector3[] Approach =
        {
            new Vector3(5f, 0, 4f), new Vector3(5f, 0, -2f), new Vector3(0f, 0, -2f),
            new Vector3(-8f, 0, -2f), new Vector3(-11.2f, 0, -2f)
        };
        private void Awake()
        {
            originalPosition = transform.position; originalRotation = transform.rotation;
            game = FindFirstObjectByType<GameManager>();
            story = FindFirstObjectByType<Phase2AStoryManager>();
            dialogue = FindFirstObjectByType<DialogueManager>();
            hud = FindFirstObjectByType<GameHUD>();
        }
        public void BeginInvestigation()
        {
            if (normalPatrol != null) normalPatrol.enabled = true;
            investigating = true; approaching = false; waypointIndex = 0;
            footstepsWarning = doorKnock = false;
            if (animator != null) animator.SetFloat("Speed", 0);
        }
        public void Tick(float elapsed)
        {
            if (!investigating || cleanup == null || !cleanup.Active) return;
            CheckEvidence();
            if (!cleanup.Active) return;
            if (elapsed >= 48f && !footstepsWarning)
            { footstepsWarning = true; hud?.ShowClue("Footsteps in the corridor. Someone is coming toward the Studio."); }
            if (elapsed >= 62f && !doorKnock)
            { doorKnock = true; hud?.ShowClue("A knock at the Studio door. Hide the evidence before anyone enters."); }
            if (elapsed >= 78f && !approaching)
            {
                approaching = true;
                if (normalPatrol != null) normalPatrol.enabled = false;
                waypointIndex = Vector3.Distance(transform.position, Approach[0]) <= Vector3.Distance(transform.position, Approach[1]) ? 0 : 1;
            }
        }
        private void Update()
        {
            CheckEarlyEncounter();
            if (!investigating || !approaching || cleanup == null || !cleanup.Active) return;
            if (waypointIndex >= Approach.Length && (studioDoor == null || studioDoor.Closed))
            { if (animator != null) animator.SetFloat("Speed", 0); return; }
            var destination = waypointIndex < Approach.Length ? Approach[waypointIndex] : new Vector3(-14.1f, 0, -3f);
            destination.y = transform.position.y;
            var delta = destination - transform.position;
            if (delta.sqrMagnitude < .1f)
            { if (waypointIndex < Approach.Length) waypointIndex++; else if (animator != null) animator.SetFloat("Speed", 0); return; }
            transform.position = Vector3.MoveTowards(transform.position, destination, approachSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), Time.deltaTime * 6f);
            if (animator != null) animator.SetFloat("Speed", approachSpeed);
        }
        private void CheckEarlyEncounter()
        {
            if (investigating || warningActive || game == null || story == null || player == null ||
                game.State != GameState.Phase2A || DialogueManager.IsOpen || Time.time < nextBumpAt) return;
            var stage = story.Stage;
            if (stage != Phase2AStage.Overhear && stage != Phase2AStage.WatchAdrian &&
                stage != Phase2AStage.Errands && stage != Phase2AStage.RibbonSearch &&
                stage != Phase2AStage.RibbonReturn) return;
            var distance = player.position - transform.position; distance.y = 0;
            if (distance.sqrMagnitude > 1.55f * 1.55f) return;
            nextBumpAt = Time.time + 12f;
            if (firstWarningShown) { FacePlayer(); return; }
            firstWarningShown = true;
            StartCoroutine(FirstWarning());
        }
        private IEnumerator FirstWarning()
        {
            warningActive = true;
            if (normalPatrol != null) normalPatrol.enabled = false;
            game.SetPhase2AControl(false);
            FacePlayer();
            var away = player.position - transform.position; away.y = 0;
            if (away.sqrMagnitude > .01f)
            {
                away.Normalize();
                var controller = player.GetComponent<CharacterController>();
                for (float t = 0; t < .35f; t += Time.deltaTime)
                {
                    if (game.State != GameState.Phase2A) yield break;
                    transform.position += away * (.18f / .35f) * Time.deltaTime;
                    if (controller != null && controller.enabled) controller.Move(away * (.16f / .35f) * Time.deltaTime);
                    yield return null;
                }
            }
            bool finished = false;
            dialogue.BeginScripted(new[]
            {
                new DialogueLine("PREFECT", "Watch where you're going."),
                new DialogueLine("PREFECT", "Know your place. This academy has rules.")
            }, () => finished = true);
            while (!finished && game.State == GameState.Phase2A) yield return null;
            if (game.State == GameState.Phase2A)
            {
                game.SetPhase2AControl(false);
                hud?.ShowClue("PREFECTS ENFORCE DISCIPLINE\nSuspicious behaviour increases your risk of being caught.");
                yield return new WaitForSeconds(3.5f);
                hud?.HideClue();
                game.SetPhase2AControl(true);
            }
            if (!investigating && normalPatrol != null) normalPatrol.enabled = true;
            warningActive = false;
        }
        private void CheckEvidence()
        {
            if (cleanup == null || !cleanup.Active || body == null || player == null) return;
            var observers = new[] { transform, story?.witness, story?.isabelle, story?.amelie,
                story?.clara, story?.margot, story?.odette };
            foreach (var observer in observers)
            {
                if (observer == null || !observer.gameObject.activeInHierarchy) continue;
                bool seesPlayer = CanSee(observer, player.position + Vector3.up * 1.1f, player, 5.5f);
                bool seesBody = !cleanup.BodyHidden && CanSee(observer, body.position + Vector3.up * .38f, body, 6f);
                if (seesBody)
                { cleanup.Fail("Celeste was found."); return; }
                if (seesPlayer && (attackProp.activeSelf || clothingMarks.activeSelf || cleanup.StainedCloth))
                { cleanup.Fail("The Prefect saw evidence on Evelyn."); return; }
                if (seesPlayer && !cleanup.BodyHidden && Vector3.Distance(player.position, body.position) < 1.8f)
                { cleanup.Fail("Evelyn was found beside Celeste."); return; }
                if (seesPlayer && cleanup.IsCleaningBlood)
                { cleanup.Fail("Evelyn was seen cleaning blood."); return; }
                if (!cleanup.BloodCleaned && bloodPool != null &&
                    CanSee(observer, bloodPool.position + Vector3.up * .12f, bloodPool, 5f))
                { cleanup.Fail("The blood was discovered."); return; }
            }
        }
        private bool CanSee(Transform observer, Vector3 target, Transform targetRoot, float maxDistance)
        {
            var origin = observer.position + Vector3.up * 1.45f;
            var ray = target - origin;
            float distance = ray.magnitude;
            if (distance < .1f || distance > maxDistance) return false;
            if (studioDoor != null && studioDoor.Closed && observer.position.x > -12f && target.x < -12.5f) return false;
            var flat = ray; flat.y = 0;
            if (flat.sqrMagnitude > .01f && Vector3.Angle(observer.forward, flat) > viewAngle * .5f) return false;
            foreach (var hit in Physics.RaycastAll(origin, ray.normalized, distance - .05f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.collider.transform.IsChildOf(observer) || hit.collider.transform.IsChildOf(targetRoot)) continue;
                return false;
            }
            return true;
        }
        private bool CanSee(Vector3 target, float distanceOverride = -1)
        {
            var origin = transform.position + Vector3.up * 1.45f;
            var ray = target - origin;
            float distance = ray.magnitude;
            if (distance < .01f || distance > (distanceOverride > 0 ? distanceOverride : sightDistance)) return false;
            if (target.x < -12f && studioDoor != null && studioDoor.Closed) return false;
            var horizontal = ray; horizontal.y = 0;
            if (horizontal.sqrMagnitude > .01f && Vector3.Angle(transform.forward, horizontal) > viewAngle * .5f) return false;
            foreach (var hit in Physics.RaycastAll(origin, ray.normalized, distance - .08f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.transform.IsChildOf(player) && Vector3.Distance(target, player.position) < 2f) return true;
                return false;
            }
            return true;
        }
        public void FacePlayer()
        {
            var delta = player.position - transform.position; delta.y = 0;
            if (delta.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(delta.normalized);
            if (animator != null) animator.SetFloat("Speed", 0);
        }
        public void StopInvestigation()
        { investigating = false; approaching = false; if (normalPatrol != null) normalPatrol.enabled = false;
          if (animator != null) animator.SetFloat("Speed", 0); }
        public void ResetInvestigation()
        {
            StopInvestigation();
            footstepsWarning = doorKnock = false;
            transform.SetPositionAndRotation(originalPosition, originalRotation);
            if (normalPatrol != null) normalPatrol.ResetRoute();
            waypointIndex = 0;
        }
    }
}
