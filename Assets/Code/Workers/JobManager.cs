using System.Collections.Generic;
using UnityEngine;
using CT.Build;

namespace CT.Workers
{
    /// <summary>
    /// Manages job assignment between buildings that need construction and available workers
    /// </summary>
    public class JobManager : MonoBehaviour
    {
        public static JobManager Instance { get; private set; }
        
        [Header("Job Management")]
        public float jobUpdateInterval = 0.5f;
        public float maxJobDistance = 20f;
        
        private List<GhostBuilding> pendingJobs = new List<GhostBuilding>();
        private Dictionary<Worker, GhostBuilding> activeJobs = new Dictionary<Worker, GhostBuilding>();
        private float lastJobUpdate = 0f;

        public int PendingJobsCount => pendingJobs.Count;
        public int ActiveJobsCount => activeJobs.Count;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            if (Time.time - lastJobUpdate >= jobUpdateInterval)
            {
                UpdateJobAssignments();
                lastJobUpdate = Time.time;
            }
        }

        /// <summary>
        /// Add a new building job to the queue
        /// </summary>
        public void AddBuildJob(GhostBuilding ghostBuilding)
        {
            if (!pendingJobs.Contains(ghostBuilding))
            {
                // Check if this is a door and we have workers struggling with unreachable jobs
                bool isDoor = IsDoorBuilding(ghostBuilding);
                bool hasUnreachableJobs = HasUnreachableActiveJobs();
                
                if (isDoor && hasUnreachableJobs)
                {
                    // Priority insert door at the front
                    pendingJobs.Insert(0, ghostBuilding);
                    Debug.Log($"🚪 PRIORITY: Door job prioritized: {ghostBuilding.buildingType.displayName}");
                    
                    // Interrupt workers who are stuck trying to reach unreachable jobs
                    InterruptUnreachableJobs();
                }
                else
                {
                    // Normal job, add to end of queue
                    pendingJobs.Add(ghostBuilding);
                }
            }
        }

        bool IsDoorBuilding(GhostBuilding building)
        {
            if (!building || !building.buildingType) return false;
            string name = building.buildingType.displayName.ToLower();
            return name.Contains("door");
        }

        bool HasUnreachableActiveJobs()
        {
            // Check if any workers are trying to reach jobs in enclosed areas OR if we have floor jobs in pending queue
            foreach (var kvp in activeJobs)
            {
                Worker worker = kvp.Key;
                GhostBuilding job = kvp.Value;
                
                if (worker && job && worker.CurrentState == WorkerState.MovingToJob)
                {
                    if (IsJobInEnclosedArea(job))
                    {
                        return true;
                    }
                }
            }
            
            // Also check pending jobs - if we have floor jobs pending, they might be unreachable
            foreach (GhostBuilding job in pendingJobs)
            {
                if (job && IsJobInEnclosedArea(job))
                {
                    return true;
                }
            }
            
            return false;
        }

        void InterruptUnreachableJobs()
        {
            List<Worker> workersToInterrupt = new List<Worker>();
            
            foreach (var kvp in activeJobs)
            {
                Worker worker = kvp.Key;
                GhostBuilding job = kvp.Value;
                
                if (worker && worker.CurrentState == WorkerState.MovingToJob && job)
                {
                    // Check if this job is inside an enclosed area
                    if (IsJobInEnclosedArea(job))
                    {
                        workersToInterrupt.Add(worker);
                    }
                }
            }
            
            // Interrupt these workers so they can pick up the door job
            foreach (Worker worker in workersToInterrupt)
            {
                Debug.Log($"Interrupting worker {worker.name} to prioritize door construction");
                WorkerTimedOutOnJob(worker);
            }
        }

        bool IsJobInEnclosedArea(GhostBuilding job)
        {
            BuildPlacer placer = FindObjectOfType<BuildPlacer>();
            if (!placer || !job || !job.buildingType) return false;
            
            string jobName = job.buildingType.displayName.ToLower();
            
            // Check if this is a floor job (most likely to be in enclosed areas)
            bool isFloor = jobName.Contains("floor");
            if (!isFloor) return false; // Only consider floor jobs as potentially enclosed
            
            // Use pathfinding to test if the job is reachable from the edges of the map
            Vector2Int jobPos = job.gridPosition;
            
            // Test pathfinding from multiple edge points to the job location
            Vector2Int[] edgeTestPoints = {
                new Vector2Int(0, jobPos.y),        // Left edge
                new Vector2Int(GridMap.I.width - 1, jobPos.y), // Right edge
                new Vector2Int(jobPos.x, 0),        // Bottom edge
                new Vector2Int(jobPos.x, GridMap.I.height - 1), // Top edge
                new Vector2Int(0, 0),               // Corners
                new Vector2Int(GridMap.I.width - 1, 0),
                new Vector2Int(0, GridMap.I.height - 1),
                new Vector2Int(GridMap.I.width - 1, GridMap.I.height - 1)
            };
            
            // If we can't find a path from any edge point to the job, it's enclosed
            foreach (Vector2Int edgePoint in edgeTestPoints)
            {
                if (!GridMap.I.InBounds(edgePoint) || !placer.IsWalkable(edgePoint)) continue;
                
                var path = CT.Workers.WorkerPathfinding.FindPath(
                    GridMap.I.CellToWorld(edgePoint), 
                    GridMap.I.CellToWorld(jobPos)
                );
                
                // If we found a valid path from any edge, the job is reachable
                if (path != null && path.Count > 1)
                {
                    return false; // Not enclosed - reachable from edge
                }
            }
            
            // No path found from any edge - job is enclosed
            Debug.Log($"🔒 ENCLOSED FLOOR: {job.buildingType.displayName} at {jobPos} (no path from map edges)");
            return true;
        }

