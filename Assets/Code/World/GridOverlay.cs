using UnityEngine;

/// Dev overlay: draws grid lines; toggle with G. Optional coords with N.
public class GridOverlay : MonoBehaviour
{
    public GridMap grid;
    public Color lineColor = new Color(1, 1, 1, 0.15f);
    [Range(0.01f, 0.1f)] public float lineWidth = 0.03f;
    public bool show = true;

    Transform _hGroup, _vGroup;

    void Start()
    {
        if (!grid) grid = GridMap.I;
        Build();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) { show = !show; SetActive(show); }
    }

    void SetActive(bool on)
    {
        if (_hGroup) _hGroup.gameObject.SetActive(on);
        if (_vGroup) _vGroup.gameObject.SetActive(on);
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
    }

    public void Build()
    {
        ClearChildren();
        if (!grid) return;

        _hGroup = new GameObject("Horiz").transform; _hGroup.SetParent(transform, false);
        _vGroup = new GameObject("Vert").transform; _vGroup.SetParent(transform, false);

        var size = grid.WorldSize;

        // Horizontal lines (y from 0..height)
        for (int y = 0; y <= grid.height; y++)
            MakeLine(_hGroup, new Vector3(0, y, 0), new Vector3(size.x, y, 0));

        // Vertical lines (x from 0..width)
        for (int x = 0; x <= grid.width; x++)
            MakeLine(_vGroup, new Vector3(x, 0, 0), new Vector3(x, size.y, 0));

        SetActive(show);
    }

    void MakeLine(Transform parent, Vector3 a, Vector3 b)
    {
        var go = new GameObject("line");
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = lineColor;
        lr.sortingLayerName = "Floor"; // render beneath buildings/pawns
        lr.sortingOrder = 0;
    }
}
