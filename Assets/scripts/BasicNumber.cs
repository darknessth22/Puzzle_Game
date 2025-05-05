using UnityEngine;

public class BasicNumber : MonoBehaviour
{
    public BasicButtonLampGame gameManager;
    public GameObject numberObject;
    public int numberValue;

    private void OnMouseDown()
    {
        FindGameManager();

        if (gameManager != null)
        {
            gameManager.NumberClicked(gameObject, numberObject, numberValue);
        }
    }

    private void OnEnable()
    {
        FindGameManager();
    }

    private void Start()
    {
        FindGameManager();
        EnsureLampBaseTag();
    }

    private void EnsureLampBaseTag()
    {
        if (gameObject.tag == "Untagged" || gameObject.tag != "LampBase")
        {
            gameObject.tag = "LampBase";
        }
    }

    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<BasicButtonLampGame>();
        }
    }
}
