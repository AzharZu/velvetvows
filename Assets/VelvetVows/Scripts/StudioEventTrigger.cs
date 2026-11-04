using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class StudioEventTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other) => Check(other);
        private void OnTriggerStay(Collider other) => Check(other);
        private void Check(Collider other)
        {
            if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null)
                GameManager.Instance?.EnteredStudio();
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null)
                GameManager.Instance?.ExitedStudio();
        }
    }
}
