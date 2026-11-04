using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class EvidenceManager : MonoBehaviour
    {
        [SerializeField] private List<EvidenceItem> items = new List<EvidenceItem>();
        private readonly HashSet<EvidenceItem> collected = new HashSet<EvidenceItem>();
        public int Total => items.Count;
        public int Count => collected.Count;
        public void Configure(List<EvidenceItem> evidence) => items = evidence;

        public void Collect(EvidenceItem item)
        {
            if (item == null || !items.Contains(item) || !collected.Add(item)) return;
            GameManager.Instance.ShowClue(item.Clue);
            GameManager.Instance.RefreshEvidence(Count);
            if (Count == Total) GameManager.Instance.AllEvidenceFound();
            Destroy(item.gameObject);
        }
    }
}
