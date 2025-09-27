using UnityEngine;

public class GridMap : MonoBehaviour
{
    public static GridMap I { get; private set; }

    [Header("Dimensions (cells)")]
    public int width = 128;
    public int height = 128;

    [Header("Cell size (world units)")]
    public Vector2 cellSize = Vector2.one; // 1x1 tile

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

    public Vector2Int WorldToCell(Vector3 world)
    {
        int x = Mathf.FloorToInt(world.x / cellSize.x);
        int y = Mathf.FloorToInt(world.y / cellSize.y);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        // center of cell
        return new Vector3((cell.x + 0.5f) * cellSize.x, (cell.y + 0.5f) * cellSize.y, 0f);
    }

    public Vector3 WorldCenter => new Vector3(width * cellSize.x * 0.5f, height * cellSize.y * 0.5f, 0f);
    public Vector2 WorldSize => new Vector2(width * cellSize.x, height * cellSize.y);
}

