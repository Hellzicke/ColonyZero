using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public float panSpeed = 18f;          // WASD/Arrows
    public float dragPanSpeed = 1.0f;     // Middle-mouse drag factor
    public float speedBoost = 2f;         // Shift to boost

    [Header("Smooth Movement")]
    public bool smoothMovement = true;
    public float movementSmoothing = 8f;
    public float acceleration = 10f;
    public float deceleration = 15f;

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minOrtho = 4f;
    public float maxOrtho = 40f;
    public bool zoomToMouse = true;
    public float zoomSmoothing = 8f;

    [Header("Clamp to Grid")]
    public bool clampToGrid = true;
    public float boundsMargin = 0.5f;

    Camera cam;
    bool dragging;
    Vector3 lastMouse;
    
    // Smooth movement variables
    Vector3 currentVelocity;
    Vector3 targetPosition;
    Vector2 currentPanInput;
    Vector2 targetPanInput;
    float currentZoom;
    float targetZoom;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        // Center on grid if present
        if (GridMap.I) transform.position = GridMap.I.WorldCenter + new Vector3(0, 0, -10);
        
        // Initialize smooth movement variables
        targetPosition = transform.position;
        currentZoom = cam.orthographicSize;
        targetZoom = currentZoom;
    }

    void Update()
    {
        HandleZoom();
        HandlePanInput();
        HandleDragPan();
        ApplySmoothMovement();
        
        if (clampToGrid && GridMap.I) ClampToGrid();
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 before = cam.ScreenToWorldPoint(Input.mousePosition);
            targetZoom = Mathf.Clamp(cam.orthographicSize * Mathf.Exp(-scroll * (zoomSpeed * 0.1f)), minOrtho, maxOrtho);
            
            if (zoomToMouse)
            {
                Vector3 after = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 delta = before - after;
                targetPosition += new Vector3(delta.x, delta.y, 0f);
            }
        }
        
        // Smooth zoom
        if (smoothMovement)
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, zoomSmoothing * Time.deltaTime);
            cam.orthographicSize = currentZoom;
        }
        else
        {
            cam.orthographicSize = targetZoom;
            currentZoom = targetZoom;
        }
    }

    void HandlePanInput()
    {
        // Get input
        Vector2 pan = new(
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) -
            (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) -
            (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)
        );
        
        if (pan.sqrMagnitude > 1f) pan.Normalize();
        
        // Apply acceleration/deceleration
        if (smoothMovement)
        {
            float accelRate = pan.sqrMagnitude > 0.01f ? acceleration : deceleration;
            currentPanInput = Vector2.Lerp(currentPanInput, pan, accelRate * Time.deltaTime);
        }
        else
        {
            currentPanInput = pan;
        }
        
        float boost = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? speedBoost : 1f;
        Vector3 movement = new Vector3(currentPanInput.x, currentPanInput.y, 0f) * panSpeed * boost * Time.deltaTime;
        targetPosition += movement;
    }

    void HandleDragPan()
    {
        if (Input.GetMouseButtonDown(2)) 
        { 
            dragging = true; 
            lastMouse = Input.mousePosition; 
        }
        if (Input.GetMouseButtonUp(2)) 
            dragging = false;
            
        if (dragging)
        {
            Vector3 worldBefore = cam.ScreenToWorldPoint(lastMouse);
            Vector3 worldAfter = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = worldBefore - worldAfter;
            targetPosition += new Vector3(delta.x, delta.y, 0f) * dragPanSpeed;
            lastMouse = Input.mousePosition;
        }
    }

    void ApplySmoothMovement()
    {
        if (smoothMovement)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, 1f / movementSmoothing);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    void ClampToGrid()
    {
        var g = GridMap.I;
        float worldW = g.WorldSize.x;
        float worldH = g.WorldSize.y;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 p = targetPosition;

        float minX = halfW - boundsMargin;
        float maxX = worldW - halfW + boundsMargin;
        float minY = halfH - boundsMargin;
        float maxY = worldH - halfH + boundsMargin;

        if (minX > maxX) p.x = worldW * 0.5f; else p.x = Mathf.Clamp(p.x, minX, maxX);
        if (minY > maxY) p.y = worldH * 0.5f; else p.y = Mathf.Clamp(p.y, minY, maxY);

        targetPosition = new Vector3(p.x, p.y, -10f);
    }
}

