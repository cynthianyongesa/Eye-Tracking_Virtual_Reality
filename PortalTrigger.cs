using UnityEngine;
using UnityEngine.InputSystem; // Needed for new Unity Input System

public class PortalTrigger : MonoBehaviour
{
    private TeleportationManager teleportManager;
    private bool isPlayerNearby = false; // Tracks if player is near a portal

    void Start()
    {
        teleportManager = FindObjectOfType<TeleportationManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true; // Player is near the portal
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false; // Player left the portal area
        }
    }

    void Update()
    {
        // Keyboard input for testing
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.T))
        {
            teleportManager.InteractWithPortal(gameObject);
        }

        // VR Controller Input (Using Unity's New Input System)
        if (isPlayerNearby && Input.GetButtonDown("Fire1")) // We need to adjust input mapping based on Unity settings
        {
            teleportManager.InteractWithPortal(gameObject);
        }
    }
}