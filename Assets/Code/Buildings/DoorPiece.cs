using UnityEngine;

/// <summary>
/// Door piece component that inherits from DoorTag.
/// Provides door-specific functionality while maintaining tag identification.
/// </summary>
public class DoorPiece : DoorTag 
{
    [Header("Door Specific")]
    public bool requiresKeycard = false;
    public int securityLevel = 0;
    public float autoCloseDelay = 3f;
    
    private float autoCloseTimer = 0f;
    
    void Update()
    {
        // Auto-close functionality
        if (isOpen && autoCloseDelay > 0)
        {
            autoCloseTimer += Time.deltaTime;
            if (autoCloseTimer >= autoCloseDelay)
            {
                CloseDoor();
            }
        }
    }
    
    public void OpenDoor()
    {
        if (!isOpen)
        {
            SetDoorState(true);
            autoCloseTimer = 0f;
        }
    }
    
    public void CloseDoor()
    {
        if (isOpen)
        {
            SetDoorState(false);
            autoCloseTimer = 0f;
        }
    }
    
    public bool CanAccessDoor(int playerSecurityLevel)
    {
        if (!requiresKeycard) return true;
        return playerSecurityLevel >= securityLevel;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Auto-open when player approaches
        if (other.CompareTag("Player"))
        {
            // Check security clearance here if needed
            OpenDoor();
        }
    }
}

