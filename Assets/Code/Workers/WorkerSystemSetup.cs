using UnityEngine;
using CT.Workers;

/// <summary>
/// Helper script to set up the worker system in the scene.
/// Add this to a GameObject in your scene to automatically configure workers.
/// </summary>
public class WorkerSystemSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool autoSetup = true;
    public bool createWorkerManager = true;
    public bool createJobManager = true;
    public bool createWorkerSpawner = true;
    
    [Header("Worker Configuration")]
    public GameObject workerPrefab;
    public int initialWorkerCount = 3;
    public float workerMoveSpeed = 2f;
    
    [Header("Spawning")]
    public Vector2 spawnAreaSize = new Vector2(10f, 10f);
    public Vector3 spawnCenter = Vector3.zero;

    void Start()
    {
        if (autoSetup)
        {
            SetupWorkerSystem();
        }
    }

    [ContextMenu("Setup Worker System")]
    public void SetupWorkerSystem()
    {
        Debug.Log("Setting up Worker System...");

        // Auto-find worker prefab if not assigned
        if (!workerPrefab)
        {
            workerPrefab = Resources.Load<GameObject>("Worker");
            if (!workerPrefab)
            {
                Debug.LogWarning("WorkerSystemSetup: Worker prefab not found. Please assign it manually in the inspector.");
            }
        }

        // Set spawn center to world center if not set
        if (spawnCenter == Vector3.zero && GridMap.I)
        {
            spawnCenter = GridMap.I.WorldCenter;
        }

        // Create Job Manager
        if (createJobManager && !FindObjectOfType<JobManager>())
        {
            GameObject jobManagerGO = new GameObject("JobManager");
            jobManagerGO.transform.SetParent(transform);
            jobManagerGO.AddComponent<JobManager>();

            Debug.Log("Created JobManager");
        }

        // Create Worker Manager
        if (createWorkerManager && !FindObjectOfType<WorkerManager>())
        {
            GameObject managerGO = new GameObject("WorkerManager");
            managerGO.transform.SetParent(transform);
            
            WorkerManager manager = managerGO.AddComponent<WorkerManager>();
            manager.workerPrefab = workerPrefab;
            manager.initialWorkerCount = initialWorkerCount;
            manager.spawnAreaSize = spawnAreaSize;
            manager.spawnCenter = spawnCenter;

            Debug.Log("Created WorkerManager");
        }

        // Create Worker Spawner for testing
        if (createWorkerSpawner && !FindObjectOfType<WorkerSpawner>())
        {
            GameObject spawnerGO = new GameObject("WorkerSpawner");
            spawnerGO.transform.SetParent(transform);
            spawnerGO.AddComponent<WorkerSpawner>();

            Debug.Log("Created WorkerSpawner");
        }

        Debug.Log("Worker System setup complete!");
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
}
