using UnityEngine;
using CT.Workers;

public class WorkerSpawner : MonoBehaviour
{
    [Header("Controls")]
    [Tooltip("Press this key to spawn a worker")]
    public KeyCode spawnKey = KeyCode.T;
    
    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            if (WorkerManager.Instance)
            {
                WorkerManager.Instance.SpawnWorker();
            }
            else
            {
                Debug.LogWarning("WorkerSpawner: No WorkerManager found in scene!");
            }
        }
    }

    void OnGUI()
    {
        // Simple UI to show controls
        GUILayout.BeginArea(new Rect(10, Screen.height - 60, 300, 50));
        GUILayout.Label($"Press '{spawnKey}' to spawn a worker", GUILayout.Width(250));
        
        if (WorkerManager.Instance)
        {
            GUILayout.Label($"Workers: {WorkerManager.Instance.WorkerCount}", GUILayout.Width(150));
        }
        
        GUILayout.EndArea();
    }
}
