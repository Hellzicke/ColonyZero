using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using CT.Buildings;
using CT.Build;

public class BuildPlacer : MonoBehaviour
{
    [Header("Palette (drag your ScriptableObjects here in order)")]
    public List<BuildingType> palette; // [0]=Floor, [1]=Wall, [2]=Door

    [Header("Scene refs")]
    public GridMap grid;
    public Transform floorParent;
    public Transform structureParent;

    [Header("Ghost")]
    public Color validColor = new Color(0f, 1f, 0f, 0.35f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.35f);

    private Camera cam;
    private int selectedIndex = 0;
    private GameObject ghostGO;
    private SpriteRenderer ghostSR;

    private GameObject[,] floors;
    private GameObject[,] structures;
    private GameObject[,] doors;  // separate array for doors that can overlay walls
    private GhostBuilding[,] ghostBuildings; // track ghost buildings waiting to be built
    private bool[,] walkable;

    void Awake()
    {
        cam = Camera.main;
        if (!grid) grid = GridMap.I;
        if (!grid) { Debug.LogError("BuildPlacer: GridMap not found."); enabled = false; return; }

        if (!floorParent)     floorParent     = new GameObject("Floor").transform;
        if (!structureParent) structureParent = new GameObject("Structure").transform;

        floors     = new GameObject[grid.width, grid.height];
        structures = new GameObject[grid.width, grid.height];
        doors      = new GameObject[grid.width, grid.height];
        ghostBuildings = new GhostBuilding[grid.width, grid.height];
        walkable   = new bool[grid.width, grid.height];
        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
                walkable[x, y] = true;

        ghostGO = new GameObject("Ghost");
        ghostSR = ghostGO.AddComponent<SpriteRenderer>();
        ghostSR.sortingLayerName = "Buildings";
        ghostSR.sortingOrder = 999;
        ghostGO.SetActive(false);

        Select(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);

        // Building placement always active (room system removed)

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        { ghostGO.SetActive(false); return; }

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        Vector2Int cell = grid.WorldToCell(world);
        if (!grid.InBounds(cell)) { ghostGO.SetActive(false); return; }

        var bt = GetSelectedType();
        if (bt == null) { ghostGO.SetActive(false); return; }

        var spr = GetPrimarySprite(bt);
        if (ghostSR.sprite != spr) ghostSR.sprite = spr;
        ghostGO.SetActive(true);
        ghostGO.transform.position = grid.CellToWorld(cell);

        bool canPlace = CanPlace(bt, cell);
        ghostSR.color = canPlace ? validColor : invalidColor;

        if (Input.GetMouseButton(0) && canPlace) 
        {
            // Only log door placements
            if (IsDoorType(bt))
            {
                Debug.Log($"🚪 DOOR CLICK: Placing {bt.displayName} at {cell}, existing wall: {structures[cell.x, cell.y]?.name}");
            }
            Place(bt, cell);
        }
        if (Input.GetMouseButton(1)) Bulldoze(cell);
    }

    // ===== helpers =====
    void Select(int index)
    {
        if (palette == null || palette.Count == 0) return;
        selectedIndex = Mathf.Clamp(index, 0, palette.Count - 1);
        var spr = GetPrimarySprite(GetSelectedType());
        if (spr) ghostSR.sprite = spr;
    }

    BuildingType GetSelectedType()
    {
        if (palette == null || palette.Count == 0) return null;
        return palette[Mathf.Clamp(selectedIndex, 0, palette.Count - 1)];
    }

