using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class BasicButtonLampGame : MonoBehaviour
{
    // Static flag to ensure game state is reset properly
    public static bool ForceGameStateReset = false;

    // Buttons and lamp bases are defined through the ButtonNumberMapping component in the Inspector

    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI triesText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Analytics UI")]
    public Button analyticsButton; // General analytics button (can be used for either panel)
    public Button winAnalyticsButton; // Analytics button on win panel
    public Button loseAnalyticsButton; // Analytics button on lose panel
    public GameObject analyticsPanel;
    public TextMeshProUGUI analyticsText;
    public TextMeshProUGUI escInstructionText; // Text that says "Press ESC to go back"

    [Header("Try Analytics UI")]
    public GameObject tryAnalyticsPanel; // Panel that appears after each try
    public TextMeshProUGUI tryAnalyticsText; // Text showing try results
    public float tryAnalyticsDuration = 3f; // How long to show the try analytics panel

    [Header("Menu Buttons")]
    public Button retryButton;
    public Button mainMenuButton;
    public Button exitButton;

    [Header("Win Panel Buttons")]
    public Button winRetryButton;
    public Button winMainMenuButton;
    public Button winExitButton;

    [Header("Game Settings")]
    public int maxTries = 10;
    [Tooltip("If set to 0, will automatically use the number of available buttons")]
    public int totalButtonsToMatch = 0;

    [Header("Win/Lose Sounds")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public float soundVolume = 1.0f;
    private AudioSource audioSource;

    // State tracking
    private GameObject selectedButton = null;
    private Material originalButtonMaterial = null;
    public int currentTries;
    public int currentProgress;
    private bool gameOver = false;
    private bool currentTryFailed = false;

    // Dictionary to store original materials for number objects
    private Dictionary<GameObject, Material> originalNumberMaterials = new Dictionary<GameObject, Material>();

    // Lists to track locked buttons and lamps
    private List<GameObject> lockedButtons = new List<GameObject>();
    private List<GameObject> lockedLampBases = new List<GameObject>();
    private List<GameObject> correctlyMatchedButtons = new List<GameObject>();

    // Button-to-number mapping
    [Header("Button-Number Mapping")]
    public ButtonNumberMapping buttonNumberMapping;

    // Analytics tracking
    private List<int> wrongButtonsPerTry = new List<int>();
    private int currentTryWrongButtons = 0;

    // Analytics data for persistence between scene reloads
    public List<string> analyticsData = new List<string>();

    // Materials for feedback
    private Material greenMaterial;
    private Material redMaterial;

    private void Update()
    {
        // Check for ESC key press to close analytics panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Only handle ESC key if analytics panel is active
            if (analyticsPanel != null && analyticsPanel.activeSelf)
            {
                HideAnalyticsPanel();
            }
        }
    }

    private void Start()
    {
        // Always find references when starting, especially important after game restart
        FindAllReferences();

        // Initialize button-number mapping if not already set
        InitializeButtonNumberMapping();

        // Continue with the rest of the start method
        Start_Continued();
    }

    // Initialize the button-number mapping
    private void InitializeButtonNumberMapping()
    {
        // If no mapping component exists, add one
        if (buttonNumberMapping == null)
        {
            buttonNumberMapping = GetComponent<ButtonNumberMapping>();
            if (buttonNumberMapping == null)
            {
                buttonNumberMapping = gameObject.AddComponent<ButtonNumberMapping>();
            }
        }

        // Make sure the mapping dictionaries are initialized
        buttonNumberMapping.RebuildMappingDictionaries();
    }

    // Find all references in one go
    private void FindAllReferences()
    {
        // Set up game manager references for all buttons and lamp bases
        SetupGameManagerReferences();
    }

    // Set up game manager references for all buttons and lamp bases
    private void SetupGameManagerReferences()
    {
        // Find all buttons in the scene using the tag
        GameObject[] allButtons = GameObject.FindGameObjectsWithTag("Button");

        // Set up references for all buttons
        foreach (GameObject btn in allButtons)
        {
            BasicButton basicButton = btn.GetComponent<BasicButton>();
            if (basicButton != null)
            {
                basicButton.gameManager = this;
            }
        }

        // Find all lamp bases in the scene using the tag
        GameObject[] allLampBases = GameObject.FindGameObjectsWithTag("LampBase");

        foreach (GameObject lamp in allLampBases)
        {
            if (lamp != null)
            {
                BasicNumber basicNumber = lamp.GetComponent<BasicNumber>();
                if (basicNumber != null)
                {
                    basicNumber.gameManager = this;
                }
            }
        }
    }

    private void Start_Continued()
    {
        // Make sure analytics panel is hidden at start
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
        }

        // Initialize audio source for win/lose sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize game state
        currentTries = maxTries;
        currentProgress = 0;
        gameOver = false; // Explicitly set to false on start
        currentTryFailed = false;
        currentTryWrongButtons = 0;
        wrongButtonsPerTry.Clear();

        // If totalButtonsToMatch is 0, automatically use the number of available buttons
        if (totalButtonsToMatch == 0)
        {
            // Count buttons from ButtonNumberMapping
            int buttonCount = 0;

            if (buttonNumberMapping != null)
            {
                // Count unique buttons in the mapping
                HashSet<GameObject> uniqueButtons = new HashSet<GameObject>();
                foreach (var pair in buttonNumberMapping.buttonNumberPairs)
                {
                    if (pair.button != null)
                    {
                        uniqueButtons.Add(pair.button);
                    }
                }
                buttonCount = uniqueButtons.Count;
            }

            // If no buttons found in mapping, try to find buttons with the Button tag
            if (buttonCount == 0)
            {
                GameObject[] taggedButtons = GameObject.FindGameObjectsWithTag("Button");
                buttonCount = taggedButtons.Length;
            }

            totalButtonsToMatch = buttonCount;

        }

        // Reset the force reset flag
        ForceGameStateReset = false;

        // Add a delayed check to ensure game state is properly reset after demo
        StartCoroutine(DelayedGameStateCheck());

        // Check if panels are assigned
        if (winPanel == null) Debug.LogWarning("winPanel is not assigned in the Inspector!");
        if (losePanel == null) Debug.LogWarning("losePanel is not assigned in the Inspector!");
        if (analyticsPanel == null) Debug.LogWarning("analyticsPanel is not assigned in the Inspector!");
        if (tryAnalyticsPanel == null) Debug.LogWarning("tryAnalyticsPanel is not assigned in the Inspector!");
        if (tryAnalyticsText == null) Debug.LogWarning("tryAnalyticsText is not assigned in the Inspector!");

        // Hide panels
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (tryAnalyticsPanel != null) tryAnalyticsPanel.SetActive(false);

        // Create materials for feedback
        greenMaterial = new Material(Shader.Find("Standard"));
        greenMaterial.color = Color.green;
        greenMaterial.EnableKeyword("_EMISSION");
        greenMaterial.SetColor("_EmissionColor", Color.green * 2f);

        redMaterial = new Material(Shader.Find("Standard"));
        redMaterial.color = Color.red;
        redMaterial.EnableKeyword("_EMISSION");
        redMaterial.SetColor("_EmissionColor", Color.red * 2f);

        // Get player name from PlayerNameAndInstructions
        string playerName = "Player";
        if (!string.IsNullOrEmpty(PlayerNameAndInstructions.PlayerName))
        {
            playerName = PlayerNameAndInstructions.PlayerName;
        }

        // Clear instruction text - user doesn't want any text to appear
        if (instructionText != null)
        {
            instructionText.text = "";
        }

        // Set up button listeners
        SetupButtonListeners();

        // Update UI
        UpdateProgressText();
        UpdateTriesText();
    }

    // Set up button listeners
    public void SetupButtonListeners()
    {
        // Analytics buttons
        // General analytics button (if used)
        if (analyticsButton != null)
        {
            // Remove any existing listeners first to avoid duplicates
            analyticsButton.onClick.RemoveAllListeners();
            analyticsButton.onClick.AddListener(ShowAnalyticsPanel);
        }

        // Win panel analytics button
        if (winAnalyticsButton != null)
        {
            // Remove any existing listeners first to avoid duplicates
            winAnalyticsButton.onClick.RemoveAllListeners();
            winAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
        }
        else
        {
            Debug.LogWarning("Win analytics button is null!");
        }

        // Lose panel analytics button
        if (loseAnalyticsButton != null)
        {
            // Remove any existing listeners first to avoid duplicates
            loseAnalyticsButton.onClick.RemoveAllListeners();
            loseAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
        }
        else
        {
            Debug.LogWarning("Lose analytics button is null!");
        }

        // Lose panel buttons
        // Retry button
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryGame);
        }

        // Main menu button
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        // Exit button
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }

        // Win panel buttons
        // Win retry button
        if (winRetryButton != null)
        {
            winRetryButton.onClick.RemoveAllListeners();
            winRetryButton.onClick.AddListener(RetryGame);
        }
        else
        {
            Debug.LogWarning("Win retry button is null!");
        }

        // Win main menu button
        if (winMainMenuButton != null)
        {
            winMainMenuButton.onClick.RemoveAllListeners();
            winMainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        // Win exit button
        if (winExitButton != null)
        {
            winExitButton.onClick.RemoveAllListeners();
            winExitButton.onClick.AddListener(ExitGame);
        }

        // We're now using ESC key instead of CloseAnalyticsButton

        // Also check if analytics panel has any close buttons
        if (analyticsPanel != null)
        {
            // Try to find a close button in the analytics panel
            Button[] analyticsButtons = analyticsPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in analyticsButtons)
            {
                // Look for buttons with "close" or "back" in their name
                if (button.name.ToLower().Contains("close") || button.name.ToLower().Contains("back"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(HideAnalyticsPanel);
                }
            }
        }
    }

    // Called when a try is complete (all buttons are locked)
    private void FinishCurrentTry()
    {
        // This method is only called for failed tries

        // Calculate correct and wrong buttons
        int correctButtons = totalButtonsToMatch - currentTryWrongButtons;

        // Record analytics for this try
        wrongButtonsPerTry.Add(currentTryWrongButtons);

        // Decrease tries
        currentTries--;
        UpdateTriesText();

        // Show try analytics panel
        ShowTryAnalyticsPanel(correctButtons, currentTryWrongButtons);

        // Check if out of tries - but don't end the game yet
        // We'll let the analytics panel show first, then end the game
        if (currentTries <= 0)
        {
            // Start a coroutine to show game over after analytics panel
            StartCoroutine(ShowGameOverAfterAnalytics());
            return;
        }
    }

    // Show game over after analytics panel
    private IEnumerator ShowGameOverAfterAnalytics()
    {
        // Wait for the analytics panel duration
        yield return new WaitForSeconds(tryAnalyticsDuration);

        // Now show the game over panel
        GameLost();
    }

    // Show the try analytics panel with results
    private void ShowTryAnalyticsPanel(int correctButtons, int wrongButtons)
    {
        if (tryAnalyticsPanel != null)
        {
            // Update try analytics text with enhanced formatting
            if (tryAnalyticsText != null)
            {
                // Create a more visually appealing text display
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"<align=center><size=40><b>TRY {maxTries - currentTries} RESULTS</b></size></align>");
                sb.AppendLine();
                sb.AppendLine($"<color=green><b>CORRECT MATCHES:</b> {correctButtons}/{totalButtonsToMatch}</color>");
                sb.AppendLine($"<color=red><b>WRONG MATCHES:</b> {wrongButtons}/{totalButtonsToMatch}</color>");

                // Add a message based on performance
                if (correctButtons > wrongButtons)
                {
                    sb.AppendLine();
                    sb.AppendLine("<color=yellow>Good effort! Keep improving!</color>");
                }
                else if (correctButtons == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("<color=yellow>Focus! You can do better!</color>");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("<color=yellow>Almost there! Try again!</color>");
                }

                tryAnalyticsText.text = sb.ToString();
            }
            else
            {
                Debug.LogError("tryAnalyticsText is null!");
            }

            // Show the panel
            tryAnalyticsPanel.SetActive(true);

            // Start coroutine to hide panel and start next try after delay
            StartCoroutine(HideTryAnalyticsPanelAfterDelay());
        }
        else
        {
            Debug.LogError("tryAnalyticsPanel is null! Check the Inspector assignment");
            // If panel doesn't exist, just start the next try
            StartCoroutine(StartNextTryAfterDelay(tryAnalyticsDuration));
        }
    }

    // Hide try analytics panel after delay and start next try
    private IEnumerator HideTryAnalyticsPanelAfterDelay()
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(tryAnalyticsDuration);

        // Hide the panel
        if (tryAnalyticsPanel != null)
        {
            tryAnalyticsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("tryAnalyticsPanel is null when trying to hide it!");
        }

        // Start the next try
        StartNextTry();
    }

    // Start the next try after a delay
    private IEnumerator StartNextTryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextTry();
    }

    // Start the next try
    private void StartNextTry()
    {
        // Hide all panels
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (tryAnalyticsPanel != null) tryAnalyticsPanel.SetActive(false);

        // Reset game state flags
        gameOver = false;
        ForceGameStateReset = false;
        currentTryFailed = false;
        currentTryWrongButtons = 0;
        currentProgress = 0;

        // Reset button selection
        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
            selectedButton = null;
        }

        // Reset all button materials
        // First try to get buttons from ButtonNumberMapping
        List<GameObject> buttonList = new List<GameObject>();

        if (buttonNumberMapping != null)
        {
            // Get unique buttons from the mapping
            HashSet<GameObject> uniqueButtons = new HashSet<GameObject>();
            foreach (var pair in buttonNumberMapping.buttonNumberPairs)
            {
                if (pair.button != null)
                {
                    uniqueButtons.Add(pair.button);
                }
            }
            buttonList.AddRange(uniqueButtons);
        }

        // If no buttons found in mapping, try to find buttons with the Button tag
        if (buttonList.Count == 0)
        {
            GameObject[] taggedButtons = GameObject.FindGameObjectsWithTag("Button");
            buttonList.AddRange(taggedButtons);
        }

        foreach (GameObject button in buttonList)
        {
            Renderer renderer = button.GetComponent<Renderer>();
            if (renderer != null && originalButtonMaterial != null)
            {
                renderer.material = originalButtonMaterial;
            }
        }

        // Reset all number materials directly
        foreach (var entry in originalNumberMaterials)
        {
            GameObject numberObject = entry.Key;
            Material originalMaterial = entry.Value;

            if (numberObject != null)
            {
                Renderer renderer = numberObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = originalMaterial;
                }
            }
        }

        // Reset any lamp base children that might not be in the dictionary
        void ResetLampBaseChild(GameObject lampBase)
        {
            if (lampBase != null && lampBase.transform.childCount > 0)
            {
                GameObject numberObject = lampBase.transform.GetChild(0).gameObject;
                if (numberObject != null && !originalNumberMaterials.ContainsKey(numberObject))
                {
                    Renderer renderer = numberObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Create a default material if needed
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.material = defaultMaterial;
                        originalNumberMaterials[numberObject] = defaultMaterial;
                    }
                }
            }
        }

        // Reset all lamp bases using the tag
        GameObject[] allLampBases = GameObject.FindGameObjectsWithTag("LampBase");
        foreach (GameObject lampBase in allLampBases)
        {
            if (lampBase != null)
            {
                ResetLampBaseChild(lampBase);
            }
        }

        // Clear all locked lists
        lockedButtons.Clear();
        lockedLampBases.Clear();
        correctlyMatchedButtons.Clear();

        // Update UI
        UpdateProgressText();
        UpdateTriesText();
    }

    // Update the progress text
    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"Progress: {currentProgress}/{totalButtonsToMatch}";
        }
    }

    // Update the tries text
    private void UpdateTriesText()
    {
        if (triesText != null)
        {
            triesText.text = $"Tries Remaining: {currentTries}/{maxTries}";
        }
    }

    // Demo functionality has been removed and moved to SimpleDemoManager

    // Get the correct number for a button
    private int GetCorrectNumberForButton(GameObject button)
    {
        if (button == null) return -1;

        // First check our button-number mapping from the Inspector
        if (buttonNumberMapping != null)
        {
            int numberValue = buttonNumberMapping.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        // Check if there's a SimpleDemoManager with mappings
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.buttonNumberMapping != null)
        {
            int numberValue = demoManager.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        // No mapping found in the Inspector or from the demo manager
        Debug.LogWarning($"Could not determine lamp number for button: {button.name}. Please define mapping in the Inspector.");

        return -1; // No mapping found
    }

    // Get lamp base by number
    private GameObject GetLampBaseByNumber(int number)
    {
        if (number < 1 || number > 10)
        {
            Debug.LogWarning($"Invalid lamp number: {number}. Must be between 1 and 10.");
            return null;
        }

        // First check if we have a mapping in the ButtonNumberMapping component
        if (buttonNumberMapping != null)
        {
            // Look for a mapping with this number value
            foreach (var pair in buttonNumberMapping.buttonNumberPairs)
            {
                if (pair.numberValue == number && pair.numberObject != null)
                {
                    // Get the parent of the number object, which should be the lamp base
                    Transform parent = pair.numberObject.transform.parent;
                    if (parent != null)
                    {
                        return parent.gameObject;
                    }
                    else
                    {
                        // If the number object has no parent, it might be the lamp base itself
                        return pair.numberObject;
                    }
                }
            }
        }

        // If we didn't find a mapping, try to find a lamp base with the appropriate tag and name
        GameObject[] lampBases = GameObject.FindGameObjectsWithTag("LampBase");
        foreach (GameObject lampBase in lampBases)
        {
            // Check if the name contains the number
            if (lampBase.name.Contains(number.ToString()))
            {
                return lampBase;
            }
        }

        Debug.LogWarning($"No lamp base found for number {number}. Please define it in the Inspector.");
        return null;
    }

    // Get number object from lamp base
    private GameObject GetNumberObjectFromLampBase(GameObject lampBase)
    {
        if (lampBase == null) return null;

        // First try to get the number object from the BasicNumber component
        BasicNumber basicNumber = lampBase.GetComponent<BasicNumber>();
        if (basicNumber != null && basicNumber.numberObject != null)
        {
            return basicNumber.numberObject;
        }

        // If that fails, try to find a child with "Number" in its name
        Transform[] children = lampBase.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.gameObject != lampBase && (child.name.Contains("Number") || child.name.Contains("number")))
            {
                return child.gameObject;
            }
        }

        // If no specific number object is found, return null
        Debug.LogWarning($"Could not find number object in lamp base: {lampBase.name}");
        return null;
    }

    // Call this from button click events
    public void ButtonClicked(GameObject button)
    {
        // Check if the game is over or if we're in the process of resetting
        if (gameOver || ForceGameStateReset)
        {
            return;
        }

        // Double-check with SimpleDemoManager if the demo is still running
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.IsDemoRunning())
        {
            return;
        }

        // Check if the button is locked
        if (lockedButtons.Contains(button))
        {
            return;
        }

        // Reset previous selection if any
        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
        }

        // Store the selected button
        selectedButton = button;

        // Highlight the button
        HighlightButton(button);
    }

    // Call this from number click events
    public void NumberClicked(GameObject lampBase, GameObject numberObject, int numberValue)
    {
        // Check if the game is over or if we're in the process of resetting
        if (gameOver || ForceGameStateReset)
        {
            return;
        }

        // Double-check with SimpleDemoManager if the demo is still running
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.IsDemoRunning())
        {
            return;
        }

        // Check if the lamp base is locked
        if (lockedLampBases.Contains(lampBase))
        {
            return;
        }

        // If no button is selected, ignore the number click
        if (selectedButton == null)
        {
            return;
        }

        // Check if this is the correct match
        bool isCorrect = IsCorrectMatch(selectedButton, numberValue);

        if (isCorrect)
        {
            // Lock the button and lamp base with success visual
            LockButtonSuccess(selectedButton);
            LockLampSuccess(lampBase, numberObject);

            // Add to locked lists
            lockedButtons.Add(selectedButton);
            lockedLampBases.Add(lampBase);

            // Add to correctly matched buttons list
            correctlyMatchedButtons.Add(selectedButton);

            // Update progress
            currentProgress = correctlyMatchedButtons.Count;
            UpdateProgressText();

            // Only check for win if all buttons have been used
            if (lockedButtons.Count >= totalButtonsToMatch)
            {
                // Check if all matches were correct (no failures)
                if (!currentTryFailed && currentProgress >= totalButtonsToMatch)
                {
                    // Player wins!
                    GameWon();
                }
                else
                {
                    // This try is complete and failed
                    FinishCurrentTry();
                }
            }
        }
        else
        {
            // Lock the button and lamp base with failure visual
            LockButtonFailure(selectedButton);
            LockLampFailure(lampBase, numberObject);

            // Add to locked lists
            lockedButtons.Add(selectedButton);
            lockedLampBases.Add(lampBase);

            // Mark this try as failed
            currentTryFailed = true;

            // Increment wrong buttons counter
            currentTryWrongButtons++;

            // Check if all buttons are locked (try is complete)
            if (lockedButtons.Count >= totalButtonsToMatch)
            {
                // This try is complete and failed
                FinishCurrentTry();
            }
        }

        // Clear the button selection
        selectedButton = null;
    }

    // These methods have been removed as we no longer need to show the correct answer

    // Check if the button and number value match
    private bool IsCorrectMatch(GameObject button, int numberValue)
    {
        if (button == null) return false;

        // First check our button-number mapping from the Inspector
        if (buttonNumberMapping != null)
        {
            bool isMatch = buttonNumberMapping.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

        // Check if there's a SimpleDemoManager with mappings
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.buttonNumberMapping != null)
        {
            bool isMatch = demoManager.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

        return false;
    }

    // Highlight a button when selected
    private void HighlightButton(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store the original material
            originalButtonMaterial = renderer.material;

            // Create a highlighted material
            Material highlightMaterial = new Material(originalButtonMaterial);
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", Color.cyan * 2f);

            // Apply the highlighted material
            renderer.material = highlightMaterial;
        }

        // Check if the button has an animator and play a highlight animation
        Animator buttonAnimator = button.GetComponent<Animator>();
        if (buttonAnimator != null)
        {
            try
            {
                // Try to play the "switch" state
                buttonAnimator.Play("switch", 0, 0f);
            }
            catch (System.Exception)
            {
                // If that fails, try to play any state
                if (buttonAnimator.runtimeAnimatorController != null)
                {
                    AnimationClip[] clips = buttonAnimator.runtimeAnimatorController.animationClips;
                    if (clips.Length > 0)
                    {
                        buttonAnimator.Play(clips[0].name, 0, 0f);
                    }
                }
            }
        }
    }

    // Reset a button's appearance
    private void ResetButtonAppearance(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null && originalButtonMaterial != null)
        {
            renderer.material = originalButtonMaterial;
        }
    }

    // Lock a button with success visual
    private void LockButtonSuccess(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store the original material if not already stored
            if (originalButtonMaterial == null)
            {
                originalButtonMaterial = renderer.material;
            }

            // Apply green material
            renderer.material = greenMaterial;
        }

        // Play success animation if available
        Animator buttonAnimator = button.GetComponent<Animator>();
        if (buttonAnimator != null)
        {
            try
            {
                // Try to play the "switch" state
                buttonAnimator.Play("switch", 0, 0f);
            }
            catch (System.Exception)
            {
                // If that fails, try to play any state
                if (buttonAnimator.runtimeAnimatorController != null)
                {
                    AnimationClip[] clips = buttonAnimator.runtimeAnimatorController.animationClips;
                    if (clips.Length > 0)
                    {
                        buttonAnimator.Play(clips[0].name, 0, 0f);
                    }
                }
            }
        }

        // Play success sound if available
        AudioSource audioSource = button.GetComponent<AudioSource>();
        BasicButton basicButton = button.GetComponent<BasicButton>();
        if (audioSource != null && basicButton != null && basicButton.buttonClickSound != null)
        {
            audioSource.PlayOneShot(basicButton.buttonClickSound, basicButton.volume);
        }
    }

    // Lock a button with failure visual
    private void LockButtonFailure(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store the original material if not already stored
            if (originalButtonMaterial == null)
            {
                originalButtonMaterial = renderer.material;
            }

            // Apply red material
            renderer.material = redMaterial;
        }

        // Play failure animation if available
        Animator buttonAnimator = button.GetComponent<Animator>();
        if (buttonAnimator != null)
        {
            try
            {
                // Try to play the "switch" state
                buttonAnimator.Play("switch", 0, 0f);
            }
            catch (System.Exception)
            {
                // If that fails, try to play any state
                if (buttonAnimator.runtimeAnimatorController != null)
                {
                    AnimationClip[] clips = buttonAnimator.runtimeAnimatorController.animationClips;
                    if (clips.Length > 0)
                    {
                        buttonAnimator.Play(clips[0].name, 0, 0f);
                    }
                }
            }
        }

        // Play failure sound if available
        AudioSource audioSource = button.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            // Try to find a failure sound clip
            BasicButton basicButton = button.GetComponent<BasicButton>();
            if (basicButton != null && basicButton.buttonClickSound != null)
            {
                audioSource.PlayOneShot(basicButton.buttonClickSound, basicButton.volume);
            }
        }
    }

    // Lock a lamp with success visual
    private void LockLampSuccess(GameObject lampBase, GameObject numberObject)
    {
        if (numberObject == null)
        {
            return;
        }

        Renderer renderer = numberObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store the original material if not already stored
            if (!originalNumberMaterials.ContainsKey(numberObject))
            {
                originalNumberMaterials[numberObject] = renderer.material;
            }

            // Apply green material
            renderer.material = greenMaterial;
        }
    }

    // Lock a lamp with failure visual
    private void LockLampFailure(GameObject lampBase, GameObject numberObject)
    {
        if (numberObject == null)
        {
            return;
        }

        Renderer renderer = numberObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store the original material if not already stored
            if (!originalNumberMaterials.ContainsKey(numberObject))
            {
                originalNumberMaterials[numberObject] = renderer.material;
            }

            // Apply red material
            renderer.material = redMaterial;
        }
    }







    // Called when the player wins the game
    private void GameWon()
    {
        gameOver = true;

        // Record analytics for the winning try (which had 0 wrong buttons)
        wrongButtonsPerTry.Add(0);

        // Show win panel if available
        if (winPanel != null)
        {
            winPanel.SetActive(true);

            // Make sure the win analytics button is properly set up
            if (winAnalyticsButton != null)
            {
                winAnalyticsButton.onClick.RemoveAllListeners();
                winAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
            }

            // Play win sound effect
            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound, soundVolume);
            }
        }

        // Disable player movement and interaction
        DisablePlayerMovement();

        // Pre-generate analytics data so it's ready when the button is clicked
        UpdateAnalyticsText();
    }

    // Called when the player loses the game
    private void GameLost()
    {
        gameOver = true;

        // Show lose panel if available
        if (losePanel != null)
        {
            losePanel.SetActive(true);

            // Make sure the lose analytics button is properly set up
            if (loseAnalyticsButton != null)
            {
                loseAnalyticsButton.onClick.RemoveAllListeners();
                loseAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
            }

            // Play lose sound effect
            if (audioSource != null && loseSound != null)
            {
                audioSource.PlayOneShot(loseSound, soundVolume);
            }
        }

        // Disable player movement and interaction
        DisablePlayerMovement();

        // Pre-generate analytics data so it's ready when the button is clicked
        UpdateAnalyticsText();
    }

    // Show analytics panel
    public void ShowAnalyticsPanel()
    {
        // Find analytics panel if it's null
        if (analyticsPanel == null)
        {
            // Try to find by tag first (using the existing tag system)
            GameObject[] panels = GameObject.FindGameObjectsWithTag("UIPanel");
            foreach (GameObject panel in panels)
            {
                if (panel.name.ToLower().Contains("analytics"))
                {
                    analyticsPanel = panel;
                    break;
                }
            }

            // If still not found, try direct name
            if (analyticsPanel == null)
            {
                analyticsPanel = GameObject.Find("GameAnalyticsPanel");
            }

            if (analyticsPanel == null)
            {
                Debug.LogError("Could not find analytics panel!");
                return;
            }
        }

        // Hide win/lose panels
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        // Show analytics panel
        analyticsPanel.SetActive(true);

        // Create or update ESC instruction text
        if (escInstructionText == null)
        {
            // Try to find existing text first
            TMPro.TextMeshProUGUI[] texts = analyticsPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (TMPro.TextMeshProUGUI text in texts)
            {
                if (text != analyticsText && (text.name.ToLower().Contains("esc") ||
                    text.name.ToLower().Contains("instruction") ||
                    text.name.ToLower().Contains("back")))
                {
                    escInstructionText = text;
                    break;
                }
            }

            // If still not found, create a new text object
            if (escInstructionText == null)
            {
                // Create a new GameObject for the ESC instruction text
                GameObject escTextObj = new GameObject("EscInstructionText");
                escTextObj.transform.SetParent(analyticsPanel.transform, false);

                // Add TextMeshProUGUI component
                escInstructionText = escTextObj.AddComponent<TMPro.TextMeshProUGUI>();

                // Set up RectTransform
                RectTransform rectTransform = escTextObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0);
                rectTransform.anchorMax = new Vector2(0.5f, 0);
                rectTransform.pivot = new Vector2(0.5f, 0);
                rectTransform.anchoredPosition = new Vector2(0, 20);
                rectTransform.sizeDelta = new Vector2(400, 50);
            }
        }

        // Set the ESC instruction text
        if (escInstructionText != null)
        {
            escInstructionText.text = "<color=yellow><size=24>Press ESC to go back</size></color>";
            escInstructionText.alignment = TMPro.TextAlignmentOptions.Center;
            escInstructionText.fontSize = 24;
            escInstructionText.color = Color.yellow;
            escInstructionText.gameObject.SetActive(true);
        }

        // Find the analytics text component
        if (analyticsText == null)
        {
            // Try to find by name first
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Try to find any TextMeshProUGUI in the panel
                TextMeshProUGUI[] allTexts = analyticsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (allTexts.Length > 0)
                {
                    analyticsText = allTexts[0];
                }
            }
        }

        // Add or get the AnalyticsTextFixer component
        AnalyticsTextFixer fixer = analyticsPanel.GetComponent<AnalyticsTextFixer>();
        if (fixer == null)
        {
            fixer = analyticsPanel.AddComponent<AnalyticsTextFixer>();
        }

        // Assign the analytics text to the fixer
        if (analyticsText != null)
        {
            fixer.analyticsText = analyticsText;
        }

        // The fixer will handle the rest in its Start method

        // Generate analytics content
        if (analyticsText != null)
        {
            // Calculate total statistics
            int totalWrongButtons = 0;
            int totalCorrectButtons = 0;
            int totalButtonsAttempted = 0;

            // Sum up all the wrong buttons from completed tries
            foreach (int wrong in wrongButtonsPerTry)
            {
                totalWrongButtons += wrong;
                totalCorrectButtons += (totalButtonsToMatch - wrong);
                totalButtonsAttempted += totalButtonsToMatch;
            }

            // Add current try stats if game is not over and we're in the middle of a try
            if (!gameOver && currentTryWrongButtons > 0 && lockedButtons.Count < totalButtonsToMatch)
            {
                totalWrongButtons += currentTryWrongButtons;
                totalCorrectButtons += correctlyMatchedButtons.Count;
                totalButtonsAttempted += lockedButtons.Count;
            }

            // Calculate success rate as a percentage
            float successRate = 0;
            if (totalButtonsAttempted > 0)
            {
                successRate = (float)totalCorrectButtons / totalButtonsAttempted * 100f;
            }

            // Create analytics text content
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Title centered at the top
            sb.AppendLine("<size=36>Game Analytics</size>");
            sb.AppendLine();

            // Per-try results
            sb.AppendLine("<color=#FF9900><size=20><b>PER-TRY RESULTS:</b></size></color>");
            sb.AppendLine();

            // Show results for each try
            for (int i = 0; i < wrongButtonsPerTry.Count; i++)
            {
                int correctButtons = totalButtonsToMatch - wrongButtonsPerTry[i];
                sb.AppendLine($"Try {i + 1}: <color=green>{correctButtons} correct</color>, <color=red>{wrongButtonsPerTry[i]} wrong</color>");
            }

            sb.AppendLine();
            sb.AppendLine("* * * * * * * * * * * * * * *");
            sb.AppendLine();

            // Game summary
            sb.AppendLine("<color=#FF9900><size=20><b>GAME SUMMARY:</b></size></color>");
            sb.AppendLine();
            sb.AppendLine($"Total Buttons Attempted: {totalButtonsAttempted}");
            sb.AppendLine($"Total Correct Matches: <color=green>{totalCorrectButtons}</color>");
            sb.AppendLine($"Total Wrong Matches: <color=red>{totalWrongButtons}</color>");
            sb.AppendLine($"Success Rate: <color=yellow>{successRate:F1}%</color>");
            sb.AppendLine($"Tries Used: {maxTries - currentTries}/{maxTries}");

            // Set the text content
            analyticsText.text = sb.ToString();
        }
        else
        {
            Debug.LogError("Could not find or create analytics text component!");
        }
    }

    // Hide analytics panel
    public void HideAnalyticsPanel()
    {
        if (analyticsPanel != null)
        {
            // Hide analytics panel
            analyticsPanel.SetActive(false);

            // Show the appropriate panel based on game result
            if (gameOver)
            {
                if (currentTries <= 0)
                {
                    // Game was lost, show lose panel
                    if (losePanel != null)
                    {
                        losePanel.SetActive(true);
                    }
                }
                else
                {
                    // Game was won, show win panel
                    if (winPanel != null)
                    {
                        winPanel.SetActive(true);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Analytics panel is null in HideAnalyticsPanel!");
        }
    }

    // Toggle analytics panel
    public void ToggleAnalyticsPanel()
    {
        if (analyticsPanel != null)
        {
            bool isActive = analyticsPanel.activeSelf;

            if (!isActive) // Opening analytics panel
            {
                ShowAnalyticsPanel();
            }
            else // Closing analytics panel
            {
                HideAnalyticsPanel();
            }
        }
        else
        {
            Debug.LogError("Analytics panel is null!");
        }
    }

    // Update analytics text with a better approach for two columns
    private void UpdateAnalyticsText()
    {
        // Double-check if analyticsText is null
        if (analyticsText == null && analyticsPanel != null)
        {
            // Try to find by name first
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Try to find any TextMeshProUGUI in the panel
                analyticsText = analyticsPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (analyticsText == null)
                {
                    Debug.LogError("Could not find analytics text in panel!");
                    return;
                }
            }

            // Get or add the AnalyticsTextFixer component
            AnalyticsTextFixer fixer = analyticsPanel.GetComponent<AnalyticsTextFixer>();
            if (fixer == null)
            {
                fixer = analyticsPanel.AddComponent<AnalyticsTextFixer>();
            }

            // Assign the analytics text to the fixer
            fixer.analyticsText = analyticsText;

            // Force the fixer to run its setup
            fixer.Start();
        }

        if (analyticsText != null)
        {

            // Calculate total statistics first
            int totalWrongButtons = 0;
            int totalCorrectButtons = 0;
            int totalButtonsAttempted = 0;

            // Sum up all the wrong buttons from completed tries
            foreach (int wrong in wrongButtonsPerTry)
            {
                totalWrongButtons += wrong;
                totalCorrectButtons += (totalButtonsToMatch - wrong);
                totalButtonsAttempted += totalButtonsToMatch;
            }

            // Add current try stats if game is not over and we're in the middle of a try
            if (!gameOver && currentTryWrongButtons > 0 && lockedButtons.Count < totalButtonsToMatch)
            {
                totalWrongButtons += currentTryWrongButtons;
                totalCorrectButtons += correctlyMatchedButtons.Count;
                totalButtonsAttempted += lockedButtons.Count;
            }

            // Calculate success rate as a percentage
            float successRate = 0;
            if (totalButtonsAttempted > 0)
            {
                successRate = (float)totalCorrectButtons / totalButtonsAttempted * 100f;
            }

            // Create a simpler approach with clear formatting
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Title centered at the top
            sb.AppendLine("<size=36>Game Analytics</size>");
            sb.AppendLine();

            // Section headers
            sb.AppendLine("<color=#FF9900><size=20><b>PER-TRY RESULTS:</b></size></color>");
            sb.AppendLine();

            // Show results for each try with enhanced formatting
            for (int i = 0; i < wrongButtonsPerTry.Count; i++)
            {
                int correctButtons = totalButtonsToMatch - wrongButtonsPerTry[i];
                sb.AppendLine($"Try {i + 1}: <color=green>{correctButtons} correct</color>, <color=red>{wrongButtonsPerTry[i]} wrong</color>");
            }

            // Show current try if game is not over and we're in the middle of a try
            if (!gameOver && currentTryWrongButtons > 0 && lockedButtons.Count < totalButtonsToMatch)
            {
                int currentCorrectButtons = correctlyMatchedButtons.Count;
                sb.AppendLine($"Current Try {wrongButtonsPerTry.Count + 1}: <color=green>{currentCorrectButtons} correct</color>, <color=red>{currentTryWrongButtons} wrong</color> so far");
            }

            sb.AppendLine();
            sb.AppendLine("* * * * * * * * * * * * * * *");
            sb.AppendLine();

            // Game summary header
            sb.AppendLine("<color=#FF9900><size=20><b>GAME SUMMARY:</b></size></color>");
            sb.AppendLine();

            // Add summary statistics with enhanced formatting
            sb.AppendLine($"Total Buttons Attempted: {totalButtonsAttempted}");
            sb.AppendLine($"Total Correct Matches: <color=green>{totalCorrectButtons}</color>");
            sb.AppendLine($"Total Wrong Matches: <color=red>{totalWrongButtons}</color>");
            sb.AppendLine($"Success Rate: <color=yellow>{successRate:F1}%</color>");
            sb.AppendLine($"Tries Used: {maxTries - currentTries}/{maxTries}");
            sb.AppendLine();

            // Add a performance rating based on success rate
            sb.AppendLine("Performance Rating:");
            if (successRate >= 90)
                sb.AppendLine("<color=green><b>EXCELLENT!</b></color>");
            else if (successRate >= 70)
                sb.AppendLine("<color=green><b>GREAT!</b></color>");
            else if (successRate >= 50)
                sb.AppendLine("<color=yellow><b>GOOD</b></color>");
            else if (successRate >= 30)
                sb.AppendLine("<color=orange><b>FAIR</b></color>");
            else
                sb.AppendLine("<color=red><b>NEEDS IMPROVEMENT</b></color>");

            // Set the text content
            analyticsText.text = sb.ToString();
        }
        else
        {
            Debug.LogError("Analytics text is null, cannot update!");
        }
    }



    // Retry the game - reloads the entire scene
    // This is the ONLY method that should be called when the player presses retry
    private void RetryGame()
    {
        // Make sure cursor is visible before reloading
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Set static flags to ensure proper reset
        SimpleDemoManager.IsGameRestarting = true;
        ForceGameStateReset = true;

        // Save analytics data before reloading
        UpdateAnalyticsData();

        // Get the current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Reload the current scene to restart everything including the demo
        SceneManager.LoadScene(currentSceneName);
    }





    // Update analytics data for saving
    private void UpdateAnalyticsData()
    {
        // Initialize analytics data if needed
        if (analyticsData == null)
        {
            analyticsData = new List<string>();
        }

        // Add game summary
        analyticsData.Add($"Game Session: {Time.time}");
        analyticsData.Add($"Tries Used: {maxTries - currentTries}/{maxTries}");

        // Add try data
        for (int i = 0; i < wrongButtonsPerTry.Count; i++)
        {
            int tryNumber = i + 1;
            int wrongButtons = wrongButtonsPerTry[i];
            int correctButtons = totalButtonsToMatch - wrongButtons;
            analyticsData.Add($"Try {tryNumber}: {correctButtons} correct, {wrongButtons} wrong");
        }

        // Add current try if in progress
        if (currentTryWrongButtons > 0 || currentProgress > 0)
        {
            int currentCorrectButtons = currentProgress - currentTryWrongButtons;
            if (currentCorrectButtons < 0) currentCorrectButtons = 0;

            analyticsData.Add($"Current Try {wrongButtonsPerTry.Count + 1}: {currentCorrectButtons} correct, {currentTryWrongButtons} wrong");
        }
    }

    // Go to main menu
    private void GoToMainMenu()
    {
        // Make sure cursor is visible before loading main menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Load the main menu scene
        // Note: You need to set up the scene name in the build settings
        SceneManager.LoadScene("MainMenu");
    }

    // Exit the game
    private void ExitGame()
    {
        // In the editor, this will stop play mode
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // In a build, this will quit the application
        Application.Quit();
        #endif
    }

    // Ensure cursor is visible and unlocked
    private void DisablePlayerMovement()
    {
        // Ensure cursor is visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Ensure cursor is visible and unlocked
    private void EnablePlayerMovement()
    {
        // Ensure cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Public method to check if the game is over
    public bool IsGameOver()
    {
        return gameOver;
    }

    // Delayed check to ensure game state is properly reset after demo
    private IEnumerator DelayedGameStateCheck()
    {
        // Wait for 5 seconds to ensure demo has completed
        yield return new WaitForSeconds(5f);

        // Check if gameOver is still true
        if (gameOver)
        {
            gameOver = false;
        }

        // Check if ForceGameStateReset is still true
        if (ForceGameStateReset)
        {
            ForceGameStateReset = false;
        }
    }


    // Reset a number's color after a delay
    private IEnumerator ResetNumberColorAfterDelay(GameObject numberObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (numberObject != null)
        {
            Renderer renderer = numberObject.GetComponent<Renderer>();
            if (renderer != null && originalNumberMaterials.ContainsKey(numberObject))
            {
                renderer.material = originalNumberMaterials[numberObject];
            }
        }
    }
}
