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
        if (useCustomCursor && cursorTexture != null)
        {
            SetCustomCursor();
        }

        StartCoroutine(MaintainCursorVisibility());
    }

    private void OnEnable()
    {
        if (useCustomCursor && cursorTexture != null)
        {
            SetCustomCursor();
        }
    }

    public void SetCustomCursor()
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ResetToDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

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

    private IEnumerator MaintainCursorVisibility()
    {
        while (true)
        {
            if (useCustomCursor && (!Cursor.visible || Cursor.lockState == CursorLockMode.Locked))
            {
                SetCustomCursor();
            }

            yield return new WaitForSeconds(cursorCheckInterval);
        }
    }
}
