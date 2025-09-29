using UnityEngine;

/// <summary>
/// Wall piece component that inherits from WallTag.
/// Provides wall-specific functionality while maintaining tag identification.
/// </summary>
public class WallPiece : WallTag 
{
    [Header("Wall Specific")]
    public int health = 100;
    public float repairCost = 10f;
    
    void Start()
    {
        // Ensure wall properties are set
        canConnectToOtherWalls = true;
        blocksSight = true;
        useAutoTiling = true;
    }
    
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Wall destroyed - could trigger collapse, explosion, etc.
            DestroyWall();
        }
    }
    
    public void RepairWall()
    {
        health = 100; // or max health value
    }
    
    private void DestroyWall()
    {
        // Find BuildPlacer and call Bulldoze on this position
        var buildPlacer = FindObjectOfType<BuildPlacer>();
        if (buildPlacer)
        {
            Vector3 worldPos = transform.position;
            Vector2Int cell = GridMap.I.WorldToCell(worldPos);
            // This would be called by external systems, not directly bulldoze
        }
        
        Destroy(gameObject);
    }
}

