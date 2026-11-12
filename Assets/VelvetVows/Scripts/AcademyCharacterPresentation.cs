using UnityEngine;

namespace VelvetVows
{
    [DefaultExecutionOrder(10000)]
    public sealed class AcademyCharacterPresentation : MonoBehaviour
    {
        [SerializeField] private Animator model;
        [SerializeField, Range(.75f, 1f)] private float headProportion = .91f;
        private Transform head;
        private Vector3 originalHeadScale;

        public void Configure(Animator animator, float proportion)
        { model = animator; headProportion = proportion; }
        private void Awake()
        {
            if (model == null) model = GetComponent<Animator>();
            if (model == null || !model.isHuman) return;
            head = model.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) originalHeadScale = head.localScale;
        }
        private void LateUpdate()
        {
            if (head != null) head.localScale = originalHeadScale * headProportion;
        }
        private void OnDisable()
        {
            if (head != null) head.localScale = originalHeadScale;
        }
    }
}
