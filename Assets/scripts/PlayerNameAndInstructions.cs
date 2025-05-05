using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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

    [Header("Instructions Panel")]
    public GameObject instructionsPanel;
    public TextMeshProUGUI instructionsText;
    public Button instructionsContinueButton;
    public AudioClip instructionsSound;

    [Header("Pre-Game Panel")]
    public GameObject preGamePanel;
    public float preGamePanelDisplayTime = 5.0f;

    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public GameObject playButton;
    public GameObject exitButton;

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    private AudioSource backgroundMusicSource;

    [Header("Scene Management")]
    public string gameSceneName = "PuzzleGame";

    public static string PlayerName { get; private set; }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        GameInitializer gameInitializer = FindObjectOfType<GameInitializer>();
        if (gameInitializer != null && gameInitializer.customCursor != null)
        {
            gameInitializer.customCursor.SetCustomCursor();
        }

        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (introductionPanel != null)
            introductionPanel.SetActive(false);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        if (preGamePanel != null)
            preGamePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (playButton != null)
            playButton.SetActive(true);

        if (exitButton != null)
            exitButton.SetActive(true);

        SetupBackgroundMusic();
    }

    private void SetupBackgroundMusic()
    {
        if (backgroundMusic == null)
        {
            return;
        }

        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.clip = backgroundMusic;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.volume = 0.05f;
        backgroundMusicSource.playOnAwake = false;

        backgroundMusicSource.Play();
    }

    public void StartNameInputProcess()
    {
        ShowPlayerNamePanel();
    }

    public void ReturnToMainMenu()
    {
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (introductionPanel != null)
            introductionPanel.SetActive(false);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        if (preGamePanel != null)
            preGamePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (playButton != null)
            playButton.SetActive(true);

        if (exitButton != null)
            exitButton.SetActive(true);

        if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.volume = 0.5f;
            backgroundMusicSource.Play();
        }
    }

    private void ShowPlayerNamePanel()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        if (playerNamePanel != null)
            playerNamePanel.SetActive(true);

        if (nameConfirmButton != null)
        {
            nameConfirmButton.onClick.RemoveAllListeners();
            nameConfirmButton.onClick.AddListener(OnNameConfirmed);
        }

        if (playerNameInput != null)
        {
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
        }
    }

    private void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            StartCoroutine(FadeOutBackgroundMusic(1.0f));
        }
    }

    private IEnumerator FadeOutBackgroundMusic(float fadeTime)
    {
        if (backgroundMusicSource == null)
            yield break;

        float startVolume = backgroundMusicSource.volume;

        while (backgroundMusicSource.volume > 0)
        {
            backgroundMusicSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        backgroundMusicSource.volume = 0;
        backgroundMusicSource.Stop();
        backgroundMusicSource.volume = startVolume;
    }

    private void OnNameConfirmed()
    {
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            PlayerName = playerNameInput.text;
        }
        else
        {
            PlayerName = "Player";
        }

        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        ShowIntroductionPanel();
    }

    private void ShowInstructionsPanel()
    {
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);

            if (instructionsSound != null)
            {
                PlaySound(instructionsSound, instructionsPanel);
            }

            if (instructionsContinueButton != null)
            {
                instructionsContinueButton.onClick.RemoveAllListeners();
                instructionsContinueButton.onClick.AddListener(OnInstructionsContinue);

                if (!instructionsContinueButton.interactable)
                {
                    instructionsContinueButton.interactable = true;
                }
            }
        }
        else
        {
            LoadGameScene();
        }
    }

    private void ShowIntroductionPanel()
    {
        StopBackgroundMusic();

        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        if (introductionPanel != null)
        {
            introductionPanel.SetActive(true);

            if (introductionContinueButton != null)
            {
                introductionContinueButton.onClick.RemoveAllListeners();
                introductionContinueButton.onClick.AddListener(OnIntroductionContinue);

                if (!introductionContinueButton.interactable)
                {
                    introductionContinueButton.interactable = true;
                }
            }
        }
        else
        {
            ShowInstructionsPanel();
        }
    }

    public void OnIntroductionContinue()
    {
        if (playerNamePanel != null)
        {
            playerNamePanel.SetActive(false);
        }

        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);

            if (instructionsSound != null)
            {
                PlaySound(instructionsSound, instructionsPanel);
            }

            if (instructionsContinueButton != null)
            {
                instructionsContinueButton.onClick.RemoveAllListeners();
                instructionsContinueButton.onClick.AddListener(OnInstructionsContinue);
            }
        }

        if (introductionPanel != null)
        {
            introductionPanel.SetActive(false);
        }
    }

    public void OnInstructionsContinue()
    {
        if (playerNamePanel != null)
        {
            playerNamePanel.SetActive(false);
        }

        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }

        ShowPreGamePanel();
    }

    private void ShowPreGamePanel()
    {
        if (preGamePanel != null)
        {
            preGamePanel.SetActive(true);
            StartCoroutine(AutoLoadGameAfterDelay(preGamePanelDisplayTime));
        }
        else
        {
            LoadGameScene();
        }
    }

    private IEnumerator AutoLoadGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Create a black overlay to prevent seeing the background
        GameObject blackOverlay = new GameObject("BlackOverlay");
        Canvas overlayCanvas = blackOverlay.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999; // Ensure it's on top of everything

        RectTransform rectTransform = blackOverlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        Image image = blackOverlay.AddComponent<Image>();
        image.color = Color.black;

        // Wait one frame to ensure the overlay is rendered
        yield return null;

        // Hide all UI elements and the entire scene before loading the new scene
        HideAllUIElements();

        // Wait one more frame to ensure everything is hidden
        yield return null;

        // Directly load the scene without any intermediate steps
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = true;
    }

    private void HideAllUIElements()
    {
        // Hide all panels
        if (playerNamePanel != null)
            playerNamePanel.SetActive(false);

        if (introductionPanel != null)
            introductionPanel.SetActive(false);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        if (preGamePanel != null)
            preGamePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (playButton != null)
            playButton.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        // Find and hide any background objects
        GameObject background = GameObject.Find("background");
        if (background != null)
            background.SetActive(false);

        // Find and disable all canvases in the scene
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.gameObject.name != "BlackOverlay")
                canvas.gameObject.SetActive(false);
        }
    }

    private void PlaySound(AudioClip clip, GameObject targetObject)
    {
        if (clip == null || targetObject == null)
            return;

        if (targetObject != instructionsPanel)
        {
            return;
        }

        AudioSource audioSource = targetObject.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = targetObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        audioSource.enabled = true;
        audioSource.clip = clip;
        audioSource.PlayOneShot(clip);
    }

    private void LoadGameScene()
    {
        // Set cursor state
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Set game state variables
        BasicButtonLampGame.ForceGameStateReset = true;
        SimpleDemoManager.IsGameRestarting = true;

        try
        {
            GameStateResetter.ResetAllStaticVariables();
        }
        catch (System.Exception)
        {
            // Ignore exceptions
        }

        // Set custom cursor if available
        GameInitializer gameInitializer = FindObjectOfType<GameInitializer>();
        if (gameInitializer != null && gameInitializer.customCursor != null)
        {
            gameInitializer.customCursor.SetCustomCursor();
        }

        // Start the coroutine to handle the transition with a black overlay
        StartCoroutine(LoadGameSceneWithBlackOverlay());
    }

    private IEnumerator LoadGameSceneWithBlackOverlay()
    {
        // Create a black overlay to prevent seeing the background
        GameObject blackOverlay = new GameObject("BlackOverlay");
        Canvas overlayCanvas = blackOverlay.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999; // Ensure it's on top of everything

        RectTransform rectTransform = blackOverlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        Image image = blackOverlay.AddComponent<Image>();
        image.color = Color.black;

        // Wait one frame to ensure the overlay is rendered
        yield return null;

        // Hide all UI elements and the entire scene
        HideAllUIElements();

        // Wait one more frame to ensure everything is hidden
        yield return null;

        // Load scene immediately
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = true;
    }


}