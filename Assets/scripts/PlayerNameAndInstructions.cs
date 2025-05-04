using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerNameAndInstructions : MonoBehaviour
{
    [Header("Player Name Panel")]
    public GameObject playerNamePanel;
    public TMP_InputField playerNameInput;
    public Button nameConfirmButton;

    [Header("Instructions Panel")]
    public GameObject instructionsPanel;
    public TextMeshProUGUI instructionsText;
    public float instructionsDuration = 15f;

    [Header("Main Menu")]
    public GameObject mainMenuPanel; // Reference to the panel with Play and Exit buttons
    public GameObject playButton; // Reference to the Play button
    public GameObject exitButton; // Reference to the Exit button

    [Header("Scene Management")]
    public string gameSceneName = "PuzzleGame";

    // Static variable to store player name across scenes
    public static string PlayerName { get; private set; }

    private void Start()
    {
        // Make sure cursor is visible and can interact with UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Use the GameInitializer for cursor management instead of PersistentGameManager
        GameInitializer gameInitializer = FindObjectOfType<GameInitializer>();
        if (gameInitializer != null && gameInitializer.customCursor != null)
        {
            // Use the game initializer's cursor
            gameInitializer.customCursor.SetCustomCursor();
            Debug.Log("Using GameInitializer's cursor in PlayerNameAndInstructions");
        }

        // Make sure panels are initially hidden
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        // Make sure main menu is visible
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Make sure play and exit buttons are visible initially
        if (playButton != null)
            playButton.SetActive(true);

        if (exitButton != null)
            exitButton.SetActive(true);
    }

    // Public method that can be called from UI buttons
    public void StartNameInputProcess()
    {
        ShowPlayerNamePanel();
    }

    private void ShowPlayerNamePanel()
    {
        // Hide instructions panel
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        // Hide main menu panel
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Hide play and exit buttons specifically
        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        // Show player name panel
        if (playerNamePanel != null)
            playerNamePanel.SetActive(true);

        // Set up confirm button
        if (nameConfirmButton != null)
        {
            nameConfirmButton.onClick.RemoveAllListeners();
            nameConfirmButton.onClick.AddListener(OnNameConfirmed);
        }

        // Set focus to the input field
        if (playerNameInput != null)
        {
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
        }
    }

    private void OnNameConfirmed()
    {
        // Store player name
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            PlayerName = playerNameInput.text;
            Debug.Log("Player name set to: " + PlayerName);
        }
        else
        {
            // Default name if none provided
            PlayerName = "Player";
            Debug.Log("No name provided, using default: " + PlayerName);
        }

        // Hide player name panel
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        // Show instructions
        ShowInstructionsPanel();
    }

    private void ShowInstructionsPanel()
    {
        // Hide player name panel
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        // Make sure main menu stays hidden
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Hide play and exit buttons specifically
        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        // Show instructions panel
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);

            // Set instructions text
            if (instructionsText != null)
            {
                // Instructions text will be set in the Inspector
                // instructionsText.text = "Welcome, " + PlayerName + "!...";

                // Add player name to the beginning of existing instructions
                if (!string.IsNullOrEmpty(PlayerName) && instructionsText.text.Length > 0)
                {
                    instructionsText.text = "مرحباً، " + PlayerName + "!\n\n" + instructionsText.text;
                }
            }

            // Start timer to automatically proceed to game
            StartCoroutine(WaitAndLoadGameScene());
        }
        else
        {
            // If no instructions panel, load game scene directly
            LoadGameScene();
        }
    }

    private IEnumerator WaitAndLoadGameScene()
    {
        // Wait for specified duration
        yield return new WaitForSeconds(instructionsDuration);

        // Load game scene
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        // Check if the scene exists in build settings
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == gameSceneName)
            {
                sceneExists = true;
                break;
            }
        }

        if (sceneExists)
        {
            // Ensure cursor is visible before loading the game scene
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Use GameInitializer for cursor management instead of PersistentGameManager
            GameInitializer gameInitializer = FindObjectOfType<GameInitializer>();
            if (gameInitializer == null)
            {
                // Create a game initializer if one doesn't exist
                GameObject initializerObject = new GameObject("GameInitializer");
                gameInitializer = initializerObject.AddComponent<GameInitializer>();

                // Add CustomCursor component
                CustomCursor customCursor = initializerObject.AddComponent<CustomCursor>();
                gameInitializer.customCursor = customCursor;

                // Find any cursor textures in the project
                Texture2D[] cursorTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
                foreach (Texture2D texture in cursorTextures)
                {
                    if (texture.name.Contains("Cursor"))
                    {
                        gameInitializer.cursorTexture = texture;
                        customCursor.cursorTexture = texture;
                        break;
                    }
                }

                // If no cursor texture found, we'll use default cursor
                if (gameInitializer.cursorTexture == null)
                {
                    Debug.LogWarning("No cursor texture found. Using default cursor.");
                }
            }
            else
            {
                // Reset game state when starting a new game from the main menu
                Debug.Log("Resetting game state before loading game scene");

                // Reset static variables if GameStateResetter exists
                try
                {
                    // This might throw an error if GameStateResetter doesn't exist
                    // We'll catch it and continue
                    GameStateResetter.ResetAllStaticVariables();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Could not reset static variables: " + e.Message);
                }

                // Set static flags to ensure proper reset when the scene loads
                BasicButtonLampGame.ForceGameStateReset = true;
                SimpleDemoManager.IsGameRestarting = true;
            }

            // Load the game scene
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Scene '" + gameSceneName + "' is not in the build settings!");
        }
    }
}
