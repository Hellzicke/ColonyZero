using System.Collections.Generic;
using UnityEngine;
using CT.Build;

namespace CT.Workers
{
    public class Worker : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 2f;
        public float rotationSpeed = 10f;
        public float buildRange = 1.5f;
        public float pathNodeReachDistance = 0.3f;
        public float workerAvoidanceRadius = 0.4f;
        public float avoidanceForce = 0.5f;

        [Header("Idle Behavior")]
        public float idleWanderRadius = 5f;
        public float minIdleTime = 2f;
        public float maxIdleTime = 8f;

        [Header("Debug")]
        public bool showDebugInfo = true;

        private WorkerState currentState = WorkerState.Idle;
        private Vector3 targetPosition;
        private Vector3 spawnPosition;
        private float idleTimer;
        private bool isMoving;
        private GhostBuilding currentJob;
        
        // Pathfinding
        private List<Vector3> currentPath;
        private int currentPathIndex;
        
        // Stuck detection and re-pathing
        private Vector3 lastPosition;
        private float stuckTimer;
        private float stuckThreshold = 0.8f; // Very aggressive stuck detection
        private float minMovementDistance = 0.1f;
        private float lastRepathTime;
        private float repathCooldown = 0.2f; // Allow more frequent re-pathing
        
        // Job timeout
        private float jobStartTime;
        private float jobTimeout = 5f; // Give up on job after 5 seconds if can't reach

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;

        public WorkerState CurrentState => currentState;
        public bool IsAvailable => currentState == WorkerState.Idle;
        public GhostBuilding CurrentJob => currentJob;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Configure rigidbody for grid-based movement
            if (rb)
            {
                rb.gravityScale = 0f;
                rb.drag = 8f; // Stop quickly when not moving
                rb.mass = 0.5f; // Lighter workers push less
            }
            
            // Ensure worker is always on top of other sprites
            if (spriteRenderer)
            {
                spriteRenderer.sortingLayerName = "Buildings";
                spriteRenderer.sortingOrder = 1000; // Very high value to stay on top of all buildings
            }
            
