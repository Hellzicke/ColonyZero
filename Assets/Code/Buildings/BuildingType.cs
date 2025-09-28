using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingType", menuName = "CT/Building Type")]
public class BuildingType : ScriptableObject
{
    [Header("UI")]
    public string displayName;
    public Sprite icon;

    [Header("Placement")]
    public GameObject prefab;          // Assign your Floor/Wall/Door prefab here
    public Vector2Int size = Vector2Int.one; // 1x1, 1x2, etc.
    public bool blocksMovement = false;

    [Header("Build")]
    public float buildSeconds = 1f;
}

