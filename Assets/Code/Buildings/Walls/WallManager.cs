using System.Collections.Generic;
using UnityEngine;

namespace CT.Buildings
{
    public static class WallManager
    {
        // All walls by cell
        private static readonly Dictionary<Vector2Int, WallAutoTile> walls = new Dictionary<Vector2Int, WallAutoTile>();
        public static GridMap Grid => GridMap.I;

        public static void Register(WallAutoTile tile, Vector2Int cell)
        {
            walls[cell] = tile;
            // Refresh this and neighbors on register
            RefreshAt(cell);
            RefreshNeighbors(cell);
        }

        public static void Unregister(Vector2Int cell)
        {
            if (walls.ContainsKey(cell))
            {
                walls.Remove(cell);
                // Refresh neighbors when removed
                RefreshNeighbors(cell);
            }
        }

        public static bool HasWall(Vector2Int cell) => walls.ContainsKey(cell);

        public static void RefreshAt(Vector2Int cell)
        {
            if (walls.TryGetValue(cell, out var tile) && tile) tile.Refresh();
        }

        public static void RefreshNeighbors(Vector2Int cell)
        {
            RefreshAt(cell + Vector2Int.up);
            RefreshAt(cell + Vector2Int.right);
            RefreshAt(cell + Vector2Int.down);
            RefreshAt(cell + Vector2Int.left);
        }

        // Call when something non-wall changed that affects walls (e.g., placing a door)
        public static void RefreshAround(Vector2Int cell)
        {
            RefreshAt(cell);
            RefreshNeighbors(cell);
        }
    }
}

