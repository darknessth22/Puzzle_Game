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

    private void Awake()
    {
        InitializeLocalCursor();
        StartCoroutine(EnsureCursorVisible());
    }

    private void InitializeLocalCursor()
    {
        if (customCursor == null)
        {
            customCursor = FindObjectOfType<CustomCursor>();

            if (customCursor == null && gameObject != null)
            {
                customCursor = gameObject.AddComponent<CustomCursor>();
            }
        }

        if (customCursor != null && cursorTexture != null)
        {
            customCursor.cursorTexture = cursorTexture;
            customCursor.hotSpot = hotSpot;
            customCursor.useCustomCursor = true;

            customCursor.SetCustomCursor();
        }
        else if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }
    }

    private IEnumerator EnsureCursorVisible()
    {
        yield return new WaitForEndOfFrame();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (customCursor != null && customCursor.cursorTexture != null)
        {
            customCursor.SetCustomCursor();
        }
        else if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        }
    }
}
