using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Static class for game state reset
public static class GameStateResetter
{
    public static void ResetAllStaticVariables()
    {
        // Reset all static variables here
        BasicButtonLampGame.ForceGameStateReset = true;
        SimpleDemoManager.IsGameRestarting = true;
    }
}

public class BasicButtonLampGame : MonoBehaviour
{
    // Static flag to ensure game state is reset properly
    public static bool ForceGameStateReset = false;

    [Header("Button References")]
    public GameObject button1;
    public GameObject button2;
    public GameObject button3;
    public GameObject button4;
    public GameObject button5;

    [Header("Lamp Base References")]
    public GameObject lampBase1;
    public GameObject lampBase2;
    public GameObject lampBase3;
    public GameObject lampBase4;
    public GameObject lampBase5;
    public GameObject lampBase6;
    public GameObject lampBase7;
    public GameObject lampBase8;
    public GameObject lampBase9;
    public GameObject lampBase10;

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
    public int totalButtonsToMatch = 5;

    [Header("Win/Lose Sounds")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public float soundVolume = 1.0f;
    private AudioSource audioSource;

    // State tracking
    private GameObject selectedButton = null;
    private Material originalButtonMaterial = null;
    public int currentTries; // Made public for PersistentGameManager access
    public int currentProgress; // Made public for PersistentGameManager access
    private bool gameOver = false;
    private bool currentTryFailed = false;

    // Dictionary to store original materials for number objects
    private Dictionary<GameObject, Material> originalNumberMaterials = new Dictionary<GameObject, Material>();

    // Arrays to store all buttons and lamp bases
    private GameObject[] buttons;
    private GameObject[] lampBases;

    // Lists to track locked buttons and lamps
    private List<GameObject> lockedButtons = new List<GameObject>();
    private List<GameObject> lockedLampBases = new List<GameObject>();
    private List<GameObject> correctlyMatchedButtons = new List<GameObject>();

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
                Debug.Log("ESC key pressed - closing analytics panel");
                HideAnalyticsPanel();
            }
        }
    }

    private void Start()
    {
        // Initialize arrays for buttons and lamp bases if they're null
        if (buttons == null)
        {
            buttons = new GameObject[5];
        }

        if (lampBases == null)
        {
            lampBases = new GameObject[10];
        }

        // Always find references when starting, especially important after game restart
        FindAllReferences();

        // Continue with the rest of the start method
        Start_Continued();
    }

    // Find all references in one go
    private void FindAllReferences()
    {
        // Find all button and lamp references first using tags
        FindButtonAndLampReferences();

        // Then find all UI references
        FindAllUIReferences();

        // Double check that we found all button and lamp references
        if (button1 == null || button2 == null || button3 == null || button4 == null || button5 == null)
        {
            FindButtonAndLampReferences();
        }

        // Set up game manager references for all buttons and lamp bases
        SetupGameManagerReferences();
    }

    // Set up game manager references for all buttons and lamp bases
    private void SetupGameManagerReferences()
    {
        // Set up references for all buttons
        GameObject[] allButtons = new GameObject[] { button1, button2, button3, button4, button5 };
        foreach (GameObject btn in allButtons)
        {
            if (btn != null)
            {
                BasicButton basicButton = btn.GetComponent<BasicButton>();
                if (basicButton != null)
                {
                    basicButton.gameManager = this;
                }
            }
        }

        // Set up references for all lamp bases
        GameObject[] allLampBases = new GameObject[]
        {
            lampBase1, lampBase2, lampBase3, lampBase4, lampBase5,
            lampBase6, lampBase7, lampBase8, lampBase9, lampBase10
        };

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
            Debug.Log("Cleared instruction text as requested by user");
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
            Debug.Log("Set up general analytics button");
        }

        // Win panel analytics button
        if (winAnalyticsButton != null)
        {
            // Remove any existing listeners first to avoid duplicates
            winAnalyticsButton.onClick.RemoveAllListeners();
            winAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
            Debug.Log("Set up win panel analytics button");
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
            Debug.Log("Set up lose panel analytics button");
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
            Debug.Log("Set up win panel retry button");
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
                    Debug.Log("Set up analytics panel close button: " + button.name);
                }
            }
        }
    }

    // Called when a try is complete (all buttons are locked)
    private void FinishCurrentTry()
    {
        Debug.Log("FinishCurrentTry called - starting try completion process");

        // This method is only called for failed tries

        // Calculate correct and wrong buttons
        int correctButtons = totalButtonsToMatch - currentTryWrongButtons;

        // Record analytics for this try
        wrongButtonsPerTry.Add(currentTryWrongButtons);
        Debug.Log($"Try {maxTries - currentTries + 1} completed with {currentTryWrongButtons} wrong buttons and {correctButtons} correct buttons");

        // Decrease tries
        currentTries--;
        UpdateTriesText();
        Debug.Log($"Decreased tries to {currentTries}");

        // Don't show message about failed try - user doesn't want this text
        Debug.Log("Skipping instruction text update for failed try as requested by user");

        // Show try analytics panel
        Debug.Log("About to show try analytics panel");
        ShowTryAnalyticsPanel(correctButtons, currentTryWrongButtons);

        // Check if out of tries - but don't end the game yet
        // We'll let the analytics panel show first, then end the game
        if (currentTries <= 0)
        {
            Debug.Log("Out of tries, starting coroutine to show game over after analytics");
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
        Debug.Log($"ShowTryAnalyticsPanel called with correctButtons={correctButtons}, wrongButtons={wrongButtons}");

        if (tryAnalyticsPanel != null)
        {
            Debug.Log("tryAnalyticsPanel is not null, proceeding");

            // Update try analytics text with enhanced formatting
            if (tryAnalyticsText != null)
            {
                Debug.Log("tryAnalyticsText is not null, updating text");

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
                Debug.Log("Try analytics text updated");
            }
            else
            {
                Debug.LogError("tryAnalyticsText is null!");
            }

            // Show the panel
            tryAnalyticsPanel.SetActive(true);
            Debug.Log("Try analytics panel activated");

            // Start coroutine to hide panel and start next try after delay
            Debug.Log($"Starting coroutine to hide panel after {tryAnalyticsDuration} seconds");
            StartCoroutine(HideTryAnalyticsPanelAfterDelay());
        }
        else
        {
            Debug.LogError("tryAnalyticsPanel is null! Check the Inspector assignment");
            // If panel doesn't exist, just start the next try
            Debug.Log($"Starting next try after delay of {tryAnalyticsDuration} seconds");
            StartCoroutine(StartNextTryAfterDelay(tryAnalyticsDuration));
        }
    }

    // Hide try analytics panel after delay and start next try
    private IEnumerator HideTryAnalyticsPanelAfterDelay()
    {
        Debug.Log($"HideTryAnalyticsPanelAfterDelay started, waiting for {tryAnalyticsDuration} seconds");

        // Wait for the specified duration
        yield return new WaitForSeconds(tryAnalyticsDuration);
        Debug.Log("Wait completed in HideTryAnalyticsPanelAfterDelay");

        // Hide the panel
        if (tryAnalyticsPanel != null)
        {
            tryAnalyticsPanel.SetActive(false);
            Debug.Log("Try analytics panel hidden");
        }
        else
        {
            Debug.LogError("tryAnalyticsPanel is null when trying to hide it!");
        }

        // Start the next try
        Debug.Log("About to start next try from HideTryAnalyticsPanelAfterDelay");
        StartNextTry();
    }

    // Start the next try after a delay
    private IEnumerator StartNextTryAfterDelay(float delay)
    {
        Debug.Log($"StartNextTryAfterDelay started, waiting for {delay} seconds");
        yield return new WaitForSeconds(delay);
        Debug.Log("Wait completed in StartNextTryAfterDelay");
        StartNextTry();
    }

    // Start the next try
    private void StartNextTry()
    {
        Debug.Log("StartNextTry called - resetting game state for next try");

        // Reset game state for next try
        currentProgress = 0;
        currentTryFailed = false;
        currentTryWrongButtons = 0;
        correctlyMatchedButtons.Clear();
        lockedButtons.Clear();
        lockedLampBases.Clear();
        Debug.Log("Game state variables reset for next try");

        // Reset all materials
        ResetAllMaterials();
        Debug.Log("All materials reset");

        // Update UI
        UpdateProgressText();
        Debug.Log("Progress text updated");

        // Don't update instruction text - user doesn't want this text
        Debug.Log("Skipping instruction text update for next try as requested by user");

        Debug.Log("Next try setup complete");
    }

    // Find all UI references at runtime
    private void FindAllUIReferences()
    {
        Debug.Log("Finding all UI references at runtime");

        // Find button and lamp references first
        FindButtonAndLampReferences();

        // Find UI panels by tag
        GameObject[] uiPanels = GameObject.FindGameObjectsWithTag("UIPanel");
        Debug.Log($"Found {uiPanels.Length} UI panels with UIPanel tag");
        foreach (GameObject panel in uiPanels)
        {
            if (panel.name.ToLower().Contains("win") && !panel.name.ToLower().Contains("analytics"))
            {
                winPanel = panel;
                Debug.Log("Found Win Panel: " + panel.name);
            }
            else if (panel.name.ToLower().Contains("game") && panel.name.ToLower().Contains("over") ||
                     panel.name.ToLower().Contains("lose"))
            {
                losePanel = panel;
                Debug.Log("Found Lose Panel: " + panel.name);
            }
            else if (panel.name.ToLower().Contains("gameanalytics") ||
                    (panel.name.ToLower().Contains("analytics") && !panel.name.ToLower().Contains("try")))
            {
                analyticsPanel = panel;
                Debug.Log("Found Analytics Panel: " + panel.name);
            }
            else if (panel.name.ToLower().Contains("try") && panel.name.ToLower().Contains("analytics"))
            {
                tryAnalyticsPanel = panel;
                Debug.Log("Found Try Analytics Panel: " + panel.name);
            }
        }

        // Find UI text elements by tag
        GameObject[] uiTexts = GameObject.FindGameObjectsWithTag("UIText");
        foreach (GameObject textObj in uiTexts)
        {
            TMPro.TextMeshProUGUI text = textObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (text == null) continue;

            if (textObj.name.ToLower().Contains("press") || textObj.name.ToLower().Contains("instruction"))
            {
                instructionText = text;
            }
            else if (textObj.name.ToLower().Contains("progress"))
            {
                progressText = text;
            }
            else if (textObj.name.ToLower().Contains("tries"))
            {
                triesText = text;
            }
            else if ((textObj.name.ToLower().Contains("gameanalytics") ||
                     textObj.name.ToLower().Contains("analytics")) &&
                     !textObj.name.ToLower().Contains("try"))
            {
                analyticsText = text;
            }
            else if (textObj.name.ToLower().Contains("try") && textObj.name.ToLower().Contains("analytics"))
            {
                tryAnalyticsText = text;
            }
        }

        // Find UI buttons by tag
        GameObject[] uiButtons = GameObject.FindGameObjectsWithTag("UIButton");
        foreach (GameObject buttonObj in uiButtons)
        {
            Button button = buttonObj.GetComponent<Button>();
            if (button == null) continue;

            if (buttonObj.name.ToLower().Contains("restart") || buttonObj.name.ToLower().Contains("retry"))
            {
                if (buttonObj.transform.IsChildOf(winPanel.transform))
                {
                    winRetryButton = button;
                    Debug.Log("Found Win Retry Button: " + buttonObj.name);
                }
                else
                {
                    retryButton = button;
                    Debug.Log("Found Retry Button: " + buttonObj.name);
                }
            }
            else if (buttonObj.name.ToLower().Contains("main") && buttonObj.name.ToLower().Contains("menu"))
            {
                if (buttonObj.transform.IsChildOf(winPanel.transform))
                {
                    winMainMenuButton = button;
                    Debug.Log("Found Win Main Menu Button: " + buttonObj.name);
                }
                else
                {
                    mainMenuButton = button;
                    Debug.Log("Found Main Menu Button: " + buttonObj.name);
                }
            }
            else if (buttonObj.name.ToLower().Contains("exit"))
            {
                if (buttonObj.transform.IsChildOf(winPanel.transform))
                {
                    winExitButton = button;
                    Debug.Log("Found Win Exit Button: " + buttonObj.name);
                }
                else
                {
                    exitButton = button;
                    Debug.Log("Found Exit Button: " + buttonObj.name);
                }
            }
            else if (buttonObj.name.ToLower().Contains("analytics"))
            {
                if (buttonObj.transform.IsChildOf(winPanel.transform))
                {
                    winAnalyticsButton = button;
                    Debug.Log("Found Win Analytics Button: " + buttonObj.name);
                }
                else if (buttonObj.transform.IsChildOf(losePanel.transform))
                {
                    loseAnalyticsButton = button;
                    Debug.Log("Found Lose Analytics Button: " + buttonObj.name);
                }
                else if (buttonObj.name.ToLower().Contains("close"))
                {
                    analyticsButton = button;
                    Debug.Log("Found Close Analytics Button: " + buttonObj.name);
                }
            }
            // We're now using ESC key instead of CloseAnalyticsButton
        }

        // If we still have missing references, try to find them by name
        FindMissingReferencesByName();
    }


    // Find missing references by name
    private void FindMissingReferencesByName()
    {
        // Find UI panels by name
        if (winPanel == null)
        {
            GameObject winPanelObj = GameObject.Find("win");
            if (winPanelObj != null)
            {
                winPanel = winPanelObj;
                Debug.Log("Found Win Panel by name: " + winPanelObj.name);
            }
        }

        if (losePanel == null)
        {
            GameObject losePanelObj = GameObject.Find("game over");
            if (losePanelObj != null)
            {
                losePanel = losePanelObj;
                Debug.Log("Found Lose Panel by name: " + losePanelObj.name);
            }
        }

        if (analyticsPanel == null)
        {
            GameObject analyticsPanelObj = GameObject.Find("GameAnalyticsPanel");
            if (analyticsPanelObj != null)
            {
                analyticsPanel = analyticsPanelObj;
                Debug.Log("Found Analytics Panel by name: " + analyticsPanelObj.name);
            }
        }

        if (tryAnalyticsPanel == null)
        {
            GameObject tryAnalyticsPanelObj = GameObject.Find("TryAnalyticsPanel");
            if (tryAnalyticsPanelObj != null)
            {
                tryAnalyticsPanel = tryAnalyticsPanelObj;
                Debug.Log("Found Try Analytics Panel by name: " + tryAnalyticsPanelObj.name);
            }
        }

        // Find UI text elements by name
        if (instructionText == null)
        {
            GameObject instructionTextObj = GameObject.Find("press");
            if (instructionTextObj != null)
            {
                instructionText = instructionTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Found Instruction Text by name: " + instructionTextObj.name);
            }
        }

        if (progressText == null)
        {
            GameObject progressTextObj = GameObject.Find("progress");
            if (progressTextObj != null)
            {
                progressText = progressTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Found Progress Text by name: " + progressTextObj.name);
            }
        }

        if (triesText == null)
        {
            GameObject triesTextObj = GameObject.Find("Tries");
            if (triesTextObj != null)
            {
                triesText = triesTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Found Tries Text by name: " + triesTextObj.name);
            }
        }

        if (analyticsText == null)
        {
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Found Analytics Text by name: " + analyticsTextObj.name);
            }
        }

        // Find ESC instruction text
        if (escInstructionText == null && analyticsPanel != null)
        {
            // First try to find by name
            GameObject escTextObj = GameObject.Find("EscInstructionText");
            if (escTextObj != null)
            {
                escInstructionText = escTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Found ESC Instruction Text by name: " + escTextObj.name);
            }
            else
            {
                // Try to find any text in the analytics panel that might be for ESC instructions
                TMPro.TextMeshProUGUI[] texts = analyticsPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (TMPro.TextMeshProUGUI text in texts)
                {
                    if (text != analyticsText && (text.name.ToLower().Contains("esc") ||
                        text.name.ToLower().Contains("instruction") ||
                        text.name.ToLower().Contains("back")))
                    {
                        escInstructionText = text;
                        Debug.Log("Found ESC Instruction Text in analytics panel: " + text.name);
                        break;
                    }
                }
            }
        }

        if (tryAnalyticsText == null)
        {
            GameObject tryAnalyticsTextObj = GameObject.Find("TryAnalyticsText");
            if (tryAnalyticsTextObj != null)
            {
                tryAnalyticsText = tryAnalyticsTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Found Try Analytics Text by name: " + tryAnalyticsTextObj.name);
            }
        }

        // Find UI buttons by name
        if (retryButton == null)
        {
            GameObject retryButtonObj = GameObject.Find("restart");
            if (retryButtonObj != null && !retryButtonObj.transform.IsChildOf(winPanel?.transform))
            {
                retryButton = retryButtonObj.GetComponent<Button>();
                Debug.Log("Found Retry Button by name: " + retryButtonObj.name);
            }
        }

        if (mainMenuButton == null)
        {
            GameObject mainMenuButtonObj = GameObject.Find("MainMenu");
            if (mainMenuButtonObj != null && !mainMenuButtonObj.transform.IsChildOf(winPanel?.transform))
            {
                mainMenuButton = mainMenuButtonObj.GetComponent<Button>();
                Debug.Log("Found Main Menu Button by name: " + mainMenuButtonObj.name);
            }
        }

        if (exitButton == null)
        {
            GameObject exitButtonObj = GameObject.Find("exit");
            if (exitButtonObj != null && !exitButtonObj.transform.IsChildOf(winPanel?.transform))
            {
                exitButton = exitButtonObj.GetComponent<Button>();
                Debug.Log("Found Exit Button by name: " + exitButtonObj.name);
            }
        }

        if (winRetryButton == null && winPanel != null)
        {
            Transform winRetryButtonTrans = winPanel.transform.Find("restart");
            if (winRetryButtonTrans != null)
            {
                winRetryButton = winRetryButtonTrans.GetComponent<Button>();
                Debug.Log("Found Win Retry Button by name: " + winRetryButtonTrans.name);
            }
        }

        if (winMainMenuButton == null && winPanel != null)
        {
            Transform winMainMenuButtonTrans = winPanel.transform.Find("MainMenu");
            if (winMainMenuButtonTrans != null)
            {
                winMainMenuButton = winMainMenuButtonTrans.GetComponent<Button>();
                Debug.Log("Found Win Main Menu Button by name: " + winMainMenuButtonTrans.name);
            }
        }

        if (winExitButton == null && winPanel != null)
        {
            Transform winExitButtonTrans = winPanel.transform.Find("exit");
            if (winExitButtonTrans != null)
            {
                winExitButton = winExitButtonTrans.GetComponent<Button>();
                Debug.Log("Found Win Exit Button by name: " + winExitButtonTrans.name);
            }
        }

        if (winAnalyticsButton == null && winPanel != null)
        {
            Transform winAnalyticsButtonTrans = winPanel.transform.Find("analytics");
            if (winAnalyticsButtonTrans != null)
            {
                winAnalyticsButton = winAnalyticsButtonTrans.GetComponent<Button>();
                Debug.Log("Found Win Analytics Button by name: " + winAnalyticsButtonTrans.name);
            }
        }

        if (loseAnalyticsButton == null && losePanel != null)
        {
            Transform loseAnalyticsButtonTrans = losePanel.transform.Find("analytics");
            if (loseAnalyticsButtonTrans != null)
            {
                loseAnalyticsButton = loseAnalyticsButtonTrans.GetComponent<Button>();
                Debug.Log("Found Lose Analytics Button by name: " + loseAnalyticsButtonTrans.name);
            }
        }
    }

    // Reset all materials
    private void ResetAllMaterials()
    {
        Debug.Log("ResetAllMaterials called");

        // Reset button materials
        foreach (GameObject button in new List<GameObject> { button1, button2, button3, button4, button5 })
        {
            if (button != null)
            {
                Renderer renderer = button.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create a new default material if originalButtonMaterial is null
                    if (originalButtonMaterial == null)
                    {
                        Debug.LogWarning("originalButtonMaterial is null, creating a new default material");
                        originalButtonMaterial = new Material(Shader.Find("Standard"));
                        originalButtonMaterial.color = Color.white;
                    }

                    renderer.material = originalButtonMaterial;
                    Debug.Log($"Reset material for button: {button.name}");
                }
                else
                {
                    Debug.LogWarning($"No renderer found on button: {button.name}");
                }
            }
        }

        // Reset number materials
        ResetAllNumbers();

        Debug.Log("All materials have been reset");
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
        int result = -1;

        if (button == button1) result = 5;
        else if (button == button2) result = 6;
        else if (button == button3) result = 7;
        else if (button == button4) result = 8;
        else if (button == button5) result = 9;

        Debug.Log($"GetCorrectNumberForButton: Button {button.name} maps to lamp number {result}");

        if (result == -1)
        {
            // Try to identify the button by name if reference comparison failed
            string buttonName = button.name.ToLower();
            if (buttonName.Contains("1") || buttonName.Contains("one")) result = 5;
            else if (buttonName.Contains("2") || buttonName.Contains("two")) result = 6;
            else if (buttonName.Contains("3") || buttonName.Contains("three")) result = 7;
            else if (buttonName.Contains("4") || buttonName.Contains("four")) result = 8;
            else if (buttonName.Contains("5") || buttonName.Contains("five")) result = 9;

            if (result != -1)
            {
                Debug.Log($"Identified button by name: {buttonName} -> lamp number {result}");
            }
            else
            {
                Debug.LogWarning($"Could not determine lamp number for button: {button.name}");
            }
        }

        return result;
    }

    // Get lamp base by number
    private GameObject GetLampBaseByNumber(int number)
    {
        switch (number)
        {
            case 1: return lampBase1;
            case 2: return lampBase2;
            case 3: return lampBase3;
            case 4: return lampBase4;
            case 5: return lampBase5;
            case 6: return lampBase6;
            case 7: return lampBase7;
            case 8: return lampBase8;
            case 9: return lampBase9;
            case 10: return lampBase10;
            default: return null;
        }
    }

    // Get number object from lamp base
    private GameObject GetNumberObjectFromLampBase(GameObject lampBase)
    {
        if (lampBase == null) return null;

        BasicNumber basicNumber = lampBase.GetComponent<BasicNumber>();
        if (basicNumber != null)
        {
            return basicNumber.numberObject;
        }

        return null;
    }

    // Call this from button click events
    public void ButtonClicked(GameObject button)
    {
        // Check if the game is over or if we're in the process of resetting
        if (gameOver || ForceGameStateReset)
        {
            Debug.Log("Game is over or resetting, ignoring button click");
            return;
        }

        // Double-check with SimpleDemoManager if the demo is still running
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.IsDemoRunning())
        {
            Debug.Log("Demo is still running, ignoring button click");
            return;
        }

        Debug.Log("Button clicked: " + button.name);

        // Check if the button is locked
        if (lockedButtons.Contains(button))
        {
            Debug.Log("Button is locked, ignoring click");
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
        Debug.Log($"NumberClicked called - lampBase: {lampBase.name}, numberObject: {(numberObject != null ? numberObject.name : "null")}, numberValue: {numberValue}");

        // Check if the game is over or if we're in the process of resetting
        if (gameOver || ForceGameStateReset)
        {
            Debug.Log("Game is over or resetting, ignoring lamp click");
            return;
        }

        // Double-check with SimpleDemoManager if the demo is still running
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.IsDemoRunning())
        {
            Debug.Log("Demo is still running, ignoring lamp click");
            return;
        }

        Debug.Log("Lamp base clicked: " + lampBase.name + ", Number value: " + numberValue);

        // Check if the lamp base is locked
        if (lockedLampBases.Contains(lampBase))
        {
            Debug.Log("Lamp base is locked, ignoring click");
            return;
        }

        // If no button is selected, ignore the number click
        if (selectedButton == null)
        {
            Debug.Log("No button selected, ignoring lamp click");
            return;
        }

        Debug.Log($"Selected button: {selectedButton.name}, checking if it matches with number {numberValue}");

        // Check if this is the correct match
        bool isCorrect = IsCorrectMatch(selectedButton, numberValue);
        Debug.Log($"Match result: {(isCorrect ? "CORRECT" : "WRONG")}");

        if (isCorrect)
        {
            Debug.Log("Correct match! Processing success...");

            // Lock the button and lamp base with success visual
            LockButtonSuccess(selectedButton);
            LockLampSuccess(lampBase, numberObject);

            // Add to locked lists
            lockedButtons.Add(selectedButton);
            lockedLampBases.Add(lampBase);
            Debug.Log($"Added to locked lists - lockedButtons count: {lockedButtons.Count}, lockedLampBases count: {lockedLampBases.Count}");

            // Add to correctly matched buttons list
            correctlyMatchedButtons.Add(selectedButton);
            Debug.Log($"Added to correctly matched buttons - count: {correctlyMatchedButtons.Count}");

            // Update progress
            currentProgress = correctlyMatchedButtons.Count;
            UpdateProgressText();
            Debug.Log($"Updated progress to {currentProgress}/{totalButtonsToMatch}");

            // Only check for win if all buttons have been used
            if (lockedButtons.Count >= totalButtonsToMatch)
            {
                Debug.Log($"All buttons used (lockedButtons count: {lockedButtons.Count} >= totalButtonsToMatch: {totalButtonsToMatch})");

                // Check if all matches were correct (no failures)
                if (!currentTryFailed && currentProgress >= totalButtonsToMatch)
                {
                    Debug.Log("All matches were correct! Player wins!");
                    // Player wins!
                    GameWon();
                }
                else
                {
                    Debug.Log("Some matches were incorrect. This try is complete and failed.");
                    // This try is complete and failed
                    FinishCurrentTry();
                }
            }
            else
            {
                Debug.Log($"Not all buttons used yet. Continue playing. (lockedButtons count: {lockedButtons.Count} < totalButtonsToMatch: {totalButtonsToMatch})");
            }
        }
        else
        {
            Debug.Log("Wrong match! Processing failure...");

            // Lock the button and lamp base with failure visual
            LockButtonFailure(selectedButton);
            LockLampFailure(lampBase, numberObject);

            // Add to locked lists
            lockedButtons.Add(selectedButton);
            lockedLampBases.Add(lampBase);
            Debug.Log($"Added to locked lists - lockedButtons count: {lockedButtons.Count}, lockedLampBases count: {lockedLampBases.Count}");

            // Mark this try as failed
            currentTryFailed = true;
            Debug.Log("Marked current try as failed");

            // Increment wrong buttons counter
            currentTryWrongButtons++;
            Debug.Log($"Incremented wrong buttons counter to {currentTryWrongButtons}");

            // Check if all buttons are locked (try is complete)
            if (lockedButtons.Count >= totalButtonsToMatch)
            {
                Debug.Log($"All buttons used (lockedButtons count: {lockedButtons.Count} >= totalButtonsToMatch: {totalButtonsToMatch})");
                // This try is complete and failed
                FinishCurrentTry();
            }
            else
            {
                Debug.Log($"Not all buttons used yet. Continue playing. (lockedButtons count: {lockedButtons.Count} < totalButtonsToMatch: {totalButtonsToMatch})");
            }
        }

        // Clear the button selection
        selectedButton = null;
        Debug.Log("Cleared button selection");
    }

    // These methods have been removed as we no longer need to show the correct answer

    // Check if the button and number value match
    private bool IsCorrectMatch(GameObject button, int numberValue)
    {
        // Fixed mappings:
        // Button 1 → Number 5
        // Button 2 → Number 6
        // Button 3 → Number 7
        // Button 4 → Number 8
        // Button 5 → Number 9

        Debug.Log($"Checking match: Button={button.name}, NumberValue={numberValue}");

        if (button == button1 && numberValue == 5) {
            Debug.Log("Match found: Button 1 → Number 5");
            return true;
        }
        if (button == button2 && numberValue == 6) {
            Debug.Log("Match found: Button 2 → Number 6");
            return true;
        }
        if (button == button3 && numberValue == 7) {
            Debug.Log("Match found: Button 3 → Number 7");
            return true;
        }
        if (button == button4 && numberValue == 8) {
            Debug.Log("Match found: Button 4 → Number 8");
            return true;
        }
        if (button == button5 && numberValue == 9) {
            Debug.Log("Match found: Button 5 → Number 9");
            return true;
        }

        Debug.Log("No match found");
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
        Debug.Log("Locking button with success: " + button.name);

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
        Debug.Log("Locking button with failure: " + button.name);

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
        Debug.Log("Locking lamp with success: " + lampBase.name);

        if (numberObject == null)
        {
            Debug.LogError("Number object is null!");
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
        else
        {
            Debug.LogError("Number object has no renderer: " + numberObject.name);
        }
    }

    // Lock a lamp with failure visual
    private void LockLampFailure(GameObject lampBase, GameObject numberObject)
    {
        Debug.Log("Locking lamp with failure: " + lampBase.name);

        if (numberObject == null)
        {
            Debug.LogError("Number object is null!");
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
        else
        {
            Debug.LogError("Number object has no renderer: " + numberObject.name);
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

    // Reset the game state (internal version)
    private void ResetGameState()
    {
        Debug.Log("ResetGameState called - resetting internal game state...");

        // Find references again in case they were lost during scene reload
        FindAllUIReferences();

        // Make sure analytics panel is hidden
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
            Debug.Log("Ensuring analytics panel is hidden during reset");
        }

        // Explicitly reset game state
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

        // Reset all locked buttons
        foreach (GameObject button in lockedButtons)
        {
            if (button != null)
            {
                Renderer renderer = button.GetComponent<Renderer>();
                if (renderer != null && originalButtonMaterial != null)
                {
                    renderer.material = originalButtonMaterial;
                }
            }
        }

        // Reset all numbers
        ResetAllNumbers();

        // Clear locked lists
        lockedButtons.Clear();
        lockedLampBases.Clear();
        correctlyMatchedButtons.Clear();

        // Reset UI
        UpdateProgressText();
        UpdateTriesText();

        // Hide panels
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (tryAnalyticsPanel != null) tryAnalyticsPanel.SetActive(false);

        Debug.Log("Game reset complete");
    }

    // Reset all numbers to their original appearance
    private void ResetAllNumbers()
    {
        Debug.Log($"ResetAllNumbers called - originalNumberMaterials count: {originalNumberMaterials.Count}");

        // Create a list of all number objects to check
        List<GameObject> allNumberObjects = new List<GameObject>();

        // Helper function to safely add child objects
        void SafelyAddChildObject(GameObject parent)
        {
            if (parent != null)
            {
                try
                {
                    if (parent.transform.childCount > 0)
                    {
                        GameObject child = parent.transform.GetChild(0).gameObject;
                        allNumberObjects.Add(child);
                        Debug.Log($"Added number object {child.name} from parent {parent.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"Lamp base {parent.name} has no children");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error accessing child of {parent.name}: {e.Message}");
                }
            }
        }

        // Add all lamp base children
        SafelyAddChildObject(lampBase1);
        SafelyAddChildObject(lampBase2);
        SafelyAddChildObject(lampBase3);
        SafelyAddChildObject(lampBase4);
        SafelyAddChildObject(lampBase5);
        SafelyAddChildObject(lampBase6);
        SafelyAddChildObject(lampBase7);
        SafelyAddChildObject(lampBase8);
        SafelyAddChildObject(lampBase9);
        SafelyAddChildObject(lampBase10);

        // Reset materials from the dictionary
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
                    Debug.Log($"Reset material for number object: {numberObject.name}");
                }
                else
                {
                    Debug.LogWarning($"No renderer found on number object: {numberObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("Null number object in originalNumberMaterials dictionary");
            }
        }

        // Check for any number objects that might not be in the dictionary
        foreach (GameObject numberObject in allNumberObjects)
        {
            if (numberObject != null && !originalNumberMaterials.ContainsKey(numberObject))
            {
                Renderer renderer = numberObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Create a default material if needed
                    Material defaultMaterial = new Material(Shader.Find("Standard"));
                    defaultMaterial.color = Color.white;

                    // Store it in the dictionary
                    originalNumberMaterials[numberObject] = defaultMaterial;

                    // Apply it
                    renderer.material = defaultMaterial;
                    Debug.Log($"Created and applied default material for number object: {numberObject.name}");
                }
            }
        }

        Debug.Log("All number materials have been reset");
    }

    // Called when the player wins the game
    private void GameWon()
    {
        Debug.Log("Game Won! All buttons matched correctly!");
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
        Debug.Log("Game Lost! Out of tries!");
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
        Debug.Log("ShowAnalyticsPanel called");

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
                    Debug.Log("Found analytics panel by tag: " + panel.name);
                    break;
                }
            }

            // If still not found, try direct name
            if (analyticsPanel == null)
            {
                analyticsPanel = GameObject.Find("GameAnalyticsPanel");
                if (analyticsPanel != null)
                {
                    Debug.Log("Found analytics panel by name: " + analyticsPanel.name);
                }
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
                    Debug.Log("Found ESC Instruction Text in analytics panel: " + text.name);
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

                Debug.Log("Created new ESC Instruction Text");
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
            Debug.Log("Updated ESC instruction text");
        }
        else
        {
            Debug.LogWarning("Could not create or find ESC instruction text!");
        }

        // Find the analytics text component
        if (analyticsText == null)
        {
            // Try to find by name first
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("Found analytics text by name: " + analyticsTextObj.name);
            }
            else
            {
                // Try to find any TextMeshProUGUI in the panel
                TextMeshProUGUI[] allTexts = analyticsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (allTexts.Length > 0)
                {
                    analyticsText = allTexts[0];
                    Debug.Log("Using first TextMeshProUGUI found in panel: " + analyticsText.name);
                }
            }
        }

        // Add or get the AnalyticsTextFixer component
        AnalyticsTextFixer fixer = analyticsPanel.GetComponent<AnalyticsTextFixer>();
        if (fixer == null)
        {
            fixer = analyticsPanel.AddComponent<AnalyticsTextFixer>();
            Debug.Log("Added AnalyticsTextFixer to analytics panel");
        }

        // Assign the analytics text to the fixer
        if (analyticsText != null)
        {
            fixer.analyticsText = analyticsText;
            Debug.Log("Assigned analytics text to fixer");
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
            Debug.Log("Analytics text updated successfully");
        }
        else
        {
            Debug.LogError("Could not find or create analytics text component!");
        }

        Debug.Log("Analytics panel shown");
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
                        Debug.Log("Showing lose panel after hiding analytics panel");
                    }
                }
                else
                {
                    // Game was won, show win panel
                    if (winPanel != null)
                    {
                        winPanel.SetActive(true);
                        Debug.Log("Showing win panel after hiding analytics panel");
                    }
                }
            }

            Debug.Log("Analytics panel hidden");
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
        Debug.Log("UpdateAnalyticsText called");

        // Double-check if analyticsText is null
        if (analyticsText == null && analyticsPanel != null)
        {
            // Try to find by name first
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TextMeshProUGUI>();
                Debug.Log("Found analytics text by name: " + analyticsTextObj.name);
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
                else
                {
                    Debug.Log("Found analytics text in panel: " + analyticsText.name);
                }
            }

            // Get or add the AnalyticsTextFixer component
            AnalyticsTextFixer fixer = analyticsPanel.GetComponent<AnalyticsTextFixer>();
            if (fixer == null)
            {
                fixer = analyticsPanel.AddComponent<AnalyticsTextFixer>();
                Debug.Log("Added AnalyticsTextFixer to analytics panel in UpdateAnalyticsText");
            }

            // Assign the analytics text to the fixer
            fixer.analyticsText = analyticsText;
            Debug.Log("Assigned analytics text to fixer in UpdateAnalyticsText");

            // Force the fixer to run its setup
            fixer.Start();
        }

        if (analyticsText != null)
        {
            Debug.Log("Updating analytics text content");

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
            Debug.Log("Analytics text updated successfully");
        }
        else
        {
            Debug.LogError("Analytics text is null, cannot update!");
        }
    }

    // Public method to reset the game
    public void ResetGame()
    {
        Debug.Log("ResetGame called - resetting game state");

        // First, refresh all references to ensure we have valid objects
        RefreshAllReferences();

        // Make sure analytics panel is hidden at start
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
            Debug.Log("Ensuring analytics panel is hidden during reset");
        }

        // Set the restart flags to true so the demo will run again and game state will be reset
        SimpleDemoManager.IsGameRestarting = true;
        ForceGameStateReset = true;

        // Reset game state
        gameOver = false;

        // Reset all game state variables
        selectedButton = null;
        currentTries = maxTries;
        currentProgress = 0;
        currentTryFailed = false;
        currentTryWrongButtons = 0;
        wrongButtonsPerTry.Clear();
        lockedButtons.Clear();
        lockedLampBases.Clear();
        correctlyMatchedButtons.Clear();

        // Reset all materials
        ResetAllMaterials();

        // Reset UI
        UpdateProgressText();
        UpdateTriesText();

        // Hide all panels
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (tryAnalyticsPanel != null) tryAnalyticsPanel.SetActive(false);

        // Keep instruction text clear
        if (instructionText != null)
        {
            instructionText.text = "";
            Debug.Log("Kept instruction text clear in ResetGame");
        }

        // Re-enable player movement that might have been disabled
        EnablePlayerMovement();

        // Restart the demo phase
        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null)
        {
            // Reset the demo manager to start the demo again
            demoManager.isGameplayActive = false;
            demoManager.isDemoRunning = false;
            demoManager.demoCompleted = false;
            demoManager.ResetDemo();
            Debug.Log("Demo manager found and reset to restart demo phase");
        }
        else
        {
            Debug.LogWarning("Could not find SimpleDemoManager!");
        }

        // Set up button listeners again
        SetupButtonListeners();

        // We're now using ESC key instead of CloseAnalyticsButton

        Debug.Log("Game reset complete");
    }

    // Retry the game
    private void RetryGame()
    {
        Debug.Log("Restarting game with full scene reload...");

        // Make sure cursor is visible before reloading
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Set static flags to ensure proper reset
        SimpleDemoManager.IsGameRestarting = true;
        ForceGameStateReset = true;

        // Save analytics data before reloading
        if (analyticsData == null)
        {
            analyticsData = new List<string>();
        }

        // Add current game data to analytics
        UpdateAnalyticsData();

        // Get the current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log("Reloading current scene: " + currentSceneName);

        // Reload the current scene to restart everything including the demo
        SceneManager.LoadScene(currentSceneName);
    }

    // Find all button and lamp references
    private void FindButtonAndLampReferences()
    {
        // First try to find by tag
        FindButtonsAndLampsByTag();

        // If we still have missing references, try to find by name as a fallback
        if (button1 == null || button2 == null || button3 == null || button4 == null || button5 == null ||
            lampBase1 == null || lampBase2 == null || lampBase3 == null || lampBase4 == null || lampBase5 == null ||
            lampBase6 == null || lampBase7 == null || lampBase8 == null || lampBase9 == null || lampBase10 == null)
        {
            FindButtonsAndLampsByName();
        }
    }

    // Find buttons and lamps by tag
    private void FindButtonsAndLampsByTag()
    {
        // Find all buttons by tag
        GameObject[] taggedButtons = GameObject.FindGameObjectsWithTag("Button");

        foreach (GameObject buttonObj in taggedButtons)
        {
            // Try to determine which button this is by name
            string buttonName = buttonObj.name.ToLower();

            if (buttonName.Contains("1") || buttonName.Contains("one"))
            {
                button1 = buttonObj;
                if (buttons != null && buttons.Length > 0) buttons[0] = buttonObj;
            }
            else if (buttonName.Contains("2") || buttonName.Contains("two"))
            {
                button2 = buttonObj;
                if (buttons != null && buttons.Length > 1) buttons[1] = buttonObj;
            }
            else if (buttonName.Contains("3") || buttonName.Contains("three"))
            {
                button3 = buttonObj;
                if (buttons != null && buttons.Length > 2) buttons[2] = buttonObj;
            }
            else if (buttonName.Contains("4") || buttonName.Contains("four"))
            {
                button4 = buttonObj;
                if (buttons != null && buttons.Length > 3) buttons[3] = buttonObj;
            }
            else if (buttonName.Contains("5") || buttonName.Contains("five"))
            {
                button5 = buttonObj;
                if (buttons != null && buttons.Length > 4) buttons[4] = buttonObj;
            }
        }

        // Find all lamp bases by tag
        GameObject[] taggedLamps = GameObject.FindGameObjectsWithTag("LampBase");

        foreach (GameObject lampObj in taggedLamps)
        {
            // Try to determine which lamp this is by name or by the BasicNumber component
            string lampName = lampObj.name.ToLower();
            BasicNumber basicNumber = lampObj.GetComponent<BasicNumber>();
            int lampNumber = -1;

            // Try to get the number from the BasicNumber component first
            if (basicNumber != null)
            {
                lampNumber = basicNumber.numberValue;
            }
            // If that fails, try to parse it from the name
            else if (lampName.Contains("lamp") || lampName.Contains("base"))
            {
                // Try to extract the number from the name
                for (int i = 1; i <= 10; i++)
                {
                    if (lampName.Contains(i.ToString()) ||
                        (i == 1 && lampName.Contains("one")) ||
                        (i == 2 && lampName.Contains("two")) ||
                        (i == 3 && lampName.Contains("three")) ||
                        (i == 4 && lampName.Contains("four")) ||
                        (i == 5 && lampName.Contains("five")) ||
                        (i == 6 && lampName.Contains("six")) ||
                        (i == 7 && lampName.Contains("seven")) ||
                        (i == 8 && lampName.Contains("eight")) ||
                        (i == 9 && lampName.Contains("nine")) ||
                        (i == 10 && lampName.Contains("ten")))
                    {
                        lampNumber = i;
                        break;
                    }
                }
            }

            // Assign the lamp to the correct reference based on its number
            if (lampNumber >= 1 && lampNumber <= 10)
            {
                switch (lampNumber)
                {
                    case 1: lampBase1 = lampObj; break;
                    case 2: lampBase2 = lampObj; break;
                    case 3: lampBase3 = lampObj; break;
                    case 4: lampBase4 = lampObj; break;
                    case 5: lampBase5 = lampObj; break;
                    case 6: lampBase6 = lampObj; break;
                    case 7: lampBase7 = lampObj; break;
                    case 8: lampBase8 = lampObj; break;
                    case 9: lampBase9 = lampObj; break;
                    case 10: lampBase10 = lampObj; break;
                }

                if (lampBases != null && lampNumber - 1 < lampBases.Length)
                {
                    lampBases[lampNumber - 1] = lampObj;
                }
            }
        }
    }

    // Find buttons and lamps by name as a fallback
    private void FindButtonsAndLampsByName()
    {
        // Find all lamp bases
        for (int i = 1; i <= 10; i++)
        {
            // Skip if we already have this reference
            if ((i == 1 && lampBase1 != null) || (i == 2 && lampBase2 != null) ||
                (i == 3 && lampBase3 != null) || (i == 4 && lampBase4 != null) ||
                (i == 5 && lampBase5 != null) || (i == 6 && lampBase6 != null) ||
                (i == 7 && lampBase7 != null) || (i == 8 && lampBase8 != null) ||
                (i == 9 && lampBase9 != null) || (i == 10 && lampBase10 != null))
            {
                continue;
            }

            string lampName = $"Lamp Base {i}";
            GameObject lampObj = GameObject.Find(lampName);
            if (lampObj != null)
            {
                switch (i)
                {
                    case 1: lampBase1 = lampObj; break;
                    case 2: lampBase2 = lampObj; break;
                    case 3: lampBase3 = lampObj; break;
                    case 4: lampBase4 = lampObj; break;
                    case 5: lampBase5 = lampObj; break;
                    case 6: lampBase6 = lampObj; break;
                    case 7: lampBase7 = lampObj; break;
                    case 8: lampBase8 = lampObj; break;
                    case 9: lampBase9 = lampObj; break;
                    case 10: lampBase10 = lampObj; break;
                }
                if (lampBases != null && i - 1 < lampBases.Length)
                {
                    lampBases[i-1] = lampObj;
                }
            }
        }

        // Find all buttons
        for (int i = 1; i <= 5; i++)
        {
            // Skip if we already have this reference
            if ((i == 1 && button1 != null) || (i == 2 && button2 != null) ||
                (i == 3 && button3 != null) || (i == 4 && button4 != null) ||
                (i == 5 && button5 != null))
            {
                continue;
            }

            string buttonName = $"Button {i}";
            GameObject buttonObj = GameObject.Find(buttonName);
            if (buttonObj != null)
            {
                switch (i)
                {
                    case 1: button1 = buttonObj; break;
                    case 2: button2 = buttonObj; break;
                    case 3: button3 = buttonObj; break;
                    case 4: button4 = buttonObj; break;
                    case 5: button5 = buttonObj; break;
                }
                if (buttons != null && i - 1 < buttons.Length)
                {
                    buttons[i-1] = buttonObj;
                }
            }
        }
    }

    // Update analytics data for saving
    private void UpdateAnalyticsData()
    {
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

        Debug.Log($"Saved {analyticsData.Count} analytics data entries");
    }

    // Go to main menu
    private void GoToMainMenu()
    {
        // Make sure cursor is visible before loading main menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // If we have a persistent game manager, it will handle cursor state across scenes
        PersistentGameManager persistentManager = FindObjectOfType<PersistentGameManager>();
        if (persistentManager != null && persistentManager.customCursor != null)
        {
            persistentManager.customCursor.SetCustomCursor();
        }

        // Load the main menu scene
        // Note: You need to set up the scene name in the build settings
        SceneManager.LoadScene("MainMenu");
    }

    // Exit the game
    private void ExitGame()
    {
        Debug.Log("Exiting game...");

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

        Debug.Log("Cursor unlocked and made visible");
    }

    // Ensure cursor is visible and unlocked
    private void EnablePlayerMovement()
    {
        // Ensure cursor is visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Cursor visibility ensured");
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
            Debug.LogWarning("Game state check: gameOver is still true after delay. Forcing reset...");
            gameOver = false;
        }

        // Check if ForceGameStateReset is still true
        if (ForceGameStateReset)
        {
            Debug.LogWarning("Game state check: ForceGameStateReset is still true after delay. Forcing reset...");
            ForceGameStateReset = false;
        }

        // Log the current state
        Debug.Log($"Delayed game state check - gameOver: {gameOver}, ForceGameStateReset: {ForceGameStateReset}");
    }

    // Refresh all references when the game is restarted
    public void RefreshAllReferences()
    {
        // Find all references again
        FindAllReferences();

        // Set up button listeners again
        SetupButtonListeners();
    }
}
