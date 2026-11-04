using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class Phase2AReputation : MonoBehaviour
    {
        [SerializeField] private GameHUD hud;
        [SerializeField, Min(1)] private int trustThreshold = 5;
        private readonly HashSet<string> rewardedTasks = new HashSet<string>();
        public int Value { get; private set; }
        public int TrustThreshold => trustThreshold;
        public void Configure(GameHUD gameHud) => hud = gameHud;
        private void Start() => hud?.SetReputation(Value, trustThreshold);
        public bool Award(string taskId, int amount)
        {
            if (string.IsNullOrEmpty(taskId) || amount <= 0 || !rewardedTasks.Add(taskId)) return false;
            Value += amount;
            hud?.SetReputation(Value, trustThreshold);
            Debug.Log($"[Phase2A] Reputation +{amount} ({taskId}) = {Value}", this);
            return true;
        }
    }
}
