using System.Collections.Generic;
using UnityEngine;

namespace VelvetVows
{
    public sealed class BlackRoseSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject rosePrefab;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        public int SpawnCount => spawnPoints.Count;
        public GameObject RosePrefab => rosePrefab;
        public List<Transform> SpawnPoints => spawnPoints;
        public void Configure(GameObject prefab, List<Transform> points) { rosePrefab = prefab; spawnPoints = points; }
        private void Start()
        {
            foreach (Transform point in spawnPoints)
                if (point != null && rosePrefab != null)
                    Instantiate(rosePrefab, point.position, point.rotation, transform);
        }
    }
}
