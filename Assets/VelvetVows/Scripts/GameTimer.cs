using UnityEngine;

namespace VelvetVows
{
    public sealed class GameTimer : MonoBehaviour
    {
        [SerializeField, Min(1)] private float durationSeconds = 300f;
        private bool running;
        public float Remaining { get; private set; }

        private void Awake() => Remaining = durationSeconds;
        public void Begin() { Remaining = durationSeconds; running = true; }
        public void Stop() => running = false;
        private void Update()
        {
            if (!running) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            GameManager.Instance?.RefreshTimer(Remaining);
            if (Remaining <= 0)
            {
                running = false;
                GameManager.Instance?.Defeat("The Black Rose Ball began without Evelyn.");
            }
        }
    }
}
