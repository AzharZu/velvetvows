using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class SuspicionSystem : MonoBehaviour
    {
        [SerializeField, Min(0)] private float DetectionRate = 8f;
        [SerializeField, Min(0)] private float RecoveryDelay = 1.5f;
        [SerializeField, Min(0)] private float RecoveryRate = 5f;
        [SerializeField] private float current;
        private float timeOutsideDetection;
        private readonly HashSet<DetectionZone> activeZones = new HashSet<DetectionZone>();
        private bool monitoring;
        public float Current => current;

        public void Begin() { current = 0; timeOutsideDetection = 0; monitoring = true; }
        public void Stop() { monitoring = false; activeZones.Clear(); timeOutsideDetection = 0; }
        public void Enter(DetectionZone zone) { if (zone != null) { activeZones.Add(zone); timeOutsideDetection = 0; } }
        public void Exit(DetectionZone zone) { if (zone != null) activeZones.Remove(zone); }
        public void Adjust(float amount)
        {
            current = Mathf.Clamp(current + amount, 0, 100);
            GameManager.Instance?.RefreshSuspicion(current);
            if (monitoring && current >= 100) GameManager.Instance?.Defeat("Suspicion reached 100%.");
        }

        private void Update()
        {
            if (!monitoring) return;
            if (activeZones.Count > 0)
            {
                timeOutsideDetection = 0;
                current += DetectionRate * Time.deltaTime;
            }
            else
            {
                timeOutsideDetection += Time.deltaTime;
                if (timeOutsideDetection >= RecoveryDelay)
                    current -= RecoveryRate * Time.deltaTime;
            }
            current = Mathf.Clamp(current, 0, 100);
            GameManager.Instance?.RefreshSuspicion(current);
            if (current >= 100) GameManager.Instance?.Defeat("Suspicion reached 100%.");
        }
    }

}
