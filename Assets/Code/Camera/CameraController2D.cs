using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public float panSpeed = 18f;          // WASD/Arrows
    public float dragPanSpeed = 1.0f;     // Middle-mouse drag factor
    public float speedBoost = 2f;         // Shift to boost

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minOrtho = 4f;
    public float maxOrtho = 40f;
    public bool zoomToMouse = true;

    [Header("Clamp to Grid")]
    public bool clampToGrid = true;
    public float boundsMargin = 0.5f;

    Camera cam;
    bool dragging;
    Vector3 lastMouse;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        // Center on grid if present
        if (GridMap.I) transform.position = GridMap.I.WorldCenter + new Vector3(0, 0, -10);
    }

    void Update()
    {
        // Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 before = cam.ScreenToWorldPoint(Input.mousePosition);
            float target = Mathf.Clamp(cam.orthographicSize * Mathf.Exp(-scroll * (zoomSpeed * 0.1f)), minOrtho, maxOrtho);
            cam.orthographicSize = target;
            if (zoomToMouse)
            {
                Vector3 after = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 delta = before - after;
                transform.position += new Vector3(delta.x, delta.y, 0f);
            }
        }

        // Pan: WASD / Arrows
        Vector2 pan = new(
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
            (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) -
            (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)
        );
        if (pan.sqrMagnitude > 1f) pan.Normalize();
        float boost = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? speedBoost : 1f;
        transform.position += new Vector3(pan.x, pan.y, 0f) * panSpeed * boost * Time.deltaTime;

        // Pan: Middle-mouse drag
        if (Input.GetMouseButtonDown(2)) { dragging = true; lastMouse = Input.mousePosition; }
        if (Input.GetMouseButtonUp(2)) dragging = false;
        if (dragging)
        {
            Vector3 worldBefore = cam.ScreenToWorldPoint(lastMouse);
            Vector3 worldAfter = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = worldBefore - worldAfter;
            transform.position += new Vector3(delta.x, delta.y, 0f) * dragPanSpeed;
            lastMouse = Input.mousePosition;
        }

        if (clampToGrid && GridMap.I) ClampToGrid();
    }

    void ClampToGrid()
    {
        var g = GridMap.I;
        float worldW = g.WorldSize.x;
        float worldH = g.WorldSize.y;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 p = transform.position;

        float minX = halfW - boundsMargin;
        float maxX = worldW - halfW + boundsMargin;
        float minY = halfH - boundsMargin;
        float maxY = worldH - halfH + boundsMargin;

        if (minX > maxX) p.x = worldW * 0.5f; else p.x = Mathf.Clamp(p.x, minX, maxX);
        if (minY > maxY) p.y = worldH * 0.5f; else p.y = Mathf.Clamp(p.y, minY, maxY);

        transform.position = new Vector3(p.x, p.y, -10f);
    }
}

