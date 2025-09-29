using UnityEngine;

/// <summary>
/// Tag component to identify door objects in the building system.
/// Used by BuildPlacer for type checking and wall auto-joining logic.
/// </summary>
public class DoorTag : MonoBehaviour 
{
    [Header("Door Properties")]
    public bool isOpen = false;
    public float openCloseSpeed = 1f;
    
    [Header("Door States")]
    public Sprite closedSprite;
    public Sprite openSprite;
    
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void ToggleDoor()
    {
        isOpen = !isOpen;
        UpdateDoorSprite();
    }
    
    public void SetDoorState(bool open)
    {
        isOpen = open;
        UpdateDoorSprite();
    }
    
    private void UpdateDoorSprite()
    {
        if (!spriteRenderer) return;
        
        if (isOpen && openSprite)
            spriteRenderer.sprite = openSprite;
        else if (!isOpen && closedSprite)
            spriteRenderer.sprite = closedSprite;
    }
}
