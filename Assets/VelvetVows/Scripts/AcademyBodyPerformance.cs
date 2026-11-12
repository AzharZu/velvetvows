using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

namespace VelvetVows
{
    [DefaultExecutionOrder(10050)]
    [RequireComponent(typeof(Animator))]
    public sealed class AcademyBodyPerformance : MonoBehaviour
    {
        private struct Pose { public Transform bone; public Quaternion before, after; }
        private readonly List<Pose> changed = new();
        private readonly List<Transform> secondary = new();
        private readonly Dictionary<Transform,Quaternion> restingFingers = new();
        private Animator model;
        private AcademyCharacterPresence presence;
        private ThirdPersonController movement;
        private Transform hips, chest, leftEye, rightEye;
        private Vector3 hipsBefore, hipsAfter, previousPosition;
        private bool hipsMoved;
        private float speed, phase, secondarySway, alertUntil;
        private float leftSole=.1f,rightSole=.1f;
        private Vector3 alertPoint;
        private Vector3? rightTarget, leftTarget;
        private Transform gripShoulder, gripElbow;
        private float rightGripAlong, leftGripAlong;
        private Vector3? rightPalm, leftPalm;
        private float handWeight, bend;
        private bool deepCrouch;
        public bool Suppressed { get; set; }
        public float FootIKWeight { get; private set; }
        public float HandIKWeight => handWeight;
        public void SetHands(Vector3? right, Vector3? left, float weight, float crouch = 0)
        { rightTarget = right; leftTarget = left; gripShoulder = gripElbow = null; handWeight = Mathf.Clamp01(weight); bend = crouch; }
        public void SetBodyGrip(Transform shoulder, Transform elbow, float rightAlong, float leftAlong)
        { gripShoulder = shoulder; gripElbow = elbow; rightGripAlong = rightAlong; leftGripAlong = leftAlong; }
        public void ClearHands() { rightTarget = leftTarget = rightPalm = leftPalm = null; gripShoulder = gripElbow = null; handWeight = bend = 0; deepCrouch = false; }
        public void SetDeepCrouch(bool value) => deepCrouch = value;
        public void SetPalmContact(bool right, Vector3 normal) { if(right) rightPalm=normal; else leftPalm=normal; }
        public void LookAtNoise(Vector3 point, float seconds = 2) { alertPoint = point; alertUntil = Time.time + seconds; }
        private void Awake()
        {
            model = GetComponent<Animator>(); presence = GetComponent<AcademyCharacterPresence>();
            movement = GetComponentInParent<ThirdPersonController>();
            if (!model.isHuman) { enabled = false; return; }
            hips = model.GetBoneTransform(HumanBodyBones.Hips);
            chest = model.GetBoneTransform(HumanBodyBones.Chest);
            leftEye = model.GetBoneTransform(HumanBodyBones.LeftEye); rightEye = model.GetBoneTransform(HumanBodyBones.RightEye);
            var left=model.GetBoneTransform(HumanBodyBones.LeftFoot);var right=model.GetBoneTransform(HumanBodyBones.RightFoot);
            if(left!=null)leftSole=Mathf.Clamp(left.position.y-transform.position.y,.065f,.15f);
            if(right!=null)rightSole=Mathf.Clamp(right.position.y-transform.position.y,.065f,.15f);
            var unique = new HashSet<Transform>();
            foreach (var mesh in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var bone in mesh.bones)
                {
                    if (bone == null || !unique.Add(bone)) continue;
                    string name = bone.name.ToLowerInvariant();
                    if ((name.Contains("hair") || name.Contains("skirt")) && secondary.Count < 16) secondary.Add(bone);
                }
            phase = (GetInstanceID() & 255) * .17f; previousPosition = transform.position;
            foreach(HumanBodyBones id in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                string name=id.ToString();
                if(!name.Contains("Thumb")&&!name.Contains("Index")&&!name.Contains("Middle")&&!name.Contains("Ring")&&!name.Contains("Little"))continue;
                var bone=model.GetBoneTransform(id);if(bone!=null)restingFingers[bone]=bone.localRotation;
            }
        }
        private void Restore()
        {
            for (int i = changed.Count - 1; i >= 0; i--)
            {
                var p = changed[i];
                if (p.bone != null && Quaternion.Angle(p.bone.localRotation, p.after) < .1f) p.bone.localRotation = p.before;
            }
            changed.Clear();
            if (hipsMoved && hips != null && (hips.localPosition - hipsAfter).sqrMagnitude < .000001f) hips.localPosition = hipsBefore;
            hipsMoved = false;
        }
        private void Update() => Restore();
        private void Rotate(Transform bone, Quaternion rotation, float weight = 1)
        {
            if (bone == null || weight <= .001f) return;
            var before = bone.localRotation; bone.rotation = Quaternion.Slerp(bone.rotation, rotation, weight);
            changed.Add(new Pose { bone = bone, before = before, after = bone.localRotation });
        }
        private void LateUpdate()
        {
            Vector3 travel = transform.position - previousPosition; previousPosition = transform.position;
            speed = new Vector2(travel.x, travel.z).magnitude / Mathf.Max(.001f, Time.deltaTime);
            if (Suppressed || presence != null && !presence.enabled) { FootIKWeight = 0; return; }
            bool grounded = movement != null ? movement.Grounded : Physics.Raycast(transform.position + Vector3.up * .25f, Vector3.down, .5f, 257, QueryTriggerInteraction.Ignore);
            FootIKWeight = Mathf.MoveTowards(FootIKWeight, grounded ? 1 : 0, Time.deltaTime * 12);
            float breath = Mathf.Sin(Time.time * 1.8f + phase) * .45f;
            if (chest != null) Rotate(chest, Quaternion.AngleAxis(breath + bend * (deepCrouch ? 50 : 43), transform.right) * chest.rotation);
            if (bend > 0 && hips != null)
            {
                hipsBefore = hips.localPosition; hips.position -= Vector3.up * (bend * (deepCrouch ? .62f : .43f));
                hipsAfter = hips.localPosition; hipsMoved = true;
            }
            if (grounded && FootIKWeight > 0)
            { GroundFoot(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot); GroundFoot(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot); }
            if (gripShoulder != null && gripElbow != null)
            {
                var rightPoint = Vector3.Lerp(gripShoulder.position, gripElbow.position, rightGripAlong);
                var leftPoint = Vector3.Lerp(gripShoulder.position, gripElbow.position, leftGripAlong);
                var rightUpper = model.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var leftUpper = model.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                rightTarget = rightPoint + (rightUpper != null ? (rightUpper.position - rightPoint).normalized * .025f : Vector3.zero);
                leftTarget = leftPoint + (leftUpper != null ? (leftUpper.position - leftPoint).normalized * .025f : Vector3.zero);
            }
            if (rightTarget.HasValue) Solve(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, rightTarget.Value, handWeight);
            if (leftTarget.HasValue) Solve(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, leftTarget.Value, handWeight);
            if(handWeight>0)
                foreach(var pair in restingFingers)
                    Rotate(pair.Key,pair.Key.parent.rotation*pair.Value,handWeight*.65f);
            if (gripShoulder != null && gripElbow != null)
            {
                CurlTowardArm(HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate);
                CurlTowardArm(HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate);
                CurlTowardArm(HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate);
                CurlTowardArm(HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate);
                CurlTowardArm(HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate);
                CurlTowardArm(HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate);
                CurlTowardArm(HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate);
                CurlTowardArm(HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate);
            }
            if(rightPalm.HasValue) AlignPalm(true,rightPalm.Value);
            if(leftPalm.HasValue) AlignPalm(false,leftPalm.Value);
            float glance = Mathf.Sin(Time.time * .83f + phase) * 1.3f + Mathf.Sin(Time.time * 2.3f + phase) * .35f;
            if (leftEye != null) Rotate(leftEye, leftEye.rotation * Quaternion.Euler(0, glance, 0));
            if (rightEye != null) Rotate(rightEye, rightEye.rotation * Quaternion.Euler(0, glance, 0));
            if (Time.time < alertUntil && !DialogueManager.IsOpen)
            {
                var head = model.GetBoneTransform(HumanBodyBones.Neck) ?? model.GetBoneTransform(HumanBodyBones.Head);
                var direction = alertPoint - head.position;
                var offset = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(transform.forward, direction), 18);
                Rotate(head, offset * head.rotation, .65f);
            }
            secondarySway = Mathf.Lerp(secondarySway, Mathf.Clamp(-travel.x * 90, -4, 4), Time.deltaTime * 7);
            foreach (var bone in secondary) Rotate(bone, bone.rotation * Quaternion.Euler(Mathf.Sin(Time.time * 2 + phase) * .65f, 0, secondarySway));
        }
        private void AlignPalm(bool right,Vector3 normal)
        {
            var hand=model.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
            var middle=model.GetBoneTransform(right?HumanBodyBones.RightMiddleProximal:HumanBodyBones.LeftMiddleProximal);
            var index=model.GetBoneTransform(right?HumanBodyBones.RightIndexProximal:HumanBodyBones.LeftIndexProximal);
            var little=model.GetBoneTransform(right?HumanBodyBones.RightLittleProximal:HumanBodyBones.LeftLittleProximal);
            if(hand==null||middle==null||index==null||little==null)return;
            var along=middle.position-hand.position;var across=index.position-little.position;
            var palm=Vector3.Cross(across,along);
            if(along.sqrMagnitude<.00001f||palm.sqrMagnitude<.00001f)return;
            var change=Quaternion.LookRotation(Vector3.up,normal)*Quaternion.Inverse(Quaternion.LookRotation(along,palm));
            Rotate(hand,change*hand.rotation,handWeight);
        }
        private void CurlTowardArm(HumanBodyBones proximal, HumanBodyBones next)
        {
            var finger = model.GetBoneTransform(proximal); var joint = model.GetBoneTransform(next);
            if (finger == null || joint == null) return;
            var axis = gripElbow.position - gripShoulder.position;
            if (axis.sqrMagnitude < .00001f) return;
            float along = Mathf.Clamp01(Vector3.Dot(joint.position - gripShoulder.position, axis) / axis.sqrMagnitude);
            var toward = gripShoulder.position + axis * along - finger.position;
            var direction = joint.position - finger.position;
            if (toward.sqrMagnitude < .00001f || direction.sqrMagnitude < .00001f) return;
            var turn = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(direction, toward), 24f);
            Rotate(finger, turn * finger.rotation, handWeight * .7f);
        }
        private void GroundFoot(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones end)
        {
            var foot = model.GetBoneTransform(end); if (foot == null) return;
            if (!Physics.Raycast(foot.position + Vector3.up * .5f, Vector3.down, out var hit, .8f, 257, QueryTriggerInteraction.Ignore)) return;
            float correction = hit.point.y + (end==HumanBodyBones.LeftFoot?leftSole:rightSole) - foot.position.y;
            // Swinging feet remain animated; only planted feet follow uneven floors.
            if (correction < -.12f || correction > (bend > .1f ? .55f : .16f)) return;
            Solve(upper, lower, end, foot.position + Vector3.up * correction, FootIKWeight);
            Rotate(foot, Quaternion.FromToRotation(Vector3.up, hit.normal) * foot.rotation, FootIKWeight * .5f);
        }
        private void Solve(HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones endBone, Vector3 goal, float weight)
        {
            var upper = model.GetBoneTransform(upperBone); var lower = model.GetBoneTransform(lowerBone); var end = model.GetBoneTransform(endBone);
            if (upper == null || lower == null || end == null || weight <= .001f) return;
            var a = upper.position; var b = lower.position; var c = end.position;
            float first = Vector3.Distance(a, b), second = Vector3.Distance(b, c);
            var direction = goal - a; if (direction.sqrMagnitude < .0001f || first < .001f || second < .001f) return;
            float length = Mathf.Clamp(direction.magnitude, Mathf.Abs(first - second) + .001f, first + second - .001f);
            direction.Normalize(); goal = a + direction * length;
            Vector3 pole = Vector3.ProjectOnPlane(b - a, direction);
            if (pole.sqrMagnitude < .0001f) pole = Vector3.ProjectOnPlane(transform.forward, direction);
            if (pole.sqrMagnitude < .0001f) pole = Vector3.ProjectOnPlane(transform.right, direction);
            pole.Normalize();
            float along = (first * first + length * length - second * second) / (2 * length);
            var joint = a + direction * along + pole * Mathf.Sqrt(Mathf.Max(0, first * first - along * along));
            Rotate(upper, Quaternion.FromToRotation(b - a, joint - a) * upper.rotation, weight);
            Rotate(lower, Quaternion.FromToRotation(end.position - lower.position, goal - lower.position) * lower.rotation, weight);
        }
        private void OnDisable() { Restore(); ClearHands(); FootIKWeight = 0; }
    }
}
