using UnityEngine;

namespace VelvetVows
{
    [RequireComponent(typeof(Collider))]
    public sealed class BlackRoseCollectible : MonoBehaviour
    {
        private bool taken;
        private void OnTriggerEnter(Collider other)
        {
            if (taken || other.GetComponentInParent<StarterAssets.ThirdPersonController>() == null) return;
            var game = GameManager.Instance;
            if (game == null || game.State == GameState.Introduction || game.State == GameState.Defeat || game.State == GameState.Victory) return;
            taken = true;
            game.RoseCollected();
            Destroy(gameObject);
        }
    }
}
