using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class BasicButtonLampGame : MonoBehaviour
{
    public static bool ForceGameStateReset = false;

    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI triesText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Analytics UI")]
    public Button analyticsButton;
    public Button winAnalyticsButton;
    public Button loseAnalyticsButton;
    public GameObject analyticsPanel;
    public TextMeshProUGUI analyticsText;
    public TextMeshProUGUI escInstructionText;

    [Header("Try Analytics UI")]
    public GameObject tryAnalyticsPanel;
    public TextMeshProUGUI tryAnalyticsText;
    public float tryAnalyticsDuration = 3f;

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

    private GameObject selectedButton = null;
    private Material originalButtonMaterial = null;
    public int currentTries;
    public int currentProgress;
    private bool gameOver = false;
    private bool currentTryFailed = false;

    private Dictionary<GameObject, Material> originalNumberMaterials = new Dictionary<GameObject, Material>();

    private List<GameObject> lockedButtons = new List<GameObject>();
    private List<GameObject> lockedLampBases = new List<GameObject>();
    private List<GameObject> correctlyMatchedButtons = new List<GameObject>();

    [Header("Button-Number Mapping")]
    public ButtonNumberMapping buttonNumberMapping;

    private List<int> wrongButtonsPerTry = new List<int>();
    private int currentTryWrongButtons = 0;

    public List<string> analyticsData = new List<string>();

    private Material greenMaterial;
    private Material redMaterial;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (analyticsPanel != null && analyticsPanel.activeSelf)
            {
                HideAnalyticsPanel();
            }
        }
    }

    private void Start()
    {
        FindAllReferences();
        InitializeButtonNumberMapping();
        Start_Continued();
    }

    private void InitializeButtonNumberMapping()
    {
        if (buttonNumberMapping == null)
        {
            buttonNumberMapping = GetComponent<ButtonNumberMapping>();
            if (buttonNumberMapping == null)
            {
                buttonNumberMapping = gameObject.AddComponent<ButtonNumberMapping>();
            }
        }

        buttonNumberMapping.RebuildMappingDictionaries();
    }

    private void FindAllReferences()
    {
        SetupGameManagerReferences();
    }

    private void SetupGameManagerReferences()
    {
        GameObject[] allButtons = GameObject.FindGameObjectsWithTag("Button");

        foreach (GameObject btn in allButtons)
        {
            BasicButton basicButton = btn.GetComponent<BasicButton>();
            if (basicButton != null)
            {
                basicButton.gameManager = this;
            }
        }

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
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        currentTries = maxTries;
        currentProgress = 0;
        gameOver = false;
        currentTryFailed = false;
        currentTryWrongButtons = 0;
        wrongButtonsPerTry.Clear();

        if (totalButtonsToMatch == 0)
        {
            int buttonCount = 0;

            if (buttonNumberMapping != null)
            {
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

            if (buttonCount == 0)
            {
                GameObject[] taggedButtons = GameObject.FindGameObjectsWithTag("Button");
                buttonCount = taggedButtons.Length;
            }

            totalButtonsToMatch = buttonCount;
        }

        ForceGameStateReset = false;

        StartCoroutine(DelayedGameStateCheck());

        if (winPanel == null) Debug.LogWarning("winPanel is not assigned in the Inspector!");
        if (losePanel == null) Debug.LogWarning("losePanel is not assigned in the Inspector!");
        if (analyticsPanel == null) Debug.LogWarning("analyticsPanel is not assigned in the Inspector!");
        if (tryAnalyticsPanel == null) Debug.LogWarning("tryAnalyticsPanel is not assigned in the Inspector!");
        if (tryAnalyticsText == null) Debug.LogWarning("tryAnalyticsText is not assigned in the Inspector!");

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (tryAnalyticsPanel != null) tryAnalyticsPanel.SetActive(false);

        greenMaterial = new Material(Shader.Find("Standard"));
        greenMaterial.color = Color.green;
        greenMaterial.EnableKeyword("_EMISSION");
        greenMaterial.SetColor("_EmissionColor", Color.green * 2f);

        redMaterial = new Material(Shader.Find("Standard"));
        redMaterial.color = Color.red;
        redMaterial.EnableKeyword("_EMISSION");
        redMaterial.SetColor("_EmissionColor", Color.red * 2f);

        string playerName = "Player";
        if (!string.IsNullOrEmpty(PlayerNameAndInstructions.PlayerName))
        {
            playerName = PlayerNameAndInstructions.PlayerName;
        }

        if (instructionText != null)
        {
            instructionText.text = "";
        }

        SetupButtonListeners();

        UpdateProgressText();
        UpdateTriesText();
    }

    public void SetupButtonListeners()
    {
        if (analyticsButton != null)
        {
            analyticsButton.onClick.RemoveAllListeners();
            analyticsButton.onClick.AddListener(ShowAnalyticsPanel);
        }

        if (winAnalyticsButton != null)
        {
            winAnalyticsButton.onClick.RemoveAllListeners();
            winAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
        }
        else
        {
            Debug.LogWarning("Win analytics button is null!");
        }

        if (loseAnalyticsButton != null)
        {
            loseAnalyticsButton.onClick.RemoveAllListeners();
            loseAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
        }
        else
        {
            Debug.LogWarning("Lose analytics button is null!");
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }

        if (winRetryButton != null)
        {
            winRetryButton.onClick.RemoveAllListeners();
            winRetryButton.onClick.AddListener(RetryGame);
        }
        else
        {
            Debug.LogWarning("Win retry button is null!");
        }

        if (winMainMenuButton != null)
        {
            winMainMenuButton.onClick.RemoveAllListeners();
            winMainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (winExitButton != null)
        {
            winExitButton.onClick.RemoveAllListeners();
            winExitButton.onClick.AddListener(ExitGame);
        }

        if (analyticsPanel != null)
        {
            Button[] analyticsButtons = analyticsPanel.GetComponentsInChildren<Button>(true);
            foreach (Button button in analyticsButtons)
            {
                if (button.name.ToLower().Contains("close") || button.name.ToLower().Contains("back"))
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(HideAnalyticsPanel);
                }
            }
        }
    }

    private void FinishCurrentTry()
    {
        int correctButtons = totalButtonsToMatch - currentTryWrongButtons;

        wrongButtonsPerTry.Add(currentTryWrongButtons);

        currentTries--;
        UpdateTriesText();

        ShowTryAnalyticsPanel(correctButtons, currentTryWrongButtons);

        if (currentTries <= 0)
        {
            StartCoroutine(ShowGameOverAfterAnalytics());
            return;
        }
    }

    private IEnumerator ShowGameOverAfterAnalytics()
    {
        yield return new WaitForSeconds(tryAnalyticsDuration);

        GameLost();
    }

    private void ShowTryAnalyticsPanel(int correctButtons, int wrongButtons)
    {
        if (tryAnalyticsPanel != null)
        {
            if (tryAnalyticsText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"<align=center><size=40><b>TRY {maxTries - currentTries} RESULTS</b></size></align>");
                sb.AppendLine();
                sb.AppendLine($"<color=green><b>CORRECT MATCHES:</b> {correctButtons}/{totalButtonsToMatch}</color>");
                sb.AppendLine($"<color=red><b>WRONG MATCHES:</b> {wrongButtons}/{totalButtonsToMatch}</color>");

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

            tryAnalyticsPanel.SetActive(true);

            StartCoroutine(HideTryAnalyticsPanelAfterDelay());
        }
        else
        {
            Debug.LogError("tryAnalyticsPanel is null! Check the Inspector assignment");
            StartCoroutine(StartNextTryAfterDelay(tryAnalyticsDuration));
        }
    }

    private IEnumerator HideTryAnalyticsPanelAfterDelay()
    {
        yield return new WaitForSeconds(tryAnalyticsDuration);

        if (tryAnalyticsPanel != null)
        {
            tryAnalyticsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("tryAnalyticsPanel is null when trying to hide it!");
        }

        StartNextTry();
    }

    private IEnumerator StartNextTryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextTry();
    }

    private void StartNextTry()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (tryAnalyticsPanel != null) tryAnalyticsPanel.SetActive(false);

        gameOver = false;
        ForceGameStateReset = false;
        currentTryFailed = false;
        currentTryWrongButtons = 0;
        currentProgress = 0;

        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
            selectedButton = null;
        }

        List<GameObject> buttonList = new List<GameObject>();

        if (buttonNumberMapping != null)
        {
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
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.material = defaultMaterial;
                        originalNumberMaterials[numberObject] = defaultMaterial;
                    }
                }
            }
        }

        GameObject[] allLampBases = GameObject.FindGameObjectsWithTag("LampBase");
        foreach (GameObject lampBase in allLampBases)
        {
            if (lampBase != null)
            {
                ResetLampBaseChild(lampBase);
            }
        }

        lockedButtons.Clear();
        lockedLampBases.Clear();
        correctlyMatchedButtons.Clear();

        UpdateProgressText();
        UpdateTriesText();
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"Progress: {currentProgress}/{totalButtonsToMatch}";
        }
    }

    private void UpdateTriesText()
    {
        if (triesText != null)
        {
            triesText.text = $"Tries Remaining: {currentTries}/{maxTries}";
        }
    }

    private int GetCorrectNumberForButton(GameObject button)
    {
        if (button == null) return -1;

        if (buttonNumberMapping != null)
        {
            int numberValue = buttonNumberMapping.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && demoManager.buttonNumberMapping != null)
        {
            int numberValue = demoManager.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        Debug.LogWarning($"Could not determine lamp number for button: {button.name}. Please define mapping in the Inspector.");

        return -1;
    }

    private GameObject GetLampBaseByNumber(int number)
    {
        if (number < 1 || number > 10)
        {
            Debug.LogWarning($"Invalid lamp number: {number}. Must be between 1 and 10.");
            return null;
        }

        if (buttonNumberMapping != null)
        {
            foreach (var pair in buttonNumberMapping.buttonNumberPairs)
            {
                if (pair.numberValue == number && pair.numberObject != null)
                {
                    Transform parent = pair.numberObject.transform.parent;
                    if (parent != null)
                    {
                        return parent.gameObject;
                    }
                    else
                    {
                        return pair.numberObject;
                    }
                }
            }
        }

        GameObject[] lampBases = GameObject.FindGameObjectsWithTag("LampBase");
        foreach (GameObject lampBase in lampBases)
        {
            if (lampBase.name.Contains(number.ToString()))
            {
                return lampBase;
            }
        }

        Debug.LogWarning($"No lamp base found for number {number}. Please define it in the Inspector.");
        return null;
    }

    private GameObject GetNumberObjectFromLampBase(GameObject lampBase)
    {
        if (lampBase == null) return null;

        BasicNumber basicNumber = lampBase.GetComponent<BasicNumber>();
        if (basicNumber != null && basicNumber.numberObject != null)
        {
            return basicNumber.numberObject;
        }

        Transform[] children = lampBase.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.gameObject != lampBase && (child.name.Contains("Number") || child.name.Contains("number")))
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"Could not find number object in lamp base: {lampBase.name}");
        return null;
    }

    public void ButtonClicked(GameObject button)
    {
        if (gameOver || ForceGameStateReset)
        {
            return;
        }

        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && (demoManager.IsDemoRunning() || demoManager.IsShowingPostDemoMessage()))
        {
            return;
        }

        if (lockedButtons.Contains(button))
        {
            return;
        }

        if (selectedButton != null)
        {
            ResetButtonAppearance(selectedButton);
        }

        selectedButton = button;

        HighlightButton(button);
    }

    public void NumberClicked(GameObject lampBase, GameObject numberObject, int numberValue)
    {
        if (gameOver || ForceGameStateReset)
        {
            return;
        }

        SimpleDemoManager demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager != null && (demoManager.IsDemoRunning() || demoManager.IsShowingPostDemoMessage()))
        {
            return;
        }

        if (lockedLampBases.Contains(lampBase))
        {
            return;
        }

        if (selectedButton == null)
        {
            return;
        }

        bool isCorrect = IsCorrectMatch(selectedButton, numberValue);

        if (isCorrect)
        {
            LockButtonSuccess(selectedButton);
            LockLampSuccess(lampBase, numberObject);

            lockedButtons.Add(selectedButton);
            lockedLampBases.Add(lampBase);

            correctlyMatchedButtons.Add(selectedButton);

            currentProgress = correctlyMatchedButtons.Count;
            UpdateProgressText();

            if (lockedButtons.Count >= totalButtonsToMatch)
            {
                if (!currentTryFailed && currentProgress >= totalButtonsToMatch)
                {
                    GameWon();
                }
                else
                {
                    FinishCurrentTry();
                }
            }
        }
        else
        {
            LockButtonFailure(selectedButton);
            LockLampFailure(lampBase, numberObject);

            lockedButtons.Add(selectedButton);
            lockedLampBases.Add(lampBase);

            currentTryFailed = true;

            currentTryWrongButtons++;

            if (lockedButtons.Count >= totalButtonsToMatch)
            {
                FinishCurrentTry();
            }
        }

        selectedButton = null;
    }

    private bool IsCorrectMatch(GameObject button, int numberValue)
    {
        if (button == null) return false;

        if (buttonNumberMapping != null)
        {
            bool isMatch = buttonNumberMapping.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

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

    private void HighlightButton(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalButtonMaterial = renderer.material;

            Material highlightMaterial = new Material(originalButtonMaterial);
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", Color.cyan * 2f);

            renderer.material = highlightMaterial;
        }

        Animator buttonAnimator = button.GetComponent<Animator>();
        if (buttonAnimator != null)
        {
            try
            {
                buttonAnimator.Play("switch", 0, 0f);
            }
            catch (System.Exception)
            {
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

    private void ResetButtonAppearance(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null && originalButtonMaterial != null)
        {
            renderer.material = originalButtonMaterial;
        }
    }

    private void LockButtonSuccess(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (originalButtonMaterial == null)
            {
                originalButtonMaterial = renderer.material;
            }

            renderer.material = greenMaterial;
        }

        Animator buttonAnimator = button.GetComponent<Animator>();
        if (buttonAnimator != null)
        {
            try
            {
                buttonAnimator.Play("switch", 0, 0f);
            }
            catch (System.Exception)
            {
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

        AudioSource audioSource = button.GetComponent<AudioSource>();
        BasicButton basicButton = button.GetComponent<BasicButton>();
        if (audioSource != null && basicButton != null && basicButton.buttonClickSound != null)
        {
            audioSource.PlayOneShot(basicButton.buttonClickSound, basicButton.volume);
        }
    }

    private void LockButtonFailure(GameObject button)
    {
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (originalButtonMaterial == null)
            {
                originalButtonMaterial = renderer.material;
            }

            renderer.material = redMaterial;
        }

        Animator buttonAnimator = button.GetComponent<Animator>();
        if (buttonAnimator != null)
        {
            try
            {
                buttonAnimator.Play("switch", 0, 0f);
            }
            catch (System.Exception)
            {
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

        AudioSource audioSource = button.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            BasicButton basicButton = button.GetComponent<BasicButton>();
            if (basicButton != null && basicButton.buttonClickSound != null)
            {
                audioSource.PlayOneShot(basicButton.buttonClickSound, basicButton.volume);
            }
        }
    }

    private void LockLampSuccess(GameObject lampBase, GameObject numberObject)
    {
        if (numberObject == null)
        {
            return;
        }

        Renderer renderer = numberObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalNumberMaterials.ContainsKey(numberObject))
            {
                originalNumberMaterials[numberObject] = renderer.material;
            }

            renderer.material = greenMaterial;
        }
    }

    private void LockLampFailure(GameObject lampBase, GameObject numberObject)
    {
        if (numberObject == null)
        {
            return;
        }

        Renderer renderer = numberObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalNumberMaterials.ContainsKey(numberObject))
            {
                originalNumberMaterials[numberObject] = renderer.material;
            }

            renderer.material = redMaterial;
        }
    }

    private void GameWon()
    {
        gameOver = true;

        wrongButtonsPerTry.Add(0);

        if (winPanel != null)
        {
            winPanel.SetActive(true);

            if (winAnalyticsButton != null)
            {
                winAnalyticsButton.onClick.RemoveAllListeners();
                winAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
            }

            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound, soundVolume);
            }
        }

        DisablePlayerMovement();

        UpdateAnalyticsText();
    }

    private void GameLost()
    {
        gameOver = true;

        if (losePanel != null)
        {
            losePanel.SetActive(true);

            if (loseAnalyticsButton != null)
            {
                loseAnalyticsButton.onClick.RemoveAllListeners();
                loseAnalyticsButton.onClick.AddListener(ShowAnalyticsPanel);
            }

            if (audioSource != null && loseSound != null)
            {
                audioSource.PlayOneShot(loseSound, soundVolume);
            }
        }

        DisablePlayerMovement();

        UpdateAnalyticsText();
    }

    public void ShowAnalyticsPanel()
    {
        if (analyticsPanel == null)
        {
            GameObject[] panels = GameObject.FindGameObjectsWithTag("UIPanel");
            foreach (GameObject panel in panels)
            {
                if (panel.name.ToLower().Contains("analytics"))
                {
                    analyticsPanel = panel;
                    break;
                }
            }

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

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        analyticsPanel.SetActive(true);

        if (escInstructionText == null)
        {
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

            if (escInstructionText == null)
            {
                GameObject escTextObj = new GameObject("EscInstructionText");
                escTextObj.transform.SetParent(analyticsPanel.transform, false);

                escInstructionText = escTextObj.AddComponent<TMPro.TextMeshProUGUI>();

                RectTransform rectTransform = escTextObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0);
                rectTransform.anchorMax = new Vector2(0.5f, 0);
                rectTransform.pivot = new Vector2(0.5f, 0);
                rectTransform.anchoredPosition = new Vector2(0, 20);
                rectTransform.sizeDelta = new Vector2(400, 50);
            }
        }

        if (escInstructionText != null)
        {
            escInstructionText.text = "<color=yellow><size=24>Press ESC to go back</size></color>";
            escInstructionText.alignment = TMPro.TextAlignmentOptions.Center;
            escInstructionText.fontSize = 24;
            escInstructionText.color = Color.yellow;
            escInstructionText.gameObject.SetActive(true);
        }

        if (analyticsText == null)
        {
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                TextMeshProUGUI[] allTexts = analyticsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (allTexts.Length > 0)
                {
                    analyticsText = allTexts[0];
                }
            }
        }

        AnalyticsTextFixer fixer = analyticsPanel.GetComponent<AnalyticsTextFixer>();
        if (fixer == null)
        {
            fixer = analyticsPanel.AddComponent<AnalyticsTextFixer>();
        }

        if (analyticsText != null)
        {
            fixer.analyticsText = analyticsText;
        }

        if (analyticsText != null)
        {
            int totalWrongButtons = 0;
            int totalCorrectButtons = 0;
            int totalButtonsAttempted = 0;

            foreach (int wrong in wrongButtonsPerTry)
            {
                totalWrongButtons += wrong;
                totalCorrectButtons += (totalButtonsToMatch - wrong);
                totalButtonsAttempted += totalButtonsToMatch;
            }

            if (!gameOver && currentTryWrongButtons > 0 && lockedButtons.Count < totalButtonsToMatch)
            {
                totalWrongButtons += currentTryWrongButtons;
                totalCorrectButtons += correctlyMatchedButtons.Count;
                totalButtonsAttempted += lockedButtons.Count;
            }

            float successRate = 0;
            if (totalButtonsAttempted > 0)
            {
                successRate = (float)totalCorrectButtons / totalButtonsAttempted * 100f;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("<size=36>Game Analytics</size>");
            sb.AppendLine();

            sb.AppendLine("<color=#FF9900><size=20><b>PER-TRY RESULTS:</b></size></color>");
            sb.AppendLine();

            for (int i = 0; i < wrongButtonsPerTry.Count; i++)
            {
                int correctButtons = totalButtonsToMatch - wrongButtonsPerTry[i];
                sb.AppendLine($"Try {i + 1}: <color=green>{correctButtons} correct</color>, <color=red>{wrongButtonsPerTry[i]} wrong</color>");
            }

            sb.AppendLine();
            sb.AppendLine("* * * * * * * * * * * * * * *");
            sb.AppendLine();

            sb.AppendLine("<color=#FF9900><size=20><b>GAME SUMMARY:</b></size></color>");
            sb.AppendLine();
            sb.AppendLine($"Total Buttons Attempted: {totalButtonsAttempted}");
            sb.AppendLine($"Total Correct Matches: <color=green>{totalCorrectButtons}</color>");
            sb.AppendLine($"Total Wrong Matches: <color=red>{totalWrongButtons}</color>");
            sb.AppendLine($"Success Rate: <color=yellow>{successRate:F1}%</color>");
            sb.AppendLine($"Tries Used: {maxTries - currentTries}/{maxTries}");

            analyticsText.text = sb.ToString();
        }
        else
        {
            Debug.LogError("Could not find or create analytics text component!");
        }
    }

    public void HideAnalyticsPanel()
    {
        if (analyticsPanel != null)
        {
            analyticsPanel.SetActive(false);

            if (gameOver)
            {
                if (currentTries <= 0)
                {
                    if (losePanel != null)
                    {
                        losePanel.SetActive(true);
                    }
                }
                else
                {
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

    public void ToggleAnalyticsPanel()
    {
        if (analyticsPanel != null)
        {
            bool isActive = analyticsPanel.activeSelf;

            if (!isActive)
            {
                ShowAnalyticsPanel();
            }
            else
            {
                HideAnalyticsPanel();
            }
        }
        else
        {
            Debug.LogError("Analytics panel is null!");
        }
    }

    private void UpdateAnalyticsText()
    {
        if (analyticsText == null && analyticsPanel != null)
        {
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                analyticsText = analyticsTextObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                analyticsText = analyticsPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (analyticsText == null)
                {
                    Debug.LogError("Could not find analytics text in panel!");
                    return;
                }
            }

            AnalyticsTextFixer fixer = analyticsPanel.GetComponent<AnalyticsTextFixer>();
            if (fixer == null)
            {
                fixer = analyticsPanel.AddComponent<AnalyticsTextFixer>();
            }

            fixer.analyticsText = analyticsText;

            fixer.Start();
        }

        if (analyticsText != null)
        {
            int totalWrongButtons = 0;
            int totalCorrectButtons = 0;
            int totalButtonsAttempted = 0;

            foreach (int wrong in wrongButtonsPerTry)
            {
                totalWrongButtons += wrong;
                totalCorrectButtons += (totalButtonsToMatch - wrong);
                totalButtonsAttempted += totalButtonsToMatch;
            }

            if (!gameOver && currentTryWrongButtons > 0 && lockedButtons.Count < totalButtonsToMatch)
            {
                totalWrongButtons += currentTryWrongButtons;
                totalCorrectButtons += correctlyMatchedButtons.Count;
                totalButtonsAttempted += lockedButtons.Count;
            }

            float successRate = 0;
            if (totalButtonsAttempted > 0)
            {
                successRate = (float)totalCorrectButtons / totalButtonsAttempted * 100f;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("<size=36>Game Analytics</size>");
            sb.AppendLine();

            sb.AppendLine("<color=#FF9900><size=20><b>PER-TRY RESULTS:</b></size></color>");
            sb.AppendLine();

            for (int i = 0; i < wrongButtonsPerTry.Count; i++)
            {
                int correctButtons = totalButtonsToMatch - wrongButtonsPerTry[i];
                sb.AppendLine($"Try {i + 1}: <color=green>{correctButtons} correct</color>, <color=red>{wrongButtonsPerTry[i]} wrong</color>");
            }

            if (!gameOver && currentTryWrongButtons > 0 && lockedButtons.Count < totalButtonsToMatch)
            {
                int currentCorrectButtons = correctlyMatchedButtons.Count;
                sb.AppendLine($"Current Try {wrongButtonsPerTry.Count + 1}: <color=green>{currentCorrectButtons} correct</color>, <color=red>{currentTryWrongButtons} wrong</color> so far");
            }

            sb.AppendLine();
            sb.AppendLine("* * * * * * * * * * * * * * *");
            sb.AppendLine();

            sb.AppendLine("<color=#FF9900><size=20><b>GAME SUMMARY:</b></size></color>");
            sb.AppendLine();

            sb.AppendLine($"Total Buttons Attempted: {totalButtonsAttempted}");
            sb.AppendLine($"Total Correct Matches: <color=green>{totalCorrectButtons}</color>");
            sb.AppendLine($"Total Wrong Matches: <color=red>{totalWrongButtons}</color>");
            sb.AppendLine($"Success Rate: <color=yellow>{successRate:F1}%</color>");
            sb.AppendLine($"Tries Used: {maxTries - currentTries}/{maxTries}");
            sb.AppendLine();

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

            analyticsText.text = sb.ToString();
        }
        else
        {
            Debug.LogError("Analytics text is null, cannot update!");
        }
    }

    private void RetryGame()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SimpleDemoManager.IsGameRestarting = true;
        ForceGameStateReset = true;

        UpdateAnalyticsData();

        string currentSceneName = SceneManager.GetActiveScene().name;

        SceneManager.LoadScene(currentSceneName);
    }

    private void UpdateAnalyticsData()
    {
        if (analyticsData == null)
        {
            analyticsData = new List<string>();
        }

        analyticsData.Add($"Game Session: {Time.time}");
        analyticsData.Add($"Tries Used: {maxTries - currentTries}/{maxTries}");

        for (int i = 0; i < wrongButtonsPerTry.Count; i++)
        {
            int tryNumber = i + 1;
            int wrongButtons = wrongButtonsPerTry[i];
            int correctButtons = totalButtonsToMatch - wrongButtons;
            analyticsData.Add($"Try {tryNumber}: {correctButtons} correct, {wrongButtons} wrong");
        }

        if (currentTryWrongButtons > 0 || currentProgress > 0)
        {
            int currentCorrectButtons = currentProgress - currentTryWrongButtons;
            if (currentCorrectButtons < 0) currentCorrectButtons = 0;

            analyticsData.Add($"Current Try {wrongButtonsPerTry.Count + 1}: {currentCorrectButtons} correct, {currentTryWrongButtons} wrong");
        }
    }

    private void GoToMainMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene("MainMenu");
    }

    private void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void DisablePlayerMovement()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void EnablePlayerMovement()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    private IEnumerator DelayedGameStateCheck()
    {
        yield return new WaitForSeconds(5f);

        if (gameOver)
        {
            gameOver = false;
        }

        if (ForceGameStateReset)
        {
            ForceGameStateReset = false;
        }
    }

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