            // Set minimal collision radius
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider)
            {
                collider.radius = 0.05f; // Extremely small collision, just 5% of original
            }
        }

        void Start()
        {
            spawnPosition = transform.position;
            targetPosition = transform.position;
            SetRandomIdleTimer();
        }

        void Update()
        {
            // Skip stuck detection for now
            
            switch (currentState)
            {
                case WorkerState.Idle:
                    HandleIdleState();
                    break;
                case WorkerState.Moving:
                    HandleMovingState();
                    break;
                case WorkerState.MovingToJob:
                    HandleMovingToJobState();
                    break;
                case WorkerState.Working:
                    HandleWorkingState();
                    break;
            }

            if (showDebugInfo)
            {
                // Draw current path
                if (currentPath != null && currentPath.Count > 1)
                {
                    for (int i = 0; i < currentPath.Count - 1; i++)
                    {
                        Debug.DrawLine(currentPath[i], currentPath[i + 1], Color.cyan);
                    }
                }
                
                // Draw line to current job
                if (currentJob)
                {
                    Debug.DrawLine(transform.position, currentJob.transform.position, Color.red);
                }
            }
        }

        void CheckStuckState()
        {
            if (currentState == WorkerState.Moving || currentState == WorkerState.MovingToJob)
            {
                float distanceMoved = Vector3.Distance(transform.position, lastPosition);
                
                if (distanceMoved < minMovementDistance)
                {
                    stuckTimer += Time.deltaTime;
                    
                    if (stuckTimer >= stuckThreshold)
                    {
                        Debug.LogWarning($"Worker {name} appears stuck, trying to resolve...");
                        HandleStuckWorker();
                        stuckTimer = 0f;
                    }
                }
                else
                {
                    stuckTimer = 0f; // Reset if worker is moving
                }
                
                lastPosition = transform.position;
            }
            else
            {
                stuckTimer = 0f;
                lastPosition = transform.position;
            }
        }

        void HandleStuckWorker()
        {
            // Force re-pathing regardless of cooldown when stuck
            lastRepathTime = 0f;
            
            if (currentState == WorkerState.MovingToJob && currentJob)
            {
                // Try to get a new work position (maybe current one is blocked)
                Vector3 jobPosition = currentJob.GetWorkerPosition();
                currentPath = WorkerPathfinding.FindPath(transform.position, jobPosition);
                currentPathIndex = 0;
                
                if (currentPath.Count <= 1) // No valid path found
                {
                    Debug.LogWarning($"Worker {name} can't find path to job, abandoning");
                    FinishJob();
                }
                else
                {
                    Debug.Log($"Worker {name} found new path to job with {currentPath.Count} waypoints");
                }
            }
            else if (currentState == WorkerState.Moving)
            {
                // Try to find a new random target
                TryFindNewIdleTarget();
            }
        }

        void TryFindNewIdleTarget()
        {
            // Try to find a new walkable position nearby
            for (int attempts = 0; attempts < 5; attempts++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * idleWanderRadius;
                Vector3 newTarget = spawnPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
                
                if (GridMap.I != null)
                {
                    Vector2Int cell = GridMap.I.WorldToCell(newTarget);
                    BuildPlacer placer = FindObjectOfType<BuildPlacer>();
                    
                    if (placer && GridMap.I.InBounds(cell) && placer.IsWalkable(cell))
                    {
                        SetTarget(GridMap.I.CellToWorld(cell));
                        return; // Found a valid target
                    }
                }
            }
            
            // If we can't find anywhere to go, just go idle
            currentState = WorkerState.Idle;
            isMoving = false;
            if (rb) rb.velocity = Vector2.zero;
            SetRandomIdleTimer();
        }

        void HandleIdleState()
        {
            idleTimer -= Time.deltaTime;
            
            if (idleTimer <= 0f && !isMoving)
            {
                // Choose a random position within wander radius
                Vector2 randomOffset = Random.insideUnitCircle * idleWanderRadius;
                Vector3 newTarget = spawnPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
                
                // Snap to grid and use pathfinding
                if (GridMap.I != null)
                {
                    Vector2Int cell = GridMap.I.WorldToCell(newTarget);
                    if (GridMap.I.InBounds(cell))
                    {
                        newTarget = GridMap.I.CellToWorld(cell);
                        SetTarget(newTarget); // This will use pathfinding now
                    }
                }
                else
                {
                    SetTarget(newTarget);
                }
                
                SetRandomIdleTimer();
            }
        }

        void HandleMovingState()
        {
            FollowCurrentPath();
            
            // Check if we've completed the entire path
            if (currentPathIndex >= currentPath.Count)
            {
                currentState = WorkerState.Idle;
                isMoving = false;
                if (rb) rb.velocity = Vector2.zero;
                SetRandomIdleTimer();
            }
        }

        void HandleMovingToJobState()
        {
            if (!currentJob)
            {
                // Job was cancelled or completed by someone else
                FinishJob();
                return;
            }

            // Check for job timeout
            float timeOnJob = Time.time - jobStartTime;
            if (timeOnJob > jobTimeout)
            {
                Debug.LogWarning($"⏰ TIMEOUT: Worker {name} timed out after {timeOnJob:F1}s trying to reach job {currentJob.buildingType.displayName}, moving to back of queue");
                
                // Notify job manager to reorder queue
                if (JobManager.Instance)
                {
                    JobManager.Instance.WorkerTimedOutOnJob(this);
                }
                else
                {
                    Debug.LogError("JobManager.Instance is null!");
                }
                
                // Reset worker state to pick up next available job
                currentJob = null;
                currentState = WorkerState.Idle;
                isMoving = false;
                if (rb) rb.velocity = Vector2.zero;
                SetRandomIdleTimer();
                return;
            }
            // Removed timeout spam logging

            FollowCurrentPath();
            
            // Check if we've reached the end of our path (near the job)
            if (currentPathIndex >= currentPath.Count)
            {
                float distanceToJob = Vector3.Distance(transform.position, currentJob.transform.position);
                
                if (distanceToJob <= buildRange)
                {
                    // Close enough to start building
                    currentState = WorkerState.Working;
                    isMoving = false;
                    
                    if (rb) rb.velocity = Vector2.zero;
                    
                    // Start building
                    if (currentJob.StartBuilding(this))
                    {
                        Debug.Log($"Worker {name} started building");
                    }
                    else
                    {
                        // Someone else is already building this, or it's complete
                        FinishJob();
                    }
                }
                else
                {
                    // We reached the end of our path but we're not close enough
                    // Try to find a new path closer to the job
                    Vector3 jobPosition = currentJob.GetWorkerPosition();
                    currentPath = WorkerPathfinding.FindPath(transform.position, jobPosition);
                    currentPathIndex = 0;
                    
                    if (currentPath.Count <= 1)
                    {
                        Debug.LogWarning($"Worker {name} can't find new path to job");
                        // Don't give up immediately, let timeout handle it
                    }
                }
            }
        }

        void HandleWorkingState()
        {
            if (!currentJob)
            {
                FinishJob();
                return;
            }

            // Check if we're still in range
            float distance = Vector3.Distance(transform.position, currentJob.transform.position);
            if (distance > buildRange)
            {
                // Too far away, move closer using pathfinding
                Vector3 jobPosition = currentJob.GetWorkerPosition();
                currentPath = WorkerPathfinding.FindPath(transform.position, jobPosition);
                currentPathIndex = 0;
                
                currentState = WorkerState.MovingToJob;
                isMoving = true;
                currentJob.StopBuilding();
                return;
            }

            // Continue building
            currentJob.ContinueBuilding(Time.deltaTime);
            
            // Job will finish itself when complete and call FinishJob() on us
        }

        /// <summary>
        /// Follow the current path by moving towards the next waypoint
        /// </summary>
        void FollowCurrentPath()
        {
            if (currentPath == null || currentPath.Count == 0) return;
            
            // Get current target waypoint
            if (currentPathIndex >= currentPath.Count) return;
            
            Vector3 currentWaypoint = currentPath[currentPathIndex];
            float distance = Vector3.Distance(transform.position, currentWaypoint);
            
            if (distance <= pathNodeReachDistance)
            {
                // Reached this waypoint, move to next one
                currentPathIndex++;
                return;
            }
            
            // Calculate basic movement direction
            Vector3 direction = (currentWaypoint - transform.position).normalized;
            
            // Add minimal collision avoidance
            Vector3 avoidanceVector = CalculateWorkerAvoidance();
            Vector3 finalDirection = (direction + avoidanceVector * 0.1f).normalized; // Very light avoidance
            
            // Apply movement
            if (rb)
            {
                rb.velocity = finalDirection * moveSpeed;
            }
            else
            {
                transform.position += finalDirection * moveSpeed * Time.deltaTime;
            }
            
            // Face movement direction (use original direction, not avoidance-modified)
            if (direction.x != 0 && spriteRenderer)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        bool ShouldRepath()
        {
            // Don't repath too frequently
            if (Time.time - lastRepathTime < repathCooldown) return false;
            
            // Check if the next few waypoints are blocked
            if (currentPath != null && currentPathIndex < currentPath.Count)
            {
                BuildPlacer placer = FindObjectOfType<BuildPlacer>();
                if (!placer) return false;
                
                // Check next 2-3 waypoints to see if path is blocked
                int checkAhead = Mathf.Min(3, currentPath.Count - currentPathIndex);
                for (int i = 0; i < checkAhead; i++)
                {
                    Vector2Int cell = GridMap.I.WorldToCell(currentPath[currentPathIndex + i]);
                    if (!placer.IsWalkable(cell))
                    {
                        return true; // Path ahead is blocked
                    }
                }
            }
            
            return false;
        }

        void TryRepath()
        {
            lastRepathTime = Time.time;
            
            Vector3 finalTarget = currentPath != null && currentPath.Count > 0 ? 
                                  currentPath[currentPath.Count - 1] : targetPosition;
            
            List<Vector3> newPath = WorkerPathfinding.FindPath(transform.position, finalTarget);
            
            if (newPath.Count > 1) // Found a valid new path
            {
                currentPath = newPath;
                currentPathIndex = 0;
                Debug.Log($"Worker {name} found new path with {newPath.Count} waypoints");
            }
        }

        /// <summary>
        /// Calculate avoidance force to avoid other workers
        /// </summary>
        Vector3 CalculateWorkerAvoidance()
        {
            Vector3 avoidanceForceVector = Vector3.zero;
            
            // Find all other workers nearby
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, workerAvoidanceRadius);
            
            foreach (Collider2D col in nearbyColliders)
            {
                Worker otherWorker = col.GetComponent<Worker>();
                if (otherWorker && otherWorker != this)
                {
                    // Calculate repulsion force
                    Vector3 awayFromOther = transform.position - otherWorker.transform.position;
                    float distance = awayFromOther.magnitude;
                    
                    if (distance > 0.1f) // Avoid division by zero
                    {
                        // Stronger force when closer
                        float forceStrength = avoidanceForce / (distance * distance);
                        avoidanceForceVector += awayFromOther.normalized * forceStrength;
                    }
                }
            }
            
            return avoidanceForceVector;
        }

        /// <summary>
        /// Validate that movement direction won't go through walls
        /// </summary>
        Vector3 ValidateMovementDirection(Vector3 proposedDirection, Vector3 fallbackDirection)
        {
            BuildPlacer buildPlacer = FindObjectOfType<BuildPlacer>();
            if (!buildPlacer || !GridMap.I) return proposedDirection;
            
            // Check the cell we're trying to move into
            Vector3 nextPosition = transform.position + proposedDirection * moveSpeed * Time.deltaTime;
            Vector2Int nextCell = GridMap.I.WorldToCell(nextPosition);
            
            // If the next cell is walkable, use the proposed direction
            if (buildPlacer.IsWalkable(nextCell))
            {
                return proposedDirection;
            }
            
            // If not walkable, check if the fallback direction is safe
            Vector3 fallbackPosition = transform.position + fallbackDirection * moveSpeed * Time.deltaTime;
            Vector2Int fallbackCell = GridMap.I.WorldToCell(fallbackPosition);
            
            if (buildPlacer.IsWalkable(fallbackCell))
            {
                return fallbackDirection;
            }
            
            // Neither direction is safe, stop moving
            return Vector3.zero;
        }

        public void SetTarget(Vector3 target)
        {
            // Use pathfinding to find route to target
            currentPath = WorkerPathfinding.FindPath(transform.position, target);
            currentPathIndex = 0;
            
            if (currentPath.Count > 0)
            {
                targetPosition = currentPath[0];
                currentState = WorkerState.Moving;
                isMoving = true;
            }
            else
            {
                // No path found, stay where we are
                targetPosition = transform.position;
                currentState = WorkerState.Idle;
                isMoving = false;
            }
        }

        public void SetJob(GhostBuilding job)
        {
            if (!job) return;
            
            currentJob = job;
            jobStartTime = Time.time; // Start timeout timer
            
            // Use pathfinding to reach the job location
            Vector3 jobPosition = job.GetWorkerPosition();
            currentPath = WorkerPathfinding.FindPath(transform.position, jobPosition);
            currentPathIndex = 0;
            
            if (currentPath.Count > 1) // Need at least start + 1 waypoint
            {
                targetPosition = currentPath[0];
                currentState = WorkerState.MovingToJob;
                isMoving = true;
                Debug.Log($"Worker {name} assigned to build {job.buildingType.displayName}");
            }
            else
            {
                Debug.LogWarning($"Worker {name} couldn't find path to job {job.buildingType.displayName}, will timeout in {jobTimeout}s");
                // Don't give up immediately, let timeout handle it
                currentState = WorkerState.MovingToJob;
                isMoving = false;
            }
        }

        public void FinishJob()
        {
            currentJob = null;
            currentState = WorkerState.Idle;
            isMoving = false;
            SetRandomIdleTimer();
            
            // Notify job manager
            if (JobManager.Instance)
            {
                JobManager.Instance.WorkerFinishedJob(this);
            }
        }

        void SetRandomIdleTimer()
        {
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
        }

        void OnDrawGizmosSelected()
        {
            if (showDebugInfo)
            {
                // Draw spawn position and wander radius
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPosition, 0.2f);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(spawnPosition, idleWanderRadius);
                
                // Draw current target
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 0.15f);
                
                // Draw state info
                Gizmos.color = GetStateColor();
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }

        Color GetStateColor()
        {
            return currentState switch
            {
                WorkerState.Idle => Color.green,
                WorkerState.Moving => Color.blue,
                WorkerState.MovingToJob => Color.yellow,
                WorkerState.Working => Color.red,
                _ => Color.white
            };
        }
    }

    public enum WorkerState
    {
        Idle,
        Moving,
        MovingToJob,
        Working
    }
}
