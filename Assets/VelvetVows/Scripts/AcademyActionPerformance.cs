using System;
using System.Collections;
using UnityEngine;

namespace VelvetVows
{
    public enum ActionKind { Pickup, Give, Door, Inspect, CleanFloor, WashClothes, HideBody, DisposeEvidence }

    public sealed class AcademyActionPerformance : MonoBehaviour
    {
        private AcademyBodyPerformance body, receiver;
        private GameManager game;
        private Coroutine sequence;
        private GameState beganIn;
        private Transform held, heldParent;
        private Vector3 heldPosition, heldScale;
        private Quaternion heldRotation;
        private Action completed;
        private GameObject carriedVisual;
        public bool IsBusy { get; private set; }
        public bool Prepared { get; private set; }
        public int CompletedActions { get; private set; }
        public int Impacts { get; private set; }
        public ActionKind CurrentAction { get; private set; }
        private void Awake() { body = GetComponentInChildren<AcademyBodyPerformance>(true); game = FindFirstObjectByType<GameManager>(); }
        public bool PlayAction(ActionKind kind, Transform target, Action onImpact, Action onComplete)
        {
            if (IsBusy || !isActiveAndEnabled || target == null || body == null) return false;
            IsBusy = true; Prepared = false; CurrentAction = kind; beganIn = game.State; completed = onComplete;
            SetControl(false); sequence = StartCoroutine(Perform(kind, target, onImpact)); return true;
        }
        public static float Duration(ActionKind kind) => kind switch
        { ActionKind.CleanFloor => 2.2f, ActionKind.WashClothes => 4.9f, ActionKind.HideBody => 7f, ActionKind.Inspect => .8f, _ => .95f };
        private Vector3 TargetPoint(Transform target, ActionKind kind)
        {
            var anchor = target.GetComponent<InteractionFocusAnchor>();
            if (anchor != null) return anchor.Point;
            if (kind == ActionKind.Give) return target.position + Vector3.up * 1.05f;
            var renderer = target.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.center : target.position;
        }
        private IEnumerator Perform(ActionKind kind, Transform target, Action impact)
        {
            var controller = GetComponent<CharacterController>();
            var animation = GetComponent<AcademyCharacterAnimation>();
            if (kind == ActionKind.Pickup || kind == ActionKind.Give || kind == ActionKind.Door || kind == ActionKind.Inspect || kind == ActionKind.CleanFloor)
            {
                for (float t = 0; t < .8f; t += Time.deltaTime)
                {
                    if (game.State != beganIn || target == null) { Cancel(); yield break; }
                    Vector3 delta = TargetPoint(target, kind) - transform.position; delta.y = 0;
                    float distance = delta.magnitude;
                    float standOff = kind == ActionKind.Give ? .85f : kind == ActionKind.CleanFloor ? .4f : .6f;
                    if (distance <= standOff || controller == null || !controller.enabled) break;
                    animation?.SetActionSpeed(2.2f);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),240*Time.deltaTime);
                    controller.Move(delta.normalized * Mathf.Min(distance - standOff,Time.deltaTime * 2.2f));
                    yield return null;
                }
                animation?.SetActionSpeed(0);
            }
            Prepared = true;
            bool hit = false;
            float duration = Duration(kind);
            receiver = kind == ActionKind.Give ? target.GetComponentInChildren<AcademyBodyPerformance>() : null;
            var carriedPerson = kind == ActionKind.HideBody ? target.GetComponentInChildren<Animator>(true) : null;
            var rightGrip = carriedPerson != null && carriedPerson.isHuman ? carriedPerson.GetBoneTransform(HumanBodyBones.RightUpperArm) : null;
            var leftGrip = carriedPerson != null && carriedPerson.isHuman ? carriedPerson.GetBoneTransform(HumanBodyBones.LeftUpperArm) : null;
            var rightElbow = carriedPerson != null && carriedPerson.isHuman ? carriedPerson.GetBoneTransform(HumanBodyBones.RightLowerArm) : null;
            var leftElbow = carriedPerson != null && carriedPerson.isHuman ? carriedPerson.GetBoneTransform(HumanBodyBones.LeftLowerArm) : null;
            bool useRightArm = rightGrip != null && leftGrip != null &&
                (rightGrip.position - transform.position).sqrMagnitude < (leftGrip.position - transform.position).sqrMagnitude;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                if (game == null || game.State != beganIn || target == null) { Cancel(); yield break; }
                float t = time / duration;
                Vector3 point = TargetPoint(target, kind);
                Vector3 direction = point - transform.position; direction.y = 0;
                if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 240 * Time.deltaTime);
                float release = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.78f, 1, t));
                float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .28f)) * release;
                float crouch = kind == ActionKind.CleanFloor || kind == ActionKind.HideBody ? weight : Mathf.Clamp01((.8f - point.y + transform.position.y) * .5f) * weight;
                if (kind == ActionKind.CleanFloor) point += transform.right * (Mathf.Sin(time * 9) * .12f);
                if (kind == ActionKind.WashClothes) point = transform.position + transform.forward * .35f + Vector3.up * .95f + transform.right * Mathf.Sin(time * 8) * .06f;
                Vector3? leftPoint = kind == ActionKind.WashClothes || kind == ActionKind.HideBody ? point - transform.right * .15f : null;
                if (kind == ActionKind.HideBody && rightGrip != null && leftGrip != null && rightElbow != null && leftElbow != null)
                {
                    // Both hands support the closer upper arm. Reaching across to the far shoulder
                    // made the second arm too short to maintain contact with a prone character.
                    var shoulder = useRightArm ? rightGrip : leftGrip;
                    var elbow = useRightArm ? rightElbow : leftElbow;
                    point = Vector3.Lerp(shoulder.position, elbow.position, .12f);
                    leftPoint = Vector3.Lerp(shoulder.position, elbow.position, .34f);
                    weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .45f)) * release;
                    crouch = weight;
                }
                body.SetHands(point, leftPoint, weight, crouch);
                body.SetDeepCrouch(kind == ActionKind.HideBody);
                if (kind == ActionKind.HideBody && rightGrip != null && leftGrip != null && rightElbow != null && leftElbow != null)
                    body.SetBodyGrip(useRightArm ? rightGrip : leftGrip, useRightArm ? rightElbow : leftElbow, .12f, .34f);
                if (receiver != null) receiver.SetHands(transform.position + transform.forward * .35f + Vector3.up * 1.05f, null, weight * .85f);
                if (kind == ActionKind.Pickup && !hit && t > .35f && held == null && target.GetComponentInChildren<Animator>() == null)
                {
                    held = target; heldParent = held.parent; heldPosition = held.localPosition; heldRotation = held.localRotation; heldScale = held.localScale;
                }
                if (held != null && !hit)
                {
                    var hand = body.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
                    held.position = Vector3.Lerp(held.position, hand.position, Time.deltaTime * 15);
                }
                if (!hit && t >= .60f)
                {
                    hit = true; RestoreProp();
                    if (kind == ActionKind.Pickup && beganIn == GameState.Phase2A) ShowCarried(target);
                    if (kind == ActionKind.Give) ClearCarried();
                    Impacts++; impact?.Invoke();
                }
                yield return null;
            }
            if (!hit) { Impacts++; impact?.Invoke(); }
            CompletedActions++; Finish(true);
        }
        private void Update()
        {
            if (IsBusy && game != null && game.State != beganIn) Cancel();
            if (carriedVisual != null && game != null && game.State != GameState.Phase2A) ClearCarried();
        }
        private void LateUpdate()
        {
            if (carriedVisual != null)
            {
                var comfort = Camera.main != null ? Camera.main.GetComponent<AcademyCameraComfort>() : null;
                carriedVisual.SetActive(comfort == null || !comfort.AvatarHidden);
            }
            if (!IsBusy && carriedVisual != null && body != null)
                body.SetHands(transform.position+transform.forward*.32f+transform.right*.12f+Vector3.up*1.02f,
                    transform.position+transform.forward*.32f-transform.right*.14f+Vector3.up*1.02f,.7f);
        }
        public void ShowCarried(Transform source)
        {
            ClearCarried();
            var meshes = source.GetComponentsInChildren<MeshFilter>(true);
            if (meshes.Length == 0 || body == null) return;
            var renderer = source.GetComponentInChildren<Renderer>(true);
            var origin = renderer != null ? renderer.bounds.center : source.position;
            carriedVisual = new GameObject("Carried " + source.name); carriedVisual.transform.position = origin;
            foreach(var mesh in meshes)
            {
                var sourceRenderer = mesh.GetComponent<MeshRenderer>(); if(sourceRenderer == null || mesh.sharedMesh == null) continue;
                var piece = new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));
                piece.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
                piece.GetComponent<MeshRenderer>().sharedMaterials=sourceRenderer.sharedMaterials;
                piece.transform.SetPositionAndRotation(mesh.transform.position,mesh.transform.rotation); piece.transform.localScale=mesh.transform.lossyScale;
                piece.transform.SetParent(carriedVisual.transform,true);
            }
            carriedVisual.transform.SetParent(body.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand),true);
            carriedVisual.transform.localPosition=Vector3.zero;
        }
        private void ClearCarried() { if(carriedVisual != null) Destroy(carriedVisual); carriedVisual=null; }
        public void Cancel()
        {
            if (sequence != null) StopCoroutine(sequence);
            Finish(false);
        }
        private void RestoreProp()
        {
            if (held != null)
            { held.SetParent(heldParent, false); held.localPosition = heldPosition; held.localRotation = heldRotation; held.localScale = heldScale; }
            held = heldParent = null;
        }
        private void Finish(bool success)
        {
            bool wasBusy = IsBusy; IsBusy = false; Prepared = false; sequence = null; RestoreProp();
            if (body != null) body.ClearHands(); if (receiver != null) receiver.ClearHands(); receiver = null;
            GetComponent<AcademyCharacterAnimation>()?.SetActionSpeed(0);
            var callback = completed; completed = null;
            if (wasBusy && game != null && game.State == beganIn && !DialogueManager.IsOpen)
            {
                var story = FindFirstObjectByType<Phase2AStoryManager>();
                var cleanup = FindFirstObjectByType<Phase2BCleanupManager>();
                bool cleanupReady = beganIn != GameState.Phase2BCleanup || cleanup != null && cleanup.Active && !cleanup.InteractionBusy;
                bool pageClosed = story == null || story.pagePanel == null || !story.pagePanel.activeInHierarchy;
                if (cleanupReady && pageClosed && (beganIn != GameState.Phase2A || story == null || story.Stage != Phase2AStage.WitnessLeaving)) SetControl(true);
            }
            if (success) callback?.Invoke();
        }
        private void SetControl(bool value)
        {
            if (game == null) return;
            if (game.State == GameState.Phase2A) game.SetPhase2AControl(value);
            else if (game.State == GameState.Phase2BCleanup) game.SetPhase2BControl(value);
        }
        private void OnDisable() { Cancel(); ClearCarried(); }
    }
}
