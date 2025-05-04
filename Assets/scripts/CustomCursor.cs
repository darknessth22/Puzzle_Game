using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("The texture to use for the cursor")]
    public Texture2D cursorTexture;

    [Tooltip("The hotspot (click point) of the cursor")]
    public Vector2 hotSpot = new Vector2(16, 16);

    [Tooltip("Whether to use a custom cursor")]
    public bool useCustomCursor = true;

    [Tooltip("How often to check if cursor is still visible (in seconds)")]
    public float cursorCheckInterval = 0.5f;

    private void Start()
    {
        // Apply the custom cursor if enabled
        if (useCustomCursor && cursorTexture != null)
        {
            SetCustomCursor();
        }

        // Start a coroutine to periodically check cursor visibility
        StartCoroutine(MaintainCursorVisibility());
    }

    private void OnEnable()
    {
        // Apply cursor when component is enabled
        if (useCustomCursor && cursorTexture != null)
        {
            SetCustomCursor();
        }
    }

    // Set the custom cursor
    public void SetCustomCursor()
    {
        if (cursorTexture != null)
        {
            // Set the cursor with the specified texture and hotspot
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            // Make sure cursor is visible
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("Custom cursor applied: " + cursorTexture.name);
        }
        else
        {
            Debug.LogWarning("No cursor texture assigned!");
        }
    }

    // Reset to default cursor
    public void ResetToDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Debug.Log("Reset to default cursor");
    }

    // Toggle between custom and default cursor
    public void ToggleCustomCursor()
    {
        useCustomCursor = !useCustomCursor;

        if (useCustomCursor && cursorTexture != null)
        {
            SetCustomCursor();
        }
        else
        {
            ResetToDefaultCursor();
        }
    }

    // Coroutine to periodically check and maintain cursor visibility
    private IEnumerator MaintainCursorVisibility()
    {
        while (true)
        {
            // Check if cursor should be visible but isn't
            if (useCustomCursor && (!Cursor.visible || Cursor.lockState == CursorLockMode.Locked))
            {
                SetCustomCursor();
            }

            // Wait for the specified interval
            yield return new WaitForSeconds(cursorCheckInterval);
        }
    }
}
