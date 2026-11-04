using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DetectionZone : MonoBehaviour
    {
        [SerializeField] private SuspicionSystem suspicion;
        [SerializeField] private Color gizmoColor = new Color(.7f, .1f, .2f, .2f);
        public void Configure(SuspicionSystem system) => suspicion = system;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null) suspicion?.Enter(this);
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null) suspicion?.Exit(this);
        }
        private void OnDisable() { if (suspicion != null) suspicion.Exit(this); }
        private void OnDrawGizmosSelected()
        {
            var sphere = GetComponent<SphereCollider>();
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * transform.lossyScale.x);
        }
    }
}
