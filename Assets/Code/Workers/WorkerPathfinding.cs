using System.Collections.Generic;
using UnityEngine;

namespace CT.Workers
{
    /// <summary>
    /// Simple A* pathfinding for workers that respects building walkability
    /// </summary>
    public static class WorkerPathfinding
    {
        private class PathNode
        {
            public Vector2Int position;
            public float gCost; // Distance from start
            public float hCost; // Distance to target
            public float fCost => gCost + hCost;
            public PathNode parent;

            public PathNode(Vector2Int pos)
            {
                position = pos;
            }
        }

        /// <summary>
        /// Find a path from start to target position using A* algorithm
        /// </summary>
        public static List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
        {
            if (!GridMap.I)
            {
                Debug.LogWarning("WorkerPathfinding: No GridMap found!");
                return new List<Vector3> { targetWorld };
            }

            BuildPlacer buildPlacer = Object.FindObjectOfType<BuildPlacer>();
            if (!buildPlacer)
            {
                Debug.LogWarning("WorkerPathfinding: No BuildPlacer found!");
                return new List<Vector3> { targetWorld };
            }

            // Convert world positions to grid coordinates
            Vector2Int startGrid = GridMap.I.WorldToCell(startWorld);
            Vector2Int targetGrid = GridMap.I.WorldToCell(targetWorld);

            // If start or target is out of bounds, return direct path
            if (!GridMap.I.InBounds(startGrid) || !GridMap.I.InBounds(targetGrid))
            {
                return new List<Vector3> { targetWorld };
            }

            // If target is not walkable, find closest walkable cell
            if (!IsWalkableForWorker(buildPlacer, targetGrid))
            {
                targetGrid = FindClosestWalkableCell(buildPlacer, targetGrid);
                if (targetGrid == Vector2Int.one * -1) // No walkable cell found
                {
                    return new List<Vector3> { targetWorld };
                }
            }

            // Run A* pathfinding
            List<Vector2Int> gridPath = AStar(buildPlacer, startGrid, targetGrid);
            
            // Debug logging (only for very short paths that might indicate problems)
            if (gridPath.Count <= 1)
            {
                Debug.LogWarning($"Pathfinding from {startGrid} to {targetGrid}: found very short path with {gridPath.Count} waypoints");
            }
            
            // Convert grid path to world positions
            List<Vector3> worldPath = new List<Vector3>();
            foreach (Vector2Int gridPos in gridPath)
            {
                worldPath.Add(GridMap.I.CellToWorld(gridPos));
            }

            return worldPath;
        }

        /// <summary>
        /// Check if a cell is walkable for workers (includes ghost buildings)
        /// </summary>
        static bool IsWalkableForWorker(BuildPlacer buildPlacer, Vector2Int cell)
        {
            if (!GridMap.I.InBounds(cell)) return false;
            
            bool isWalkable = buildPlacer.IsWalkable(cell);
            
            // Debug logging disabled to prevent spam
            // Debug.Log($"Cell {cell} not walkable - has structure: {structure.name}");
            
            return isWalkable;
        }

        /// <summary>
        /// A* pathfinding algorithm
        /// </summary>
        static List<Vector2Int> AStar(BuildPlacer buildPlacer, Vector2Int start, Vector2Int target)
        {
            List<PathNode> openSet = new List<PathNode>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            
            PathNode startNode = new PathNode(start);
            startNode.gCost = 0;
            startNode.hCost = GetDistance(start, target);
            openSet.Add(startNode);

            int maxIterations = 1000; // Prevent infinite loops
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                // Get node with lowest fCost
                PathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || 
                        (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);

                // Check if we reached the target
                if (currentNode.position == target)
                {
                    List<Vector2Int> path = ReconstructPath(currentNode);
                    Debug.Log($"A* found path in {iterations} iterations with {path.Count} waypoints");
                    return path;
                }

                // Check all neighbors
                foreach (Vector2Int neighbor in GetNeighbors(currentNode.position))
                {
                    if (closedSet.Contains(neighbor) || !IsWalkableForWorker(buildPlacer, neighbor))
                    {
                        continue;
                    }

                    float newGCost = currentNode.gCost + GetDistance(currentNode.position, neighbor);
                    PathNode neighborNode = openSet.Find(n => n.position == neighbor);

                    if (neighborNode == null)
                    {
                        neighborNode = new PathNode(neighbor);
                        neighborNode.gCost = newGCost;
                        neighborNode.hCost = GetDistance(neighbor, target);
                        neighborNode.parent = currentNode;
                        openSet.Add(neighborNode);
                    }
                    else if (newGCost < neighborNode.gCost)
                    {
                        neighborNode.gCost = newGCost;
                        neighborNode.parent = currentNode;
                    }
                }
            }

            // No path found
            Debug.LogWarning($"A* failed to find path from {start} to {target} after {iterations} iterations");
            return new List<Vector2Int> { start };
        }

        /// <summary>
        /// Get neighboring cells (8-directional movement for better pathfinding)
        /// </summary>
        static List<Vector2Int> GetNeighbors(Vector2Int cell)
        {
            return new List<Vector2Int>
            {
                // Cardinal directions
                cell + Vector2Int.up,
                cell + Vector2Int.down,
                cell + Vector2Int.left,
                cell + Vector2Int.right,
                // Diagonal directions
                cell + new Vector2Int(1, 1),   // NE
                cell + new Vector2Int(-1, 1),  // NW
                cell + new Vector2Int(1, -1),  // SE
                cell + new Vector2Int(-1, -1)  // SW
            };
        }

        /// <summary>
        /// Calculate distance between two grid positions (using Euclidean for 8-directional)
        /// </summary>
        static float GetDistance(Vector2Int a, Vector2Int b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Reconstruct the path from target back to start
        /// </summary>
        static List<Vector2Int> ReconstructPath(PathNode targetNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PathNode currentNode = targetNode;

            while (currentNode != null)
            {
                path.Add(currentNode.position);
                currentNode = currentNode.parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Find the closest walkable cell to a target position
        /// </summary>
        static Vector2Int FindClosestWalkableCell(BuildPlacer buildPlacer, Vector2Int target)
        {
            int maxSearchRadius = 10;
            
            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius) // Only check perimeter
                        {
                            Vector2Int candidate = target + new Vector2Int(x, y);
                            if (IsWalkableForWorker(buildPlacer, candidate))
                            {
                                return candidate;
                            }
                        }
                    }
                }
            }

            return Vector2Int.one * -1; // No walkable cell found
        }
    }
}