        /// <summary>
        /// Remove a completed job
        /// </summary>
        public void CompleteBuildJob(GhostBuilding ghostBuilding)
        {
            pendingJobs.Remove(ghostBuilding);
            
            // Remove from active jobs
            Worker workerToRemove = null;
            foreach (var kvp in activeJobs)
            {
                if (kvp.Value == ghostBuilding)
                {
                    workerToRemove = kvp.Key;
                    break;
                }
            }
            
            if (workerToRemove)
            {
                activeJobs.Remove(workerToRemove);
            }
            
            Debug.Log($"Completed build job: {ghostBuilding.buildingType.displayName}");
        }

        /// <summary>
        /// Main job assignment logic
        /// </summary>
        void UpdateJobAssignments()
        {
            if (pendingJobs.Count == 0) return;
            
            // Get available workers
            List<Worker> availableWorkers = GetAvailableWorkers();
            if (availableWorkers.Count == 0) return;
            
            // Assign jobs to workers (prioritized jobs first!)
            for (int i = 0; i < pendingJobs.Count && availableWorkers.Count > 0; i++)
            {
                GhostBuilding job = pendingJobs[i];
                if (!job || job.IsBeingBuilt) 
                {
                    pendingJobs.RemoveAt(i);
                    i--; // Adjust index after removal
                    continue;
                }
                
                // Find the closest available worker
                Worker closestWorker = FindClosestWorker(availableWorkers, job.transform.position);
                if (closestWorker)
                {
                    AssignJobToWorker(closestWorker, job);
                    availableWorkers.Remove(closestWorker);
                    pendingJobs.RemoveAt(i);
                    i--; // Adjust index after removal
                }
            }
        }

        /// <summary>
        /// Assign a specific job to a specific worker
        /// </summary>
        void AssignJobToWorker(Worker worker, GhostBuilding job)
        {
            activeJobs[worker] = job;
            worker.SetJob(job);
            
            Debug.Log($"Assigned job {job.buildingType.displayName} to worker {worker.name}");
        }

        /// <summary>
        /// Get all available workers from WorkerManager
        /// </summary>
        List<Worker> GetAvailableWorkers()
        {
            List<Worker> available = new List<Worker>();
            
            if (WorkerManager.Instance)
            {
                foreach (Worker worker in WorkerManager.Instance.Workers)
                {
                    if (worker && worker.IsAvailable && !activeJobs.ContainsKey(worker))
                    {
                        available.Add(worker);
                    }
                }
            }
            
            return available;
        }

        /// <summary>
        /// Find the closest worker to a given position
        /// </summary>
        Worker FindClosestWorker(List<Worker> workers, Vector3 targetPosition)
        {
            Worker closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (Worker worker in workers)
            {
                if (!worker) continue;
                
                float distance = Vector3.Distance(worker.transform.position, targetPosition);
                if (distance < closestDistance && distance <= maxJobDistance)
                {
                    closest = worker;
                    closestDistance = distance;
                }
            }
            
            return closest;
        }

        /// <summary>
        /// Called when a worker finishes or abandons a job
        /// </summary>
        public void WorkerFinishedJob(Worker worker)
        {
            if (activeJobs.ContainsKey(worker))
            {
                GhostBuilding job = activeJobs[worker];
                activeJobs.Remove(worker);
                
                // If job wasn't completed, add it back to pending
                if (job && !job.IsComplete)
                {
                    job.StopBuilding();
                    if (!pendingJobs.Contains(job))
                    {
                        pendingJobs.Add(job);
                    }
                }
            }
        }

        /// <summary>
        /// Called when a worker times out trying to reach a job - move job to back of queue
        /// </summary>
        public void WorkerTimedOutOnJob(Worker worker)
        {
            if (activeJobs.ContainsKey(worker))
            {
                GhostBuilding job = activeJobs[worker];
                activeJobs.Remove(worker);
                
                if (job && !job.IsComplete)
                {
                    job.StopBuilding();
                    // Move this job to the back of the queue
                    if (!pendingJobs.Contains(job))
                    {
                        pendingJobs.Add(job);
                        Debug.Log($"Moved unreachable job to back of queue: {job.buildingType.displayName}");
                    }
                }
            }
        }

        /// <summary>
        /// Get the current job for a worker
        /// </summary>
        public GhostBuilding GetWorkerJob(Worker worker)
        {
            activeJobs.TryGetValue(worker, out GhostBuilding job);
            return job;
        }

        /// <summary>
        /// Cancel all jobs (for testing/debugging)
        /// </summary>
        [ContextMenu("Cancel All Jobs")]
        public void CancelAllJobs()
        {
            foreach (var kvp in activeJobs)
            {
                if (kvp.Value) kvp.Value.StopBuilding();
                if (kvp.Key) kvp.Key.FinishJob();
            }
            
            activeJobs.Clear();
            pendingJobs.Clear();
            
            Debug.Log("Cancelled all jobs");
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 150));
            
            GUILayout.Label("=== Job Manager ===");
            GUILayout.Label($"Pending Jobs: {PendingJobsCount}");
            GUILayout.Label($"Active Jobs: {ActiveJobsCount}");
            
            if (GUILayout.Button("Cancel All Jobs"))
            {
                CancelAllJobs();
            }
            
            GUILayout.EndArea();
        }
    }
}

