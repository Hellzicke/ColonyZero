using UnityEngine;
using CT.Workers;

/// <summary>
/// Simple script to test worker functionality.
/// Attach this to any GameObject in the scene to test workers.
/// </summary>
public class WorkerTester : MonoBehaviour
{
    [Header("Testing")]
    public KeyCode spawnWorkerKey = KeyCode.T;
    public KeyCode clearWorkersKey = KeyCode.R;
    
    [Header("Prefab Reference")]
    public GameObject workerPrefab;

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Spawn worker
        if (Input.GetKeyDown(spawnWorkerKey))
        {
            SpawnTestWorker();
        }

        // Clear all workers
        if (Input.GetKeyDown(clearWorkersKey))
        {
            ClearAllWorkers();
        }
    }

    void SpawnTestWorker()
    {
        if (WorkerManager.Instance)
        {
            Worker newWorker = WorkerManager.Instance.SpawnWorker();
            if (newWorker)
            {
                Debug.Log($"Spawned worker: {newWorker.name}");
            }
        }
        else if (workerPrefab)
        {
            // Fallback: spawn directly if no WorkerManager
            Vector3 spawnPos = GetRandomPosition();
            GameObject workerGO = Instantiate(workerPrefab, spawnPos, Quaternion.identity);
            workerGO.name = $"TestWorker_{Random.Range(1000, 9999)}";
            Debug.Log($"Spawned test worker at {spawnPos}");
        }
        else
        {
            Debug.LogWarning("No WorkerManager found and no worker prefab assigned!");
        }
    }

    void ClearAllWorkers()
    {
        Worker[] allWorkers = FindObjectsOfType<Worker>();
        int count = allWorkers.Length;
        
        foreach (Worker worker in allWorkers)
        {
            if (worker)
            {
                DestroyImmediate(worker.gameObject);
            }
        }
        
        Debug.Log($"Cleared {count} workers");
    }

    Vector3 GetRandomPosition()
    {
        Vector3 basePos = transform.position;
        
        if (GridMap.I)
        {
            // Use grid center if available
            basePos = GridMap.I.WorldCenter;
        }
        
        // Add some random offset
        Vector2 randomOffset = Random.insideUnitCircle * 5f;
        Vector3 worldPos = basePos + new Vector3(randomOffset.x, randomOffset.y, 0f);
        
        // Snap to grid if GridMap exists
        if (GridMap.I)
        {
            Vector2Int cell = GridMap.I.WorldToCell(worldPos);
            if (GridMap.I.InBounds(cell))
            {
                return GridMap.I.CellToWorld(cell);
            }
        }
        
        return worldPos;
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        
        GUILayout.Label("=== Worker Testing ===");
        GUILayout.Label($"Press '{spawnWorkerKey}' to spawn worker");
        GUILayout.Label($"Press '{clearWorkersKey}' to clear workers");
        GUILayout.Space(10);
        
        if (WorkerManager.Instance)
        {
            GUILayout.Label($"Workers: {WorkerManager.Instance.WorkerCount}");
            GUILayout.Label($"Available: {WorkerManager.Instance.GetAvailableWorkers().Count}");
        }
        else
        {
            GUILayout.Label("No WorkerManager in scene");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Spawn Worker"))
        {
            SpawnTestWorker();
        }
        
        if (GUILayout.Button("Clear All Workers"))
        {
            ClearAllWorkers();
        }
        
        GUILayout.EndArea();
    }
}
