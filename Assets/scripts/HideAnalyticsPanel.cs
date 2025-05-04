using UnityEngine;

public class HideAnalyticsPanel : MonoBehaviour
{
    void Awake()
    {
        // Hide this panel immediately on Awake
        gameObject.SetActive(false);
        Debug.Log("HideAnalyticsPanel: Hiding analytics panel on Awake");
    }

    void Start()
    {
        // Double-check that the panel is hidden on Start
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            Debug.Log("HideAnalyticsPanel: Hiding analytics panel on Start");
        }
    }

    void OnEnable()
    {
        // If this script is enabled after scene load, make sure it's properly set up
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager != null)
        {
            // Only hide if we're not in a game over state
            if (!gameManager.IsGameOver())
            {
                gameObject.SetActive(false);
                Debug.Log("HideAnalyticsPanel: Hiding analytics panel on OnEnable");
            }
        }
    }
}
