using UnityEngine;
using CT.Buildings;

/// <summary>
/// Tag component to identify wall objects in the building system.
/// Used by BuildPlacer for type checking and wall auto-joining logic.
/// </summary>
public class WallTag : MonoBehaviour 
{
    [Header("Wall Properties")]
    public bool canConnectToOtherWalls = true;
    public bool blocksSight = true;
    
    [Header("Auto-Tiling")]
    public bool useAutoTiling = true;
    
    /// <summary>
    /// Called when this wall should refresh its sprite based on neighbors
    /// </summary>
    public void RefreshWallSprite()
    {
        if (!useAutoTiling) return;
        
        var autoTiler = GetComponent<WallAutoTile>();
        if (autoTiler)
        {
            autoTiler.Refresh();
        }
    }
    
    /// <summary>
    /// Check if this wall can connect to another wall
    /// </summary>
    public bool CanConnectTo(WallTag otherWall)
    {
        if (!otherWall || !canConnectToOtherWalls) return false;
        return otherWall.canConnectToOtherWalls;
    }
}
