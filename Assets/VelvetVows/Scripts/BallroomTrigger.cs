using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BallroomTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null)
                GameManager.Instance?.EnteredBallroom();
        }
    }
}
