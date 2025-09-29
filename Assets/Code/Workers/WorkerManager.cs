using System.Collections.Generic;
using UnityEngine;

namespace CT.Workers
{
    public class WorkerManager : MonoBehaviour
    {
        public static WorkerManager Instance { get; private set; }
        
        [Header("Worker Spawning")]
        public GameObject workerPrefab;
        public int initialWorkerCount = 3;
        public Transform workerParent;
        
        [Header("Spawning Area")]
        public Vector2 spawnAreaSize = new Vector2(10f, 10f);
        public Vector3 spawnCenter = Vector3.zero;
        
        private List<Worker> workers = new List<Worker>();

        public List<Worker> Workers => workers;
        public int WorkerCount => workers.Count;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            if (!workerParent)
            {
                workerParent = new GameObject("Workers").transform;
                workerParent.SetParent(transform);
            }

            // Spawn initial workers
            for (int i = 0; i < initialWorkerCount; i++)
            {
                SpawnWorker();
            }
        }

        public Worker SpawnWorker()
        {
            if (!workerPrefab)
            {
                Debug.LogError("WorkerManager: No worker prefab assigned!");
                return null;
            }

            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject workerGO = Instantiate(workerPrefab, spawnPos, Quaternion.identity, workerParent);
            workerGO.name = $"Worker_{workers.Count + 1}";

            Worker worker = workerGO.GetComponent<Worker>();
            if (worker)
            {
                workers.Add(worker);
                
                // Ensure the worker sprite is on top
                SpriteRenderer sr = workerGO.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    sr.sortingLayerName = "Buildings";
                    sr.sortingOrder = 1000; // Very high value to stay on top of all buildings
                }
                
                // Configure physics for less collision pushing
                Rigidbody2D rb = workerGO.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    rb.mass = 0.5f; // Lighter workers push less
                }
                
                // Set minimal collision radius
                CircleCollider2D collider = workerGO.GetComponent<CircleCollider2D>();
                if (collider)
                {
                    collider.radius = 0.05f; // Extremely small collision, just 5% of original
                }
                
                Debug.Log($"Spawned worker at {spawnPos}");
            }
            else
            {
                Debug.LogError("WorkerManager: Spawned worker prefab doesn't have Worker component!");
                Destroy(workerGO);
            }

            return worker;
        }

        public Worker GetAvailableWorker()
        {
            foreach (Worker worker in workers)
            {
                if (worker && worker.IsAvailable)
                {
                    return worker;
                }
            }
            return null;
        }

        public List<Worker> GetAvailableWorkers()
        {
            List<Worker> available = new List<Worker>();
            foreach (Worker worker in workers)
            {
                if (worker && worker.IsAvailable)
                {
                    available.Add(worker);
                }
            }
            return available;
        }

        Vector3 GetRandomSpawnPosition()
        {
            // Get random position within spawn area
            Vector2 randomOffset = new Vector2(
                Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
                Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f)
            );
            
            Vector3 worldPos = spawnCenter + new Vector3(randomOffset.x, randomOffset.y, 0f);
            
            // Snap to grid if GridMap exists
            if (GridMap.I)
            {
                Vector2Int cell = GridMap.I.WorldToCell(worldPos);
                if (GridMap.I.InBounds(cell))
                {
                    return GridMap.I.CellToWorld(cell);
                }
                else
                {
                    // If out of bounds, use center of grid
                    return GridMap.I.WorldCenter;
                }
            }
            
            return worldPos;
        }

        void OnDrawGizmosSelected()
        {
            // Draw spawn area
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnCenter, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0f));
            
            // Draw spawn center
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnCenter, 0.5f);
        }

        // Method to spawn worker manually (for testing)
        [ContextMenu("Spawn Worker")]
        public void SpawnWorkerManual()
        {
            SpawnWorker();
        }

        // Method to clear all workers (for testing)
        [ContextMenu("Clear All Workers")]
        public void ClearAllWorkers()
        {
            foreach (Worker worker in workers)
            {
                if (worker)
                {
                    DestroyImmediate(worker.gameObject);
                }
            }
            workers.Clear();
        }
    }
}
