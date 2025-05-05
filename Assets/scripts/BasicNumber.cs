using UnityEngine;

public class BasicNumber : MonoBehaviour
{
    public BasicButtonLampGame gameManager;

    // Reference to the actual number object that should change color
    public GameObject numberObject;

    // The number this lamp represents (5-9)
    public int numberValue;

    private void OnMouseDown()
    {
        // Make sure we have a game manager reference
        FindGameManager();

        // Notify the game manager that this number was clicked
        if (gameManager != null)
        {
            // Pass both the lamp base (this object) and the number object
            gameManager.NumberClicked(gameObject, numberObject, numberValue);
        }
    }

    // Called when the object is enabled
    private void OnEnable()
    {
        // Always try to find the game manager when enabled
        // This is important for when the game is restarted
        FindGameManager();
    }

    // Called when the object starts
    private void Start()
    {
        // Make sure we have a game manager reference
        FindGameManager();

        // Make sure this object has the LampBase tag
        EnsureLampBaseTag();
    }

    // Ensure this object has the LampBase tag
    private void EnsureLampBaseTag()
    {
        if (gameObject.tag == "Untagged" || gameObject.tag != "LampBase")
        {
            gameObject.tag = "LampBase";
        }
    }

    // Find the game manager
    private void FindGameManager()
    {
        // Try to find the game manager if not already assigned
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<BasicButtonLampGame>();
        }
    }
}
