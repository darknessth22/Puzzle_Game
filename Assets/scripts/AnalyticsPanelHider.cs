using UnityEngine;

public class AnalyticsPanelHider : MonoBehaviour
{
    void Start()
    {
        Debug.Log("AnalyticsPanelHider: Hiding analytics panel on start");
        gameObject.SetActive(false);
    }
    
    void OnEnable()
    {
        // If this is enabled after Start has run, make sure it's properly configured
        if (Time.timeSinceLevelLoad > 1f)
        {
            Debug.Log("AnalyticsPanelHider: Panel was enabled after start, checking if it should be visible");
            
            // Check if we're in a game over state
            BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
            if (gameManager == null || !gameManager.gameObject.activeInHierarchy)
            {
                Debug.Log("AnalyticsPanelHider: No active game manager found, hiding panel");
                gameObject.SetActive(false);
            }
        }
    }
}
