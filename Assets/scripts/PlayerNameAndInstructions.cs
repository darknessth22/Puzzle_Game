using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages the flow of panels before starting the game:
/// 1. Main Menu Panel with Play/Exit buttons
/// 2. Player Name Input Panel
/// 3. Introduction Panel (no audio)
/// 4. Instructions Panel with Continue button and sound
/// 5. Load the PuzzleGame scene
/// </summary>

public class PlayerNameAndInstructions : MonoBehaviour
{
    [Header("Player Name Panel")]
    public GameObject playerNamePanel;
    public TMP_InputField playerNameInput;
    public Button nameConfirmButton;

    [Header("Introduction Panel")]
    public GameObject introductionPanel;
    public TextMeshProUGUI introductionText;
    public Button introductionContinueButton;
    public AudioClip introductionSound; // Sound to play on the introduction panel

    [Header("Instructions Panel")]
    public GameObject instructionsPanel;
    public TextMeshProUGUI instructionsText;
    public Button instructionsContinueButton;
    public AudioClip instructionsSound;

    [Header("Main Menu")]
    public GameObject mainMenuPanel; // Reference to the panel with Play and Exit buttons
    public GameObject playButton; // Reference to the Play button
    public GameObject exitButton; // Reference to the Exit button

    [Header("Background Music")]
    public AudioClip backgroundMusic; // Background music for the main menu
    private AudioSource backgroundMusicSource; // Reference to the audio source for background music

    [Header("Scene Management")]
    public string gameSceneName = "PuzzleGame";

    // Static variable to store player name across scenes
    public static string PlayerName { get; private set; }

    private void Start()
    {
        // Make sure cursor is visible and can interact with UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Check if we have an audio clip for instructions
        // No warning needed

        // Use the GameInitializer for cursor management
        GameInitializer gameInitializer = FindObjectOfType<GameInitializer>();
        if (gameInitializer != null && gameInitializer.customCursor != null)
        {
            gameInitializer.customCursor.SetCustomCursor();
        }

        // Make sure panels are initially hidden
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (introductionPanel != null)
            introductionPanel.SetActive(false);

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

        // Set up and play background music
        SetupBackgroundMusic();
    }

    // Set up and play background music on the main menu
    private void SetupBackgroundMusic()
    {
        if (backgroundMusic == null)
        {
            return;
        }

        // Create an audio source for background music on this GameObject
        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.clip = backgroundMusic;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.volume = 0.5f; // Set to 50% volume by default
        backgroundMusicSource.playOnAwake = false;

        // Start playing the background music
        backgroundMusicSource.Play();
    }

    // Public method that can be called from UI buttons
    public void StartNameInputProcess()
    {
        // Start the sequence: Name Input -> Instructions -> Game
        ShowPlayerNamePanel();
    }

    // Public method to return to the main menu (can be called from a back button)
    public void ReturnToMainMenu()
    {
        // Hide all other panels
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (introductionPanel != null)
            introductionPanel.SetActive(false);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        // Show main menu
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Show play and exit buttons
        if (playButton != null)
            playButton.SetActive(true);

        if (exitButton != null)
            exitButton.SetActive(true);

        // Restart background music if it's not playing
        if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
        {
            // Reset volume in case it was faded out
            backgroundMusicSource.volume = 0.5f;
            backgroundMusicSource.Play();
        }
    }

