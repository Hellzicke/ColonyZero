using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildPlacer : MonoBehaviour
{
    [Header("Data")]
    public List<BuildingType> palette;  // drag Floor.asset, Wall.asset, Door.asset here (in this order)

    [Header("Scene Refs")]
    public GridMap grid;                // drag your GridMap (or it will auto-grab GridMap.I)
    public Transform floorParent;       // empty GameObject named "Floor"
    public Transform structureParent;   // empty GameObject named "Structure"

    [Header("Ghost")]
    public Color validColor = new Color(0f, 1f, 0f, 0.35f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.35f);

    Camera cam;
    int selected = 0; // 0=Floor, 1=Wall, 2=Door (match your palette order)
    GameObject ghostGO;
    SpriteRenderer ghostSR;

    // occupancy & objects per cell
    GameObject[,] floors;
    GameObject[,] structures;
    bool[,] walkable;

    void Awake()
    {
        cam = Camera.main;
        if (!grid) grid = GridMap.I;

        floors = new GameObject[grid.width, grid.height];
        structures = new GameObject[grid.width, grid.height];
        walkable = new bool[grid.width, grid.height];
        for (int x = 0; x < grid.width; x++) for (int y = 0; y < grid.height; y++) walkable[x, y] = true;

        // parents (optional)
        if (!floorParent)
        {
            var f = new GameObject("Floor");
            floorParent = f.transform;
        }
        if (!structureParent)
        {
            var s = new GameObject("Structure");
            structureParent = s.transform;
        }

        // ghost
        ghostGO = new GameObject("Ghost");
        ghostSR = ghostGO.AddComponent<SpriteRenderer>();
        ghostSR.sortingLayerName = "Buildings";
        ghostSR.sortingOrder = 999;
        ghostGO.SetActive(false);

        Select(0); // default to Floor
    }

    void Update()
    {
        // hotkeys 1/2/3 to swap
        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Select(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Select(2);

        // don't place through UI
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        { ghostGO.SetActive(false); return; }

        // mouse → cell
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0;
        Vector2Int cell = grid.WorldToCell(world);
        if (!grid.InBounds(cell)) { ghostGO.SetActive(false); return; }

        // show ghost
        var bt = palette[selected];
        if (ghostSR.sprite == null || ghostSR.sprite != GetPrimarySprite(bt))
            ghostSR.sprite = GetPrimarySprite(bt);

        ghostGO.SetActive(true);
        ghostGO.transform.position = grid.CellToWorld(cell);
        bool canPlace = CanPlace(bt, cell);
        ghostSR.color = canPlace ? validColor : invalidColor;

        // LMB = paint / RMB = bulldoze
        if (Input.GetMouseButton(0) && canPlace)
            Place(bt, cell);
        if (Input.GetMouseButton(1))
            Bulldoze(cell);
    }

    void Select(int index)
    {
        selected = Mathf.Clamp(index, 0, palette.Count - 1);
        // set ghost sprite immediately if possible
        var bt = palette[selected];
        var spr = GetPrimarySprite(bt);
        if (spr) ghostSR.sprite = spr;
    }

    Sprite GetPrimarySprite(BuildingType bt)
    {
        if (!bt || !bt.prefab) return null;
        var sr = bt.prefab.GetComponentInChildren<SpriteRenderer>();
        return sr ? sr.sprite : null;
    }

    bool CanPlace(BuildingType bt, Vector2Int c)
    {
        // This guide is for 1x1 items. If you make bigger items later, extend this to check the footprint rect.
        if (!grid.InBounds(c)) return false;

        if (bt == null) return false;

        if (IsStructure(bt))
        {
            // Check if there's already a structure at this position
            var existingStructure = structures[c.x, c.y];
            if (existingStructure != null)
            {
                // Allow doors to replace walls (doors are openings in walls)
                if (IsDoor(bt) && IsWallAt(c))
                {
                    return true; // Allow door placement on wall
                }
                // Otherwise, don't allow multiple structures in same cell
                return false;
            }
        }
        else
        {
            // one floor per cell
            if (floors[c.x, c.y] != null) return false;
        }

        // Extra rules can go here (e.g., forbid door if no adjacent walls). For now, allow.
        return true;
    }

    bool IsStructure(BuildingType bt)
    {
        // Treat anything that might block movement (walls/doors) as "structure".
        // Floors are not structures.
        return bt.blocksMovement || bt.displayName.ToLower().Contains("door") || bt.displayName.ToLower().Contains("wall");
    }

    bool IsDoor(BuildingType bt)
    {
        return bt.displayName.ToLower().Contains("door");
    }

    bool IsWallAt(Vector2Int c)
    {
        if (!grid.InBounds(c)) return false;
        var go = structures[c.x, c.y];
        if (!go) return false;
        
        // Check if the existing structure is a wall (blocks movement)
        // We can also check the name or component for more precise identification
        return go.name.ToLower().Contains("wall") || go.GetComponent<WallPiece>() != null;
    }

    void Place(BuildingType bt, Vector2Int c)
    {
        // If placing a door on a wall, destroy the existing wall first
        if (IsStructure(bt) && IsDoor(bt) && IsWallAt(c))
        {
            var existingWall = structures[c.x, c.y];
            if (existingWall != null)
            {
                Destroy(existingWall);
                structures[c.x, c.y] = null;
            }
        }

        // spawn
        Transform parent = IsStructure(bt) ? structureParent : floorParent;
        GameObject go = Instantiate(bt.prefab, grid.CellToWorld(c), Quaternion.identity, parent);
        go.name = $"{bt.displayName}_{c.x}_{c.y}";

        // register & walkability
        if (IsStructure(bt))
        {
            structures[c.x, c.y] = go;
            // wall blocks, door does not (we trust bt.blocksMovement)
            walkable[c.x, c.y] = !bt.blocksMovement;
            // If you implement auto-joining walls (next step), call RefreshWallsAround(c) here
        }
        else
        {
            floors[c.x, c.y] = go;
            // no change to walkable
        }
    }

    void Bulldoze(Vector2Int c)
    {
        // remove structure first if present; else remove floor
        if (structures[c.x, c.y] != null)
        {
            Destroy(structures[c.x, c.y]);
            structures[c.x, c.y] = null;
            // restore walkability (true if no other blocker)
            walkable[c.x, c.y] = true;
            // If auto-walls: RefreshWallsAround(c)
            return;
        }
        if (floors[c.x, c.y] != null)
        {
            Destroy(floors[c.x, c.y]);
            floors[c.x, c.y] = null;
        }
    }

    // === Public helpers you can expose to your pathfinder later ===
    public bool IsWalkable(Vector2Int c)
    {
        if (!grid.InBounds(c)) return false;
        return walkable[c.x, c.y];
    }

    public bool HasWall(Vector2Int c)
    {
        if (!grid.InBounds(c)) return false;
        var go = structures[c.x, c.y];
        if (!go) return false;
        // crude: assume "blocksMovement==true" means wall-like
        return true; // refine later if needed
    }
}

