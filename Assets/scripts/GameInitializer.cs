using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("Reference to the custom cursor component")]
    public CustomCursor customCursor;

    [Tooltip("Cursor texture to use")]
    public Texture2D cursorTexture;

    [Tooltip("Hotspot (click point) of the cursor")]
    public Vector2 hotSpot = new Vector2(16, 16);

    [Tooltip("Whether to use the PersistentGameManager if available")]
    public bool usePersistentManager = true;

    private void Awake()
    {
        // Check if we should use the persistent manager
        if (usePersistentManager)
        {
            // Look for a PersistentGameManager
            PersistentGameManager persistentManager = FindObjectOfType<PersistentGameManager>();
            if (persistentManager != null)
            {
                // Use the persistent manager's cursor settings
                Debug.Log("Using PersistentGameManager for cursor settings");
                return;
            }
        }

        // If no persistent manager or we're not using it, initialize our own cursor
        InitializeLocalCursor();

        // Start a coroutine to ensure cursor stays visible
        StartCoroutine(EnsureCursorVisible());
    }

    private void InitializeLocalCursor()
    {
        // Initialize custom cursor if not already present
        if (customCursor == null)
        {
            // Check if there's already a CustomCursor component in the scene
            customCursor = FindObjectOfType<CustomCursor>();

            // If not found, add one to this GameObject
            if (customCursor == null && gameObject != null)
            {
                customCursor = gameObject.AddComponent<CustomCursor>();
                Debug.Log("Added CustomCursor component to " + gameObject.name);
            }
        }

        // Set cursor texture and hotspot
        if (customCursor != null && cursorTexture != null)
        {
            customCursor.cursorTexture = cursorTexture;
            customCursor.hotSpot = hotSpot;
            customCursor.useCustomCursor = true;

            // Apply the cursor immediately
            customCursor.SetCustomCursor();
            Debug.Log("Custom cursor initialized locally");
        }
        else if (cursorTexture != null)
        {
            // If no CustomCursor component but we have a texture, set it directly
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            Debug.Log("Custom cursor set directly");
        }
    }

    private IEnumerator EnsureCursorVisible()
    {
        // Wait for end of frame to let other scripts initialize
        yield return new WaitForEndOfFrame();

        // Make sure cursor is visible and unlocked
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // If we have a custom cursor, apply it again to ensure it's used
        if (customCursor != null && customCursor.cursorTexture != null)
        {
            customCursor.SetCustomCursor();
        }
        else if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }

        Debug.Log("Cursor visibility enforced");
    }
}
