using UnityEngine;
using CT.Buildings;
using CT.Workers;

namespace CT.Build
{
    /// <summary>
    /// Component attached to ghost buildings that need to be constructed by workers
    /// </summary>
    public class GhostBuilding : MonoBehaviour
    {
        [Header("Building Info")]
        public BuildingType buildingType;
        public Vector2Int gridPosition;
        public float buildProgress = 0f;
        
        [Header("Visual")]
        public Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
        public Color buildingColor = new Color(0f, 1f, 0f, 0.7f);
        
        private SpriteRenderer spriteRenderer;
        private bool isBeingBuilt = false;
        private Worker currentWorker;

        public bool IsComplete => buildProgress >= 1f;
        public bool IsBeingBuilt => isBeingBuilt;
        public Worker CurrentWorker => currentWorker;
        public float BuildTimeRequired => buildingType ? buildingType.buildSeconds : 1f;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer)
            {
                spriteRenderer.color = ghostColor;
                spriteRenderer.sortingOrder = 5; // Above normal buildings but below UI
            }
        }

        void Start()
        {
            // Register this ghost building with the job system
            if (JobManager.Instance)
            {
                JobManager.Instance.AddBuildJob(this);
            }
        }

        /// <summary>
        /// Called by a worker to start building this structure
        /// </summary>
        public bool StartBuilding(Worker worker)
        {
            if (isBeingBuilt || IsComplete) return false;
            
            // Check if building this would trap the worker
            if (WouldTrapWorker(worker))
            {
                Debug.LogWarning($"Worker {worker.name} would be trapped by building {buildingType.displayName}, skipping");
                return false;
            }
            
            isBeingBuilt = true;
            currentWorker = worker;
            
            if (spriteRenderer)
            {
                spriteRenderer.color = buildingColor;
            }
            
            Debug.Log($"Worker {worker.name} started building {buildingType.displayName} at {gridPosition}");
            return true;
        }

        /// <summary>
        /// Check if completing this building would trap the worker
        /// </summary>
        bool WouldTrapWorker(Worker worker)
        {
            BuildPlacer placer = FindObjectOfType<BuildPlacer>();
            if (!placer || !GridMap.I) return false;
            
            // Only check for walls, not floors
            bool isWall = buildingType.blocksMovement || buildingType.displayName.ToLower().Contains("wall");
            if (!isWall) return false;
            
            // Temporarily mark this cell as non-walkable to test pathfinding
            Vector2Int workerCell = GridMap.I.WorldToCell(worker.transform.position);
            
            // Find a path from worker's position to outside the enclosed area
            Vector2Int[] exitTargets = {
                workerCell + Vector2Int.up * 10,
                workerCell + Vector2Int.down * 10,
                workerCell + Vector2Int.left * 10,
                workerCell + Vector2Int.right * 10
            };
            
            // Simulate building this wall
            bool originalWalkable = placer.IsWalkable(gridPosition);
            // We can't actually modify the walkable array here, so we'll use a simple heuristic
            
            // Simple check: if worker is adjacent to this wall position and there are walls on 3+ sides, likely trapped
            int adjacentWalls = 0;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int checkPos = workerCell + dir;
                if (checkPos == gridPosition) // This is the wall we're about to build
                {
                    adjacentWalls++;
                }
                else if (!placer.IsWalkable(checkPos))
                {
                    adjacentWalls++;
                }
            }
            
            // If 3 or more sides would be blocked, worker might be trapped
            return adjacentWalls >= 3;
        }

        /// <summary>
        /// Called by worker each frame while building
        /// </summary>
        public void ContinueBuilding(float deltaTime)
        {
            if (!isBeingBuilt || IsComplete) return;
            
            buildProgress += deltaTime / BuildTimeRequired;
            buildProgress = Mathf.Clamp01(buildProgress);
            
            // Update visual progress
            if (spriteRenderer)
            {
                Color currentColor = Color.Lerp(buildingColor, Color.white, buildProgress);
                currentColor.a = Mathf.Lerp(0.7f, 1f, buildProgress);
                spriteRenderer.color = currentColor;
            }
            
            // Check if building is complete
            if (IsComplete)
            {
                CompleteBuilding();
            }
        }

        /// <summary>
        /// Called when building is finished
        /// </summary>
        void CompleteBuilding()
        {
            Debug.Log($"Building {buildingType.displayName} completed at {gridPosition}");
            
            // Spawn the actual building
            GameObject realBuilding = Instantiate(buildingType.prefab, transform.position, transform.rotation);
            realBuilding.name = $"{buildingType.displayName}_{gridPosition.x}_{gridPosition.y}";
            
            // Set parent to the appropriate container
            BuildPlacer placer = FindObjectOfType<BuildPlacer>();
            if (placer)
            {
                bool isStructure = placer.IsStructure(buildingType);
                Transform parent = isStructure ? placer.structureParent : placer.floorParent;
                if (parent) realBuilding.transform.SetParent(parent);
                
                // Update the BuildPlacer's arrays
                placer.RegisterCompletedBuilding(buildingType, gridPosition, realBuilding);
            }
            
            // Notify the job system
            if (JobManager.Instance)
            {
                JobManager.Instance.CompleteBuildJob(this);
            }
            
            // Free the worker
            if (currentWorker)
            {
                currentWorker.FinishJob();
            }
            
            // Destroy the ghost
            Destroy(gameObject);
        }

        /// <summary>
        /// Called when worker stops building (interrupted)
        /// </summary>
        public void StopBuilding()
        {
            if (!isBeingBuilt) return;
            
            isBeingBuilt = false;
            currentWorker = null;
            
            if (spriteRenderer)
            {
                spriteRenderer.color = ghostColor;
            }
            
            Debug.Log($"Building {buildingType.displayName} at {gridPosition} construction stopped");
        }

        /// <summary>
        /// Get the world position for workers to stand when building
        /// </summary>
        public Vector3 GetWorkerPosition()
        {
            // Try to find a good position around the building
            Vector3 buildPos = transform.position;
            
            // Define potential work positions around the building
            Vector3[] potentialPositions = {
                buildPos + Vector3.down * 0.8f,      // South
                buildPos + Vector3.up * 0.8f,       // North  
                buildPos + Vector3.left * 0.8f,     // West
                buildPos + Vector3.right * 0.8f,    // East
                buildPos + new Vector3(-0.6f, -0.6f, 0f), // Southwest
                buildPos + new Vector3(0.6f, -0.6f, 0f),  // Southeast
                buildPos + new Vector3(-0.6f, 0.6f, 0f),  // Northwest
                buildPos + new Vector3(0.6f, 0.6f, 0f)    // Northeast
            };
            
            // Check each position for availability and walkability
            BuildPlacer placer = FindObjectOfType<BuildPlacer>();
            if (placer && GridMap.I)
            {
                foreach (Vector3 pos in potentialPositions)
                {
                    Vector2Int cell = GridMap.I.WorldToCell(pos);
                    if (GridMap.I.InBounds(cell) && placer.IsWalkable(cell))
                    {
                        // Check if another worker is already near this position
                        bool positionFree = true;
                        Collider2D[] nearby = Physics2D.OverlapCircleAll(pos, 0.3f);
                        
                        foreach (Collider2D col in nearby)
                        {
                            if (col.GetComponent<Worker>())
                            {
                                positionFree = false;
                                break;
                            }
                        }
                        
                        if (positionFree)
                        {
                            return pos;
                        }
                    }
                }
            }
            
            // Fallback to default position if no good spot found
            return buildPos + Vector3.down * 0.8f;
        }

        void OnDrawGizmosSelected()
        {
            // Draw build progress
            Gizmos.color = Color.yellow;
            Vector3 pos = transform.position;
            Vector3 size = Vector3.one;
            
            // Progress bar above the building
            Vector3 barPos = pos + Vector3.up * 0.6f;
            Gizmos.DrawWireCube(barPos, new Vector3(1f, 0.1f, 0f));
            
            if (buildProgress > 0f)
            {
                Gizmos.color = Color.green;
                Vector3 fillSize = new Vector3(buildProgress, 0.1f, 0f);
                Vector3 fillPos = barPos + Vector3.left * (1f - buildProgress) * 0.5f;
                Gizmos.DrawCube(fillPos, fillSize);
            }
            
            // Worker position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(GetWorkerPosition(), 0.2f);
        }
    }
}