    Sprite GetPrimarySprite(BuildingType bt)
    {
        if (!bt || !bt.prefab) return null;
        var sr = bt.prefab.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    bool CanPlace(BuildingType bt, Vector2Int c)
    {
        if (!grid.InBounds(c) || bt == null) return false;

        bool placingStructure = IsStructure(bt);
        var existingStruct = structures[c.x, c.y];
        var existingGhost = ghostBuildings[c.x, c.y];

        // Can't place if there's already a ghost building here
        if (existingGhost != null) return false;

        if (placingStructure)
        {
            if (existingStruct == null) return true;

            // allow door replacing wall
            if (IsDoorType(bt) && IsWallGO(existingStruct)) return true;

            return false;
        }
        else
        {
            return floors[c.x, c.y] == null;
        }
    }

    void Place(BuildingType bt, Vector2Int c)
    {
        bool placingStructure = IsStructure(bt);

        if (placingStructure)
        {
            if (IsDoorType(bt))
            {
                // Doors can only be placed if there's no existing door
                if (doors[c.x, c.y] != null) return;
                // Doors can be placed on walls or empty spaces
            }
            else
            {
                // Other structures (walls) can't be placed if there's already a structure
                if (structures[c.x, c.y] != null) return;
            }
        }

        // Create ghost building that needs to be constructed
        Vector3 pos = grid.CellToWorld(c);
        GameObject ghostGO = CreateGhostBuilding(bt, pos, c);
        
        if (ghostGO)
        {
            Transform parent = placingStructure ? structureParent : floorParent;
            ghostGO.transform.SetParent(parent);
            
            GhostBuilding ghost = ghostGO.GetComponent<GhostBuilding>();
            if (ghost)
            {
                ghostBuildings[c.x, c.y] = ghost;
                Debug.Log($"Placed ghost building: {bt.displayName} at {c}");
            }
        }
    }

    GameObject CreateGhostBuilding(BuildingType bt, Vector3 position, Vector2Int gridPos)
    {
        // Create a ghost version of the building
        GameObject ghostGO = new GameObject($"Ghost_{bt.displayName}_{gridPos.x}_{gridPos.y}");
        ghostGO.transform.position = position;
        
        // Add sprite renderer with the building's sprite
        SpriteRenderer sr = ghostGO.AddComponent<SpriteRenderer>();
        Sprite buildingSprite = GetPrimarySprite(bt);
        if (buildingSprite)
        {
            sr.sprite = buildingSprite;
        }
        
        // Add ghost building component
        GhostBuilding ghost = ghostGO.AddComponent<GhostBuilding>();
        ghost.buildingType = bt;
        ghost.gridPosition = gridPos;
        
        return ghostGO;
    }

    void Bulldoze(Vector2Int c)
    {
        if (!grid.InBounds(c)) return;

        // First check for ghost buildings
        if (ghostBuildings[c.x, c.y] != null)
        {
            var ghost = ghostBuildings[c.x, c.y];
            Destroy(ghost.gameObject);
            ghostBuildings[c.x, c.y] = null;
            Debug.Log($"Cancelled ghost building at {c}");
            return;
        }

        // Then check for doors (on top layer)
        if (doors[c.x, c.y] != null)
        {
            var door = doors[c.x, c.y];
            Destroy(door);
            doors[c.x, c.y] = null;
            
            // Update walkability based on what's underneath
            bool hasWall = structures[c.x, c.y] != null;
            walkable[c.x, c.y] = !hasWall; // walkable if no wall underneath
            
            WallManager.RefreshAround(c);
            return;
        }
        
        // Then check for structures (walls)
        if (structures[c.x, c.y] != null)
        {
            var go = structures[c.x, c.y];
            bool wasWall = IsWallGO(go);

            Destroy(go);
            structures[c.x, c.y] = null;
            walkable[c.x, c.y] = true;

            if (wasWall) WallManager.RefreshAround(c); // update neighbors
            return;
        }

        if (floors[c.x, c.y] != null)
        {
            Destroy(floors[c.x, c.y]);
            floors[c.x, c.y] = null;
        }
    }

    /// <summary>
    /// Called by GhostBuilding when construction is completed
    /// </summary>
    public void RegisterCompletedBuilding(BuildingType bt, Vector2Int c, GameObject realBuilding)
    {
        if (!grid.InBounds(c)) return;
        
        // Clear the ghost building reference
        ghostBuildings[c.x, c.y] = null;
        
        bool placingStructure = IsStructure(bt);
        
        if (placingStructure)
        {
            if (IsDoorType(bt))
            {
                doors[c.x, c.y] = realBuilding;
                walkable[c.x, c.y] = true; // doors are walkable
            }
            else
            {
                structures[c.x, c.y] = realBuilding;
                // Walls should always block movement, regardless of BuildingType setting
                bool isWall = IsWallGO(realBuilding) || bt.displayName.ToLower().Contains("wall");
                walkable[c.x, c.y] = !isWall; // walls block movement
                
                Debug.Log($"Completed building {bt.displayName} at {c} - walkable: {walkable[c.x, c.y]} (isWall: {isWall})");
            }

            // If it's a wall, refresh neighbors
            if (IsWallGO(realBuilding)) WallManager.RefreshAround(c);
            if (IsDoorGO(realBuilding)) WallManager.RefreshAround(c);
        }
        else
        {
            floors[c.x, c.y] = realBuilding;
        }
        
        Debug.Log($"Registered completed building: {bt.displayName} at {c}");
    }

    // ===== classification =====
    public bool IsStructure(BuildingType bt)
    {
        // Treat anything that might block movement (walls/doors) as "structure".
        // Floors are not structures.
        return bt.blocksMovement || bt.displayName.ToLower().Contains("door") || bt.displayName.ToLower().Contains("wall");
    }

    bool IsDoorType(BuildingType bt)
    {
        if (bt == null || bt.prefab == null) return false;
        if (bt.prefab.GetComponent<DoorTag>() != null) return true;
        var n = bt.displayName?.ToLower();
        return n != null && n.Contains("door");
    }

    bool IsWallGO(GameObject go) => go && go.GetComponent<WallTag>() != null;
    bool IsDoorGO(GameObject go) => go && go.GetComponent<DoorTag>() != null;

    // Public for your pathfinder and debug overlay
    public bool IsWalkable(Vector2Int c) => grid.InBounds(c) && walkable[c.x, c.y];
    public GameObject GetStructure(Vector2Int c) => grid.InBounds(c) ? structures[c.x, c.y] : null;
    public GameObject GetFloor(Vector2Int c) => grid.InBounds(c) ? floors[c.x, c.y] : null;
    public GameObject GetDoor(Vector2Int c) => grid.InBounds(c) ? doors[c.x, c.y] : null;

    // Debug method to visualize walkability
    void OnDrawGizmos()
    {
        if (!grid || walkable == null) return;
        
        Vector3 cameraPos = Camera.main ? Camera.main.transform.position : Vector3.zero;
        float viewDistance = 20f; // Only draw gizmos near camera
        
        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Vector3 worldPos = grid.CellToWorld(new Vector2Int(x, y));
                
                // Only draw if close to camera
                if (Vector3.Distance(worldPos, cameraPos) > viewDistance) continue;
                
                if (!walkable[x, y])
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                }
            }
        }
    }
}