    private void ShowPlayerNamePanel()
    {
        // Background music continues playing during the player name panel
        // It will stop when transitioning to the introduction panel

        // Hide instructions panel
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        // Hide main menu panel
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Hide play and exit buttons
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

    // Fade out the background music
    private void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            // Start the fade out coroutine
            StartCoroutine(FadeOutBackgroundMusic(1.0f)); // Fade out over 1 second
        }
    }

    // Coroutine to fade out the background music
    private IEnumerator FadeOutBackgroundMusic(float fadeTime)
    {
        if (backgroundMusicSource == null)
            yield break;

        // Get the starting volume
        float startVolume = backgroundMusicSource.volume;

        // Gradually reduce the volume
        while (backgroundMusicSource.volume > 0)
        {
            backgroundMusicSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        // Ensure volume is set to 0
        backgroundMusicSource.volume = 0;

        // Stop the music
        backgroundMusicSource.Stop();

        // Reset the volume for future use
        backgroundMusicSource.volume = startVolume;
    }

    private void OnNameConfirmed()
    {
        // Store player name
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            PlayerName = playerNameInput.text;
        }
        else
        {
            // Default name if none provided
            PlayerName = "Player";
        }

        // Hide player name panel
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        // Show introduction panel
        ShowIntroductionPanel();
    }

    // This method is now only used when skipping the introduction panel
    // It should NOT be called from OnIntroductionContinue
    private void ShowInstructionsPanel()
    {
        // Hide player name panel
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        // Make sure main menu stays hidden
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Hide play and exit buttons
        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        // Show instructions panel
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);

            // Instructions text is set in the Inspector
            // No need to modify it here

            // Play instructions sound if available
            if (instructionsSound != null)
            {
                PlaySound(instructionsSound, instructionsPanel);
            }

            // Set up continue button
            if (instructionsContinueButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                instructionsContinueButton.onClick.RemoveAllListeners();

                // Add our click handler
                instructionsContinueButton.onClick.AddListener(OnInstructionsContinue);

                // Make sure the button is interactable
                if (!instructionsContinueButton.interactable)
                {
                    instructionsContinueButton.interactable = true;
                }
            }
        }
        else
        {
            // If no instructions panel, load game scene directly
            LoadGameScene();
        }
    }

    private void ShowIntroductionPanel()
    {
        // Stop background music when showing the introduction panel
        StopBackgroundMusic();

        // Hide player name panel
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        // Make sure main menu stays hidden
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Hide play and exit buttons
        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        // Show introduction panel
        if (introductionPanel != null)
        {
            introductionPanel.SetActive(true);

            // Introduction text is set in the Inspector
            // No need to modify it here

            // Play introduction sound if available
            if (introductionSound != null)
            {
                PlaySound(introductionSound, introductionPanel);
            }

            // Set up continue button
            if (introductionContinueButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                introductionContinueButton.onClick.RemoveAllListeners();

                // Add our click handler
                introductionContinueButton.onClick.AddListener(OnIntroductionContinue);

                // Make sure the button is interactable
                if (!introductionContinueButton.interactable)
                {
                    introductionContinueButton.interactable = true;
                }
            }
        }
        else
        {
            // If no introduction panel, go directly to instructions
            ShowInstructionsPanel();
        }
    }

    // This method is called when the introduction continue button is clicked
    public void OnIntroductionContinue()
    {
        // IMPORTANT: First show the instructions panel, then hide the introduction panel
        // to avoid both panels being hidden at the same time

        // Show instructions panel first
        if (instructionsPanel != null)
        {
            // Directly activate the instructions panel
            instructionsPanel.SetActive(true);

            // Play instructions sound if available
            if (instructionsSound != null)
            {
                // Use our helper method to play the sound on the instructions panel
                PlaySound(instructionsSound, instructionsPanel);
            }

            // Set up the continue button on the instructions panel
            if (instructionsContinueButton != null)
            {
                instructionsContinueButton.onClick.RemoveAllListeners();
                instructionsContinueButton.onClick.AddListener(OnInstructionsContinue);
            }
        }

        // Now hide the introduction panel
        if (introductionPanel != null)
        {
            introductionPanel.SetActive(false);
        }
    }

    // This method is called when the instructions continue button is clicked
    public void OnInstructionsContinue()
    {
        // Hide instructions panel
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }

        // Load the game scene
        LoadGameScene();
    }

    // Removed WaitAndLoadGameScene coroutine as we now use a button to proceed

    // Helper method to play a sound without requiring a pre-existing AudioSource
    private void PlaySound(AudioClip clip, GameObject targetObject)
    {
        if (clip == null || targetObject == null)
            return;

        // Verify we're playing sound on a valid panel
        if (targetObject != instructionsPanel && targetObject != introductionPanel)
        {
            return; // Don't continue if not a valid panel
        }

        // Try to get an existing AudioSource
        AudioSource audioSource = targetObject.GetComponent<AudioSource>();

        // If no AudioSource exists, create one
        if (audioSource == null)
        {
            audioSource = targetObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        // Make sure it's enabled
        audioSource.enabled = true;

        // Set the clip and play it
        audioSource.clip = clip;
        audioSource.PlayOneShot(clip);
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
            }
            else
            {
                // Reset game state when starting a new game from the main menu

                // Reset static variables if GameStateResetter exists
                try
                {
                    // This might throw an error if GameStateResetter doesn't exist
                    // We'll catch it and continue
                    GameStateResetter.ResetAllStaticVariables();
                }
                catch (System.Exception)
                {
                    // Silently continue if reset fails
                }

                // Set static flags to ensure proper reset when the scene loads
                BasicButtonLampGame.ForceGameStateReset = true;
                SimpleDemoManager.IsGameRestarting = true;
            }

            // Load the game scene
            SceneManager.LoadScene(gameSceneName);
        }
        // If scene doesn't exist, nothing happens
    }
}
