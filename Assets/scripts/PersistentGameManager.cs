using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class PersistentGameManager : MonoBehaviour
{
    // Singleton instance
    public static PersistentGameManager Instance { get; private set; }

    [Header("Cursor Settings")]
    [Tooltip("Reference to the custom cursor component")]
    public CustomCursor customCursor;

    [Tooltip("Cursor texture to use")]
    public Texture2D cursorTexture;

    [Tooltip("Hotspot (click point) of the cursor")]
    public Vector2 hotSpot = new Vector2(16, 16);

    // Track if we've already initialized
    private bool isInitialized = false;
    private bool isSetupComplete = false;
    private float setupStartTime = 0f;
    private bool isReloadingScene = false;

    // Track game state
    private bool gameStateReset = true;
    private int gameSessionCount = 0;

    // Scene names
    private string mainMenuSceneName = "MainMenu";
    private string gameSceneName = "PuzzleGame";

    // Reference management
    [Header("Reference Management")]
    [Tooltip("Enable to save and restore references between scene changes")]
    public bool preserveReferences = true;

    // Cache for game object references
    private Dictionary<string, GameObject> gameObjectCache = new Dictionary<string, GameObject>();

    // Cache for component references
    private Dictionary<string, Component> componentCache = new Dictionary<string, Component>();

    // Reference to the main game manager
    private BasicButtonLampGame gameManager;

    // Reference to the demo manager
    private SimpleDemoManager demoManager;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize cursor
            InitializeCursor();

            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // If an instance already exists, destroy this one
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeCursor()
    {
        if (isInitialized)
            return;

        // Initialize custom cursor if not already present
        if (customCursor == null)
        {
            // Check if there's already a CustomCursor component in the scene
            customCursor = FindObjectOfType<CustomCursor>();

            // If not found, add one to this GameObject
            if (customCursor == null && gameObject != null)
            {
                customCursor = gameObject.AddComponent<CustomCursor>();
                Debug.Log("Added CustomCursor component to " + gameObject.name);
            }
        }

        // Set cursor texture and hotspot
        if (customCursor != null && cursorTexture != null)
        {
            customCursor.cursorTexture = cursorTexture;
            customCursor.hotSpot = hotSpot;
            customCursor.useCustomCursor = true;

            // Apply the cursor immediately
            customCursor.SetCustomCursor();
            Debug.Log("Custom cursor initialized");
        }
        else if (cursorTexture != null)
        {
            // If no CustomCursor component but we have a texture, set it directly
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            Debug.Log("Custom cursor set directly");
        }

        isInitialized = true;
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded: " + scene.name);

        // Ensure cursor is visible and using our custom texture
        if (customCursor != null)
        {
            customCursor.SetCustomCursor();
        }
        else if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // If we're loading the game scene
        if (scene.name == gameSceneName)
        {
            // Wait a frame to ensure all objects are initialized
            // Use a longer delay to ensure everything is fully loaded
            StartCoroutine(DelayedSceneSetup(scene.name));
        }

        // If we're loading the main menu, mark that we need to reset game state
        if (scene.name == mainMenuSceneName)
        {
            gameStateReset = false;
            Debug.Log("Returned to main menu, game state will be reset on next game start");

            // Save references before leaving the game scene
            if (preserveReferences && gameManager != null)
            {
                SaveAllReferences();
            }
        }
    }

    // Called when the application is quitting
    private void OnApplicationQuit()
    {
        // Save references when the application quits
        if (preserveReferences && gameManager != null)
        {
            SaveAllReferences();
        }
    }

    // Delayed setup to ensure all objects are initialized
    private IEnumerator DelayedSceneSetup(string sceneName)
    {
        Debug.Log($"DelayedSceneSetup for scene: {sceneName}");

        // Wait for the end of the frame to ensure all objects are initialized
        yield return new WaitForEndOfFrame();

        // Wait a bit longer to ensure everything is fully loaded
        yield return new WaitForSeconds(0.5f);  // Increased delay for better stability

        // If we're reloading the scene
        if (isReloadingScene && sceneName == gameSceneName)
        {
            Debug.Log("Reloading game scene - will find managers and restore state");

            // Find game managers
            FindGameManagers();

            // Restore references if enabled
            if (preserveReferences)
            {
                RestoreAllReferences();
            }

            // Restore game state from PlayerPrefs
            RestoreGameState();

            // Reset the reloading flag
            isReloadingScene = false;

            Debug.Log("Scene reload complete");
        }
        // If we're loading the game scene from the main menu, reset the game state
        else if (sceneName == gameSceneName && !gameStateReset)
        {
            Debug.Log("Loading game scene from main menu - will restore references and reset game state");

            // Find game managers
            FindGameManagers();

            // Restore references if enabled
            if (preserveReferences)
            {
                RestoreAllReferences();
            }

            // Reset game state
            ResetGameState();

            // Increment game session count
            gameSessionCount++;
            Debug.Log("Starting new game session #" + gameSessionCount);
        }
        else if (sceneName == gameSceneName)
        {
            Debug.Log("Loading game scene directly - will find managers and cache references");

            // Find game managers
            FindGameManagers();

            // If this is the first time loading the game scene, cache references
            if (preserveReferences && (objectPaths.Count == 0 || objectNames.Count == 0))
            {
                Debug.Log("First time loading game scene - caching initial references");
                CacheInitialReferences();
            }
        }
    }

    // Public method to load the game scene
    public void LoadGameScene()
    {
        Debug.Log("Loading game scene");
        SceneManager.LoadScene(gameSceneName);
    }

    // Public method to load the main menu scene
    public void LoadMainMenuScene()
    {
        Debug.Log("Loading main menu scene");

        // Save references before leaving the game scene
        if (preserveReferences && gameManager != null)
        {
            SaveAllReferences();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Find game managers in the scene
    private void FindGameManagers()
    {
        Debug.Log("Finding game managers in scene");

        // Find the game manager
        gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager == null)
        {
            Debug.LogWarning("No BasicButtonLampGame found in the scene!");

            // Try to find by name as a fallback
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj != null)
            {
                gameManager = gameManagerObj.GetComponent<BasicButtonLampGame>();
                if (gameManager != null)
                {
                    Debug.Log("Found BasicButtonLampGame by name: " + gameManagerObj.name);
                }
            }
        }
        else
        {
            Debug.Log("Found BasicButtonLampGame: " + gameManager.name);

            // Check if this is a new instance or the same one
            if (lastGameManagerInstanceID != 0 && lastGameManagerInstanceID != gameManager.GetInstanceID())
            {
                Debug.Log($"New game manager instance detected. Old ID: {lastGameManagerInstanceID}, New ID: {gameManager.GetInstanceID()}");
            }

            // Store the instance ID for future reference
            lastGameManagerInstanceID = gameManager.GetInstanceID();
        }

        // Find the demo manager
        demoManager = FindObjectOfType<SimpleDemoManager>();
        if (demoManager == null)
        {
            Debug.LogWarning("No SimpleDemoManager found in the scene!");

            // Try to find by name as a fallback
            GameObject demoManagerObj = GameObject.Find("DemoManager");
            if (demoManagerObj != null)
            {
                demoManager = demoManagerObj.GetComponent<SimpleDemoManager>();
                if (demoManager != null)
                {
                    Debug.Log("Found SimpleDemoManager by name: " + demoManagerObj.name);
                }
            }
        }
        else
        {
            Debug.Log("Found SimpleDemoManager: " + demoManager.name);
        }

        // If we found a game manager but some references are null, try to find them
        if (gameManager != null)
        {
            // Check if any critical references are null
            bool hasNullReferences = false;

            // Check button references
            if (gameManager.button1 == null || gameManager.button2 == null || gameManager.button3 == null ||
                gameManager.button4 == null || gameManager.button5 == null)
            {
                hasNullReferences = true;
                Debug.LogWarning("Some button references are null in the game manager");
            }

            // Check lamp base references
            if (gameManager.lampBase1 == null || gameManager.lampBase2 == null || gameManager.lampBase3 == null ||
                gameManager.lampBase4 == null || gameManager.lampBase5 == null || gameManager.lampBase6 == null ||
                gameManager.lampBase7 == null || gameManager.lampBase8 == null || gameManager.lampBase9 == null ||
                gameManager.lampBase10 == null)
            {
                hasNullReferences = true;
                Debug.LogWarning("Some lamp base references are null in the game manager");
            }

            // If we have null references, try to find them directly
            if (hasNullReferences)
            {
                Debug.Log("Attempting to find null references directly...");
                FindMissingReferences();
            }
        }
    }

    // Instance ID of the last game manager we found
    private int lastGameManagerInstanceID = 0;

    // Cache initial references from the game manager
    private void CacheInitialReferences()
    {
        Debug.Log("Caching initial references from game manager");

        if (gameManager == null)
        {
            Debug.LogError("Cannot cache references: gameManager is null!");
            return;
        }

        // Store the hierarchy paths instead of direct references
        // This is more reliable across scene changes

        // Cache button references by storing their full paths
        CacheObjectPath("button1", gameManager.button1);
        CacheObjectPath("button2", gameManager.button2);
        CacheObjectPath("button3", gameManager.button3);
        CacheObjectPath("button4", gameManager.button4);
        CacheObjectPath("button5", gameManager.button5);

        // Cache lamp base references
        CacheObjectPath("lampBase1", gameManager.lampBase1);
        CacheObjectPath("lampBase2", gameManager.lampBase2);
        CacheObjectPath("lampBase3", gameManager.lampBase3);
        CacheObjectPath("lampBase4", gameManager.lampBase4);
        CacheObjectPath("lampBase5", gameManager.lampBase5);
        CacheObjectPath("lampBase6", gameManager.lampBase6);
        CacheObjectPath("lampBase7", gameManager.lampBase7);
        CacheObjectPath("lampBase8", gameManager.lampBase8);
        CacheObjectPath("lampBase9", gameManager.lampBase9);
        CacheObjectPath("lampBase10", gameManager.lampBase10);

        // Cache UI references
        CacheObjectPath("instructionText", gameManager.instructionText != null ? gameManager.instructionText.gameObject : null);
        CacheObjectPath("progressText", gameManager.progressText != null ? gameManager.progressText.gameObject : null);
        CacheObjectPath("triesText", gameManager.triesText != null ? gameManager.triesText.gameObject : null);
        CacheObjectPath("winPanel", gameManager.winPanel);
        CacheObjectPath("losePanel", gameManager.losePanel);

        // Cache analytics UI references
        CacheObjectPath("analyticsButton", gameManager.analyticsButton != null ? gameManager.analyticsButton.gameObject : null);
        CacheObjectPath("winAnalyticsButton", gameManager.winAnalyticsButton != null ? gameManager.winAnalyticsButton.gameObject : null);
        CacheObjectPath("loseAnalyticsButton", gameManager.loseAnalyticsButton != null ? gameManager.loseAnalyticsButton.gameObject : null);
        CacheObjectPath("analyticsPanel", gameManager.analyticsPanel);
        CacheObjectPath("analyticsText", gameManager.analyticsText != null ? gameManager.analyticsText.gameObject : null);

        // Cache try analytics UI references
        CacheObjectPath("tryAnalyticsPanel", gameManager.tryAnalyticsPanel);
        CacheObjectPath("tryAnalyticsText", gameManager.tryAnalyticsText != null ? gameManager.tryAnalyticsText.gameObject : null);

        // Cache menu buttons
        CacheObjectPath("retryButton", gameManager.retryButton != null ? gameManager.retryButton.gameObject : null);
        CacheObjectPath("mainMenuButton", gameManager.mainMenuButton != null ? gameManager.mainMenuButton.gameObject : null);
        CacheObjectPath("exitButton", gameManager.exitButton != null ? gameManager.exitButton.gameObject : null);

        // Cache win panel buttons
        CacheObjectPath("winRetryButton", gameManager.winRetryButton != null ? gameManager.winRetryButton.gameObject : null);
        CacheObjectPath("winMainMenuButton", gameManager.winMainMenuButton != null ? gameManager.winMainMenuButton.gameObject : null);
        CacheObjectPath("winExitButton", gameManager.winExitButton != null ? gameManager.winExitButton.gameObject : null);

        // Also store the names for easier lookup
        StoreObjectNames();

        Debug.Log($"Cached {gameObjectCache.Count} GameObjects and {componentCache.Count} Components");
    }

    // Store the names of all objects for easier lookup
    private Dictionary<string, string> objectNames = new Dictionary<string, string>();
    private Dictionary<string, string> objectPaths = new Dictionary<string, string>();

    private void StoreObjectNames()
    {
        objectNames.Clear();

        // Store button names
        StoreObjectName("button1", gameManager.button1);
        StoreObjectName("button2", gameManager.button2);
        StoreObjectName("button3", gameManager.button3);
        StoreObjectName("button4", gameManager.button4);
        StoreObjectName("button5", gameManager.button5);

        // Store lamp base names
        StoreObjectName("lampBase1", gameManager.lampBase1);
        StoreObjectName("lampBase2", gameManager.lampBase2);
        StoreObjectName("lampBase3", gameManager.lampBase3);
        StoreObjectName("lampBase4", gameManager.lampBase4);
        StoreObjectName("lampBase5", gameManager.lampBase5);
        StoreObjectName("lampBase6", gameManager.lampBase6);
        StoreObjectName("lampBase7", gameManager.lampBase7);
        StoreObjectName("lampBase8", gameManager.lampBase8);
        StoreObjectName("lampBase9", gameManager.lampBase9);
        StoreObjectName("lampBase10", gameManager.lampBase10);

        // Store UI names
        StoreObjectName("instructionText", gameManager.instructionText != null ? gameManager.instructionText.gameObject : null);
        StoreObjectName("progressText", gameManager.progressText != null ? gameManager.progressText.gameObject : null);
        StoreObjectName("triesText", gameManager.triesText != null ? gameManager.triesText.gameObject : null);
        StoreObjectName("winPanel", gameManager.winPanel);
        StoreObjectName("losePanel", gameManager.losePanel);

        // Store analytics UI names
        StoreObjectName("analyticsButton", gameManager.analyticsButton != null ? gameManager.analyticsButton.gameObject : null);
        StoreObjectName("winAnalyticsButton", gameManager.winAnalyticsButton != null ? gameManager.winAnalyticsButton.gameObject : null);
        StoreObjectName("loseAnalyticsButton", gameManager.loseAnalyticsButton != null ? gameManager.loseAnalyticsButton.gameObject : null);
        StoreObjectName("analyticsPanel", gameManager.analyticsPanel);
        StoreObjectName("analyticsText", gameManager.analyticsText != null ? gameManager.analyticsText.gameObject : null);

        // Store try analytics UI names
        StoreObjectName("tryAnalyticsPanel", gameManager.tryAnalyticsPanel);
        StoreObjectName("tryAnalyticsText", gameManager.tryAnalyticsText != null ? gameManager.tryAnalyticsText.gameObject : null);

        // Store menu button names
        StoreObjectName("retryButton", gameManager.retryButton != null ? gameManager.retryButton.gameObject : null);
        StoreObjectName("mainMenuButton", gameManager.mainMenuButton != null ? gameManager.mainMenuButton.gameObject : null);
        StoreObjectName("exitButton", gameManager.exitButton != null ? gameManager.exitButton.gameObject : null);

        // Store win panel button names
        StoreObjectName("winRetryButton", gameManager.winRetryButton != null ? gameManager.winRetryButton.gameObject : null);
        StoreObjectName("winMainMenuButton", gameManager.winMainMenuButton != null ? gameManager.winMainMenuButton.gameObject : null);
        StoreObjectName("winExitButton", gameManager.winExitButton != null ? gameManager.winExitButton.gameObject : null);

        Debug.Log($"Stored {objectNames.Count} object names for lookup");
    }

    // Helper method to store an object's name
    private void StoreObjectName(string key, GameObject obj)
    {
        if (obj != null)
        {
            objectNames[key] = obj.name;
            Debug.Log($"Stored object name: {key} = {obj.name}");
        }
    }

    // Helper method to cache an object's full path
    private void CacheObjectPath(string key, GameObject obj)
    {
        if (obj != null)
        {
            string path = GetGameObjectPath(obj);
            objectPaths[key] = path;
            Debug.Log($"Cached object path: {key} = {path}");
        }
    }

    // Get the full path of a GameObject in the hierarchy
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "";

        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Save all references from the current game manager
    private void SaveAllReferences()
    {
        Debug.Log("Saving all references from game manager");

        if (gameManager == null)
        {
            Debug.LogError("Cannot save references: gameManager is null!");
            return;
        }

        // Clear existing caches
        gameObjectCache.Clear();
        componentCache.Clear();
        objectNames.Clear();
        objectPaths.Clear();

        // Cache all references
        CacheInitialReferences();

        // Log the saved references
        Debug.Log($"Saved {objectPaths.Count} object paths and {objectNames.Count} object names");

        // Log some sample paths to verify
        int count = 0;
        foreach (var entry in objectPaths)
        {
            Debug.Log($"Saved path: {entry.Key} = {entry.Value}");
            count++;
            if (count >= 5) break; // Just log a few for brevity
        }
    }

    // Restore all references to the game manager
    private void RestoreAllReferences()
    {
        Debug.Log("Restoring all references to game manager");

        if (gameManager == null)
        {
            Debug.LogError("Cannot restore references: gameManager is null!");
            return;
        }

        // First, try to find objects by their stored paths
        // Restore button references
        gameManager.button1 = FindGameObjectByPath("button1");
        gameManager.button2 = FindGameObjectByPath("button2");
        gameManager.button3 = FindGameObjectByPath("button3");
        gameManager.button4 = FindGameObjectByPath("button4");
        gameManager.button5 = FindGameObjectByPath("button5");

        // Restore lamp base references
        gameManager.lampBase1 = FindGameObjectByPath("lampBase1");
        gameManager.lampBase2 = FindGameObjectByPath("lampBase2");
        gameManager.lampBase3 = FindGameObjectByPath("lampBase3");
        gameManager.lampBase4 = FindGameObjectByPath("lampBase4");
        gameManager.lampBase5 = FindGameObjectByPath("lampBase5");
        gameManager.lampBase6 = FindGameObjectByPath("lampBase6");
        gameManager.lampBase7 = FindGameObjectByPath("lampBase7");
        gameManager.lampBase8 = FindGameObjectByPath("lampBase8");
        gameManager.lampBase9 = FindGameObjectByPath("lampBase9");
        gameManager.lampBase10 = FindGameObjectByPath("lampBase10");

        // Restore UI references
        GameObject instructionTextObj = FindGameObjectByPath("instructionText");
        if (instructionTextObj != null)
            gameManager.instructionText = instructionTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        GameObject progressTextObj = FindGameObjectByPath("progressText");
        if (progressTextObj != null)
            gameManager.progressText = progressTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        GameObject triesTextObj = FindGameObjectByPath("triesText");
        if (triesTextObj != null)
            gameManager.triesText = triesTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        gameManager.winPanel = FindGameObjectByPath("winPanel");
        gameManager.losePanel = FindGameObjectByPath("losePanel");

        // Restore analytics UI references
        GameObject analyticsButtonObj = FindGameObjectByPath("analyticsButton");
        if (analyticsButtonObj != null)
            gameManager.analyticsButton = analyticsButtonObj.GetComponent<UnityEngine.UI.Button>();

        GameObject winAnalyticsButtonObj = FindGameObjectByPath("winAnalyticsButton");
        if (winAnalyticsButtonObj != null)
            gameManager.winAnalyticsButton = winAnalyticsButtonObj.GetComponent<UnityEngine.UI.Button>();

        GameObject loseAnalyticsButtonObj = FindGameObjectByPath("loseAnalyticsButton");
        if (loseAnalyticsButtonObj != null)
            gameManager.loseAnalyticsButton = loseAnalyticsButtonObj.GetComponent<UnityEngine.UI.Button>();

        gameManager.analyticsPanel = FindGameObjectByPath("analyticsPanel");

        GameObject analyticsTextObj = FindGameObjectByPath("analyticsText");
        if (analyticsTextObj != null)
            gameManager.analyticsText = analyticsTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        // Restore try analytics UI references
        gameManager.tryAnalyticsPanel = FindGameObjectByPath("tryAnalyticsPanel");

        GameObject tryAnalyticsTextObj = FindGameObjectByPath("tryAnalyticsText");
        if (tryAnalyticsTextObj != null)
            gameManager.tryAnalyticsText = tryAnalyticsTextObj.GetComponent<TMPro.TextMeshProUGUI>();

        // Restore menu buttons
        GameObject retryButtonObj = FindGameObjectByPath("retryButton");
        if (retryButtonObj != null)
            gameManager.retryButton = retryButtonObj.GetComponent<UnityEngine.UI.Button>();

        GameObject mainMenuButtonObj = FindGameObjectByPath("mainMenuButton");
        if (mainMenuButtonObj != null)
            gameManager.mainMenuButton = mainMenuButtonObj.GetComponent<UnityEngine.UI.Button>();

        GameObject exitButtonObj = FindGameObjectByPath("exitButton");
        if (exitButtonObj != null)
            gameManager.exitButton = exitButtonObj.GetComponent<UnityEngine.UI.Button>();

        // Restore win panel buttons
        GameObject winRetryButtonObj = FindGameObjectByPath("winRetryButton");
        if (winRetryButtonObj != null)
            gameManager.winRetryButton = winRetryButtonObj.GetComponent<UnityEngine.UI.Button>();

        GameObject winMainMenuButtonObj = FindGameObjectByPath("winMainMenuButton");
        if (winMainMenuButtonObj != null)
            gameManager.winMainMenuButton = winMainMenuButtonObj.GetComponent<UnityEngine.UI.Button>();

        GameObject winExitButtonObj = FindGameObjectByPath("winExitButton");
        if (winExitButtonObj != null)
            gameManager.winExitButton = winExitButtonObj.GetComponent<UnityEngine.UI.Button>();

        // Check for any null references and try to find them by name or tag
        FindMissingReferences();

        // Re-setup button listeners
        gameManager.SetupButtonListeners();

        Debug.Log("References restored successfully");
    }

    // Find a GameObject by its stored path
    private GameObject FindGameObjectByPath(string key)
    {
        if (objectPaths.TryGetValue(key, out string path))
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Stored path for {key} is empty");
                return null;
            }

            // Try to find by path
            GameObject obj = FindGameObjectByHierarchyPath(path);
            if (obj != null)
            {
                Debug.Log($"Found GameObject by path: {key} = {path}");
                return obj;
            }

            // If path lookup fails, try by name
            if (objectNames.TryGetValue(key, out string name))
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject go in allObjects)
                {
                    if (go.name == name)
                    {
                        Debug.Log($"Found GameObject by name: {key} = {name}");
                        return go;
                    }
                }

                // Try partial name match
                foreach (GameObject go in allObjects)
                {
                    if (go.name.Contains(name) || name.Contains(go.name))
                    {
                        Debug.Log($"Found GameObject by partial name match: {key} = {go.name} (looking for {name})");
                        return go;
                    }
                }
            }
        }

        // If all else fails, try to find by key as name
        GameObject keyObj = GameObject.Find(key);
        if (keyObj != null)
        {
            Debug.Log($"Found GameObject by key as name: {key}");
            return keyObj;
        }

        Debug.LogWarning($"Failed to find GameObject for key: {key}");
        return null;
    }

    // Find a GameObject by its full hierarchy path
    private GameObject FindGameObjectByHierarchyPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Split the path into parts
        string[] parts = path.Split('/');

        // Start with all root objects
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        // If the first part matches a root object, start there
        GameObject currentObject = null;
        int startIndex = 0;

        foreach (GameObject root in rootObjects)
        {
            if (root.name == parts[0])
            {
                currentObject = root;
                startIndex = 1;
                break;
            }
        }

        // If we didn't find a matching root, return null
        if (currentObject == null && parts.Length > 1)
        {
            return null;
        }
        else if (parts.Length == 1)
        {
            // If it's just a single-part path, try to find it directly
            return GameObject.Find(parts[0]);
        }

        // Navigate down the hierarchy
        for (int i = startIndex; i < parts.Length; i++)
        {
            Transform child = currentObject.transform.Find(parts[i]);
            if (child == null)
            {
                // Try to find by name in the entire scene as a fallback
                return GameObject.Find(parts[parts.Length - 1]);
            }

            currentObject = child.gameObject;
        }

        return currentObject;
    }

    // Find missing references by name or tag
    private void FindMissingReferences()
    {
        Debug.Log("Looking for missing references by name or tag");

        // Try to find buttons by type and name pattern
        if (gameManager.button1 == null || gameManager.button2 == null || gameManager.button3 == null ||
            gameManager.button4 == null || gameManager.button5 == null)
        {
            FindButtonsByPattern();
        }

        // Try to find lamp bases by type and name pattern
        if (gameManager.lampBase1 == null || gameManager.lampBase2 == null || gameManager.lampBase3 == null ||
            gameManager.lampBase4 == null || gameManager.lampBase5 == null || gameManager.lampBase6 == null ||
            gameManager.lampBase7 == null || gameManager.lampBase8 == null || gameManager.lampBase9 == null ||
            gameManager.lampBase10 == null)
        {
            FindLampBasesByPattern();
        }

        // Try to find UI elements by type
        FindMissingUIElements();

        // Try to find buttons by type
        FindMissingButtons();

        // Last resort: try to find any remaining objects by tag
        FindRemainingObjectsByTag();

        // Log the results
        LogReferenceStatus();
    }

    // Find buttons by pattern in their names
    private void FindButtonsByPattern()
    {
        Debug.Log("Searching for buttons by name pattern...");

        // Get all objects with "Button" in their name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> buttonObjects = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Button") && !obj.name.Contains("Analytics") &&
                !obj.name.Contains("Retry") && !obj.name.Contains("Menu") && !obj.name.Contains("Exit"))
            {
                buttonObjects.Add(obj);
                Debug.Log($"Found potential button: {obj.name}");
            }
        }

        // Sort them by name to try to get them in order
        buttonObjects.Sort((a, b) => a.name.CompareTo(b.name));

        // Assign them to the game manager
        if (buttonObjects.Count >= 1 && gameManager.button1 == null)
        {
            gameManager.button1 = buttonObjects[0];
            Debug.Log($"Assigned {buttonObjects[0].name} to button1");
        }

        if (buttonObjects.Count >= 2 && gameManager.button2 == null)
        {
            gameManager.button2 = buttonObjects[1];
            Debug.Log($"Assigned {buttonObjects[1].name} to button2");
        }

        if (buttonObjects.Count >= 3 && gameManager.button3 == null)
        {
            gameManager.button3 = buttonObjects[2];
            Debug.Log($"Assigned {buttonObjects[2].name} to button3");
        }

        if (buttonObjects.Count >= 4 && gameManager.button4 == null)
        {
            gameManager.button4 = buttonObjects[3];
            Debug.Log($"Assigned {buttonObjects[3].name} to button4");
        }

        if (buttonObjects.Count >= 5 && gameManager.button5 == null)
        {
            gameManager.button5 = buttonObjects[4];
            Debug.Log($"Assigned {buttonObjects[4].name} to button5");
        }
    }

    // Find lamp bases by pattern in their names
    private void FindLampBasesByPattern()
    {
        Debug.Log("Searching for lamp bases by name pattern...");

        // Get all objects with "Lamp" in their name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> lampObjects = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Lamp") || obj.name.Contains("lamp"))
            {
                lampObjects.Add(obj);
                Debug.Log($"Found potential lamp base: {obj.name}");
            }
        }

        // Sort them by name to try to get them in order
        lampObjects.Sort((a, b) => a.name.CompareTo(b.name));

        // Assign them to the game manager
        if (lampObjects.Count >= 1 && gameManager.lampBase1 == null)
        {
            gameManager.lampBase1 = lampObjects[0];
            Debug.Log($"Assigned {lampObjects[0].name} to lampBase1");
        }

        if (lampObjects.Count >= 2 && gameManager.lampBase2 == null)
        {
            gameManager.lampBase2 = lampObjects[1];
            Debug.Log($"Assigned {lampObjects[1].name} to lampBase2");
        }

        if (lampObjects.Count >= 3 && gameManager.lampBase3 == null)
        {
            gameManager.lampBase3 = lampObjects[2];
            Debug.Log($"Assigned {lampObjects[2].name} to lampBase3");
        }

        if (lampObjects.Count >= 4 && gameManager.lampBase4 == null)
        {
            gameManager.lampBase4 = lampObjects[3];
            Debug.Log($"Assigned {lampObjects[3].name} to lampBase4");
        }

        if (lampObjects.Count >= 5 && gameManager.lampBase5 == null)
        {
            gameManager.lampBase5 = lampObjects[4];
            Debug.Log($"Assigned {lampObjects[4].name} to lampBase5");
        }

        if (lampObjects.Count >= 6 && gameManager.lampBase6 == null)
        {
            gameManager.lampBase6 = lampObjects[5];
            Debug.Log($"Assigned {lampObjects[5].name} to lampBase6");
        }

        if (lampObjects.Count >= 7 && gameManager.lampBase7 == null)
        {
            gameManager.lampBase7 = lampObjects[6];
            Debug.Log($"Assigned {lampObjects[6].name} to lampBase7");
        }

        if (lampObjects.Count >= 8 && gameManager.lampBase8 == null)
        {
            gameManager.lampBase8 = lampObjects[7];
            Debug.Log($"Assigned {lampObjects[7].name} to lampBase8");
        }

        if (lampObjects.Count >= 9 && gameManager.lampBase9 == null)
        {
            gameManager.lampBase9 = lampObjects[8];
            Debug.Log($"Assigned {lampObjects[8].name} to lampBase9");
        }

        if (lampObjects.Count >= 10 && gameManager.lampBase10 == null)
        {
            gameManager.lampBase10 = lampObjects[9];
            Debug.Log($"Assigned {lampObjects[9].name} to lampBase10");
        }
    }

    // Find missing UI elements
    private void FindMissingUIElements()
    {
        Debug.Log("Searching for missing UI elements...");

        // Find panels by tag first
        FindPanelsByTag();

        // Find text components by tag
        FindTextsByTag();

        // If we still have missing panels, try by name
        if (gameManager.winPanel == null || gameManager.losePanel == null ||
            gameManager.analyticsPanel == null || gameManager.tryAnalyticsPanel == null)
        {
            FindPanelsByName();
        }

        // If we still have missing texts, try by name
        if (gameManager.instructionText == null || gameManager.progressText == null ||
            gameManager.triesText == null || gameManager.analyticsText == null ||
            gameManager.tryAnalyticsText == null)
        {
            FindTextsByName();
        }

        // Last resort: try direct GameObject.Find for specific UI elements
        FindUIElementsByDirectName();
    }

    // Find panels by tag
    private void FindPanelsByTag()
    {
        // Try to find panels by tag
        GameObject[] uiPanels = GameObject.FindGameObjectsWithTag("UIPanel");
        if (uiPanels.Length > 0)
        {
            Debug.Log($"Found {uiPanels.Length} UI panels with UIPanel tag");

            foreach (GameObject panel in uiPanels)
            {
                if (panel.name.Contains("Win") && gameManager.winPanel == null)
                {
                    gameManager.winPanel = panel;
                    Debug.Log($"Found Win Panel by tag: {panel.name}");
                }
                else if (panel.name.Contains("Lose") && gameManager.losePanel == null)
                {
                    gameManager.losePanel = panel;
                    Debug.Log($"Found Lose Panel by tag: {panel.name}");
                }
                else if (panel.name.Contains("Analytics") && !panel.name.Contains("Try") && gameManager.analyticsPanel == null)
                {
                    gameManager.analyticsPanel = panel;
                    Debug.Log($"Found Analytics Panel by tag: {panel.name}");
                }
                else if (panel.name.Contains("Try") && panel.name.Contains("Analytics") && gameManager.tryAnalyticsPanel == null)
                {
                    gameManager.tryAnalyticsPanel = panel;
                    Debug.Log($"Found Try Analytics Panel by tag: {panel.name}");
                }
            }
        }
    }

    // Find text components by tag
    private void FindTextsByTag()
    {
        // Try to find texts by tag
        GameObject[] uiTexts = GameObject.FindGameObjectsWithTag("UIText");
        if (uiTexts.Length > 0)
        {
            Debug.Log($"Found {uiTexts.Length} UI texts with UIText tag");

            foreach (GameObject textObj in uiTexts)
            {
                TMPro.TextMeshProUGUI text = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                if (text == null) continue;

                if ((textObj.name.Contains("Instruction") || textObj.name.Contains("instruction")) && gameManager.instructionText == null)
                {
                    gameManager.instructionText = text;
                    Debug.Log($"Found Instruction Text by tag: {textObj.name}");
                }
                else if ((textObj.name.Contains("Progress") || textObj.name.Contains("progress")) && gameManager.progressText == null)
                {
                    gameManager.progressText = text;
                    Debug.Log($"Found Progress Text by tag: {textObj.name}");
                }
                else if ((textObj.name.Contains("Tries") || textObj.name.Contains("tries")) && gameManager.triesText == null)
                {
                    gameManager.triesText = text;
                    Debug.Log($"Found Tries Text by tag: {textObj.name}");
                }
                else if (textObj.name.Contains("Analytics") && !textObj.name.Contains("Try") && gameManager.analyticsText == null)
                {
                    gameManager.analyticsText = text;
                    Debug.Log($"Found Analytics Text by tag: {textObj.name}");
                }
                else if (textObj.name.Contains("Try") && textObj.name.Contains("Analytics") && gameManager.tryAnalyticsText == null)
                {
                    gameManager.tryAnalyticsText = text;
                    Debug.Log($"Found Try Analytics Text by tag: {textObj.name}");
                }
            }
        }
    }

    // Find panels by name
    private void FindPanelsByName()
    {
        GameObject[] panels = FindObjectsOfType<GameObject>();
        foreach (GameObject panel in panels)
        {
            if (panel.name.Contains("Win") && gameManager.winPanel == null)
            {
                gameManager.winPanel = panel;
                Debug.Log($"Found Win Panel by name: {panel.name}");
            }
            else if (panel.name.Contains("Lose") && gameManager.losePanel == null)
            {
                gameManager.losePanel = panel;
                Debug.Log($"Found Lose Panel by name: {panel.name}");
            }
            else if (panel.name.Contains("Analytics") && !panel.name.Contains("Try") && gameManager.analyticsPanel == null)
            {
                gameManager.analyticsPanel = panel;
                Debug.Log($"Found Analytics Panel by name: {panel.name}");
            }
            else if (panel.name.Contains("Try") && panel.name.Contains("Analytics") && gameManager.tryAnalyticsPanel == null)
            {
                gameManager.tryAnalyticsPanel = panel;
                Debug.Log($"Found Try Analytics Panel by name: {panel.name}");
            }
        }
    }

    // Find text components by name
    private void FindTextsByName()
    {
        TMPro.TextMeshProUGUI[] texts = FindObjectsOfType<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI text in texts)
        {
            if ((text.name.Contains("Instruction") || text.name.Contains("instruction")) && gameManager.instructionText == null)
            {
                gameManager.instructionText = text;
                Debug.Log($"Found Instruction Text by name: {text.name}");
            }
            else if ((text.name.Contains("Progress") || text.name.Contains("progress")) && gameManager.progressText == null)
            {
                gameManager.progressText = text;
                Debug.Log($"Found Progress Text by name: {text.name}");
            }
            else if ((text.name.Contains("Tries") || text.name.Contains("tries")) && gameManager.triesText == null)
            {
                gameManager.triesText = text;
                Debug.Log($"Found Tries Text by name: {text.name}");
            }
            else if (text.name.Contains("Analytics") && !text.name.Contains("Try") && gameManager.analyticsText == null)
            {
                gameManager.analyticsText = text;
                Debug.Log($"Found Analytics Text by name: {text.name}");
            }
            else if (text.name.Contains("Try") && text.name.Contains("Analytics") && gameManager.tryAnalyticsText == null)
            {
                gameManager.tryAnalyticsText = text;
                Debug.Log($"Found Try Analytics Text by name: {text.name}");
            }
        }
    }

    // Find UI elements by direct name
    private void FindUIElementsByDirectName()
    {
        // Try direct GameObject.Find for specific UI elements
        if (gameManager.winPanel == null)
        {
            GameObject winPanel = GameObject.Find("WinPanel");
            if (winPanel != null)
            {
                gameManager.winPanel = winPanel;
                Debug.Log($"Found Win Panel by direct name: {winPanel.name}");
            }
        }

        if (gameManager.losePanel == null)
        {
            GameObject losePanel = GameObject.Find("LosePanel");
            if (losePanel != null)
            {
                gameManager.losePanel = losePanel;
                Debug.Log($"Found Lose Panel by direct name: {losePanel.name}");
            }
        }

        if (gameManager.analyticsPanel == null)
        {
            GameObject analyticsPanel = GameObject.Find("GameAnalyticsPanel");
            if (analyticsPanel != null)
            {
                gameManager.analyticsPanel = analyticsPanel;
                Debug.Log($"Found Analytics Panel by direct name: {analyticsPanel.name}");
            }
        }

        if (gameManager.tryAnalyticsPanel == null)
        {
            GameObject tryAnalyticsPanel = GameObject.Find("TryAnalyticsPanel");
            if (tryAnalyticsPanel != null)
            {
                gameManager.tryAnalyticsPanel = tryAnalyticsPanel;
                Debug.Log($"Found Try Analytics Panel by direct name: {tryAnalyticsPanel.name}");
            }
        }

        // Try to find text components by direct name
        if (gameManager.instructionText == null)
        {
            GameObject instructionTextObj = GameObject.Find("InstructionText");
            if (instructionTextObj != null)
            {
                gameManager.instructionText = instructionTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log($"Found Instruction Text by direct name: {instructionTextObj.name}");
            }
        }

        if (gameManager.progressText == null)
        {
            GameObject progressTextObj = GameObject.Find("progress");
            if (progressTextObj != null)
            {
                gameManager.progressText = progressTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log($"Found Progress Text by direct name: {progressTextObj.name}");
            }
        }

        if (gameManager.triesText == null)
        {
            GameObject triesTextObj = GameObject.Find("Tries");
            if (triesTextObj != null)
            {
                gameManager.triesText = triesTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log($"Found Tries Text by direct name: {triesTextObj.name}");
            }
        }

        if (gameManager.analyticsText == null)
        {
            GameObject analyticsTextObj = GameObject.Find("GameAnalyticsText");
            if (analyticsTextObj != null)
            {
                gameManager.analyticsText = analyticsTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log($"Found Analytics Text by direct name: {analyticsTextObj.name}");
            }
        }

        if (gameManager.tryAnalyticsText == null)
        {
            GameObject tryAnalyticsTextObj = GameObject.Find("TryAnalyticsText");
            if (tryAnalyticsTextObj != null)
            {
                gameManager.tryAnalyticsText = tryAnalyticsTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log($"Found Try Analytics Text by direct name: {tryAnalyticsTextObj.name}");
            }
        }
    }

    // Find missing buttons
    private void FindMissingButtons()
    {
        Debug.Log("Searching for missing buttons...");

        // Find buttons by type
        UnityEngine.UI.Button[] buttons = FindObjectsOfType<UnityEngine.UI.Button>();
        foreach (UnityEngine.UI.Button button in buttons)
        {
            if (button.name.Contains("Retry") && !button.name.Contains("Win") && gameManager.retryButton == null)
            {
                gameManager.retryButton = button;
                Debug.Log($"Found Retry Button: {button.name}");
            }
            else if (button.name.Contains("Win") && button.name.Contains("Retry") && gameManager.winRetryButton == null)
            {
                gameManager.winRetryButton = button;
                Debug.Log($"Found Win Retry Button: {button.name}");
            }
            else if (button.name.Contains("Main") && button.name.Contains("Menu") && !button.name.Contains("Win") && gameManager.mainMenuButton == null)
            {
                gameManager.mainMenuButton = button;
                Debug.Log($"Found Main Menu Button: {button.name}");
            }
            else if (button.name.Contains("Win") && button.name.Contains("Main") && button.name.Contains("Menu") && gameManager.winMainMenuButton == null)
            {
                gameManager.winMainMenuButton = button;
                Debug.Log($"Found Win Main Menu Button: {button.name}");
            }
            else if (button.name.Contains("Exit") && !button.name.Contains("Win") && gameManager.exitButton == null)
            {
                gameManager.exitButton = button;
                Debug.Log($"Found Exit Button: {button.name}");
            }
            else if (button.name.Contains("Win") && button.name.Contains("Exit") && gameManager.winExitButton == null)
            {
                gameManager.winExitButton = button;
                Debug.Log($"Found Win Exit Button: {button.name}");
            }
            else if (button.name.Contains("Win") && button.name.Contains("Analytics") && gameManager.winAnalyticsButton == null)
            {
                gameManager.winAnalyticsButton = button;
                Debug.Log($"Found Win Analytics Button: {button.name}");
            }
            else if (button.name.Contains("Lose") && button.name.Contains("Analytics") && gameManager.loseAnalyticsButton == null)
            {
                gameManager.loseAnalyticsButton = button;
                Debug.Log($"Found Lose Analytics Button: {button.name}");
            }
            else if (button.name.Contains("Analytics") && !button.name.Contains("Win") && !button.name.Contains("Lose") && gameManager.analyticsButton == null)
            {
                GameObject analyticsButtonObj = GameObject.Find(button.name);
                if (analyticsButtonObj != null)
                {
                    gameManager.analyticsButton = analyticsButtonObj.GetComponent<UnityEngine.UI.Button>();
                    Debug.Log($"Found Analytics Button: {button.name}");
                }
            }
        }
    }

    // Find remaining objects by tag
    private void FindRemainingObjectsByTag()
    {
        Debug.Log("Searching for remaining objects by tag...");

        // Try to find buttons by tag
        if (gameManager.button1 == null || gameManager.button2 == null || gameManager.button3 == null ||
            gameManager.button4 == null || gameManager.button5 == null)
        {
            GameObject[] buttonObjects = GameObject.FindGameObjectsWithTag("Button");
            if (buttonObjects.Length > 0)
            {
                // Sort them by name to try to get them in order
                System.Array.Sort(buttonObjects, (a, b) => a.name.CompareTo(b.name));

                if (gameManager.button1 == null && buttonObjects.Length >= 1)
                {
                    gameManager.button1 = buttonObjects[0];
                    Debug.Log($"Found button1 by tag: {buttonObjects[0].name}");
                }

                if (gameManager.button2 == null && buttonObjects.Length >= 2)
                {
                    gameManager.button2 = buttonObjects[1];
                    Debug.Log($"Found button2 by tag: {buttonObjects[1].name}");
                }

                if (gameManager.button3 == null && buttonObjects.Length >= 3)
                {
                    gameManager.button3 = buttonObjects[2];
                    Debug.Log($"Found button3 by tag: {buttonObjects[2].name}");
                }

                if (gameManager.button4 == null && buttonObjects.Length >= 4)
                {
                    gameManager.button4 = buttonObjects[3];
                    Debug.Log($"Found button4 by tag: {buttonObjects[3].name}");
                }

                if (gameManager.button5 == null && buttonObjects.Length >= 5)
                {
                    gameManager.button5 = buttonObjects[4];
                    Debug.Log($"Found button5 by tag: {buttonObjects[4].name}");
                }
            }
        }

        // Try to find lamp bases by tag
        if (gameManager.lampBase1 == null || gameManager.lampBase2 == null || gameManager.lampBase3 == null ||
            gameManager.lampBase4 == null || gameManager.lampBase5 == null || gameManager.lampBase6 == null ||
            gameManager.lampBase7 == null || gameManager.lampBase8 == null || gameManager.lampBase9 == null ||
            gameManager.lampBase10 == null)
        {
            GameObject[] lampObjects = GameObject.FindGameObjectsWithTag("LampBase");
            if (lampObjects.Length > 0)
            {
                // Sort them by name to try to get them in order
                System.Array.Sort(lampObjects, (a, b) => a.name.CompareTo(b.name));

                if (gameManager.lampBase1 == null && lampObjects.Length >= 1)
                {
                    gameManager.lampBase1 = lampObjects[0];
                    Debug.Log($"Found lampBase1 by tag: {lampObjects[0].name}");
                }

                if (gameManager.lampBase2 == null && lampObjects.Length >= 2)
                {
                    gameManager.lampBase2 = lampObjects[1];
                    Debug.Log($"Found lampBase2 by tag: {lampObjects[1].name}");
                }

                if (gameManager.lampBase3 == null && lampObjects.Length >= 3)
                {
                    gameManager.lampBase3 = lampObjects[2];
                    Debug.Log($"Found lampBase3 by tag: {lampObjects[2].name}");
                }

                if (gameManager.lampBase4 == null && lampObjects.Length >= 4)
                {
                    gameManager.lampBase4 = lampObjects[3];
                    Debug.Log($"Found lampBase4 by tag: {lampObjects[3].name}");
                }

                if (gameManager.lampBase5 == null && lampObjects.Length >= 5)
                {
                    gameManager.lampBase5 = lampObjects[4];
                    Debug.Log($"Found lampBase5 by tag: {lampObjects[4].name}");
                }

                if (gameManager.lampBase6 == null && lampObjects.Length >= 6)
                {
                    gameManager.lampBase6 = lampObjects[5];
                    Debug.Log($"Found lampBase6 by tag: {lampObjects[5].name}");
                }

                if (gameManager.lampBase7 == null && lampObjects.Length >= 7)
                {
                    gameManager.lampBase7 = lampObjects[6];
                    Debug.Log($"Found lampBase7 by tag: {lampObjects[6].name}");
                }

                if (gameManager.lampBase8 == null && lampObjects.Length >= 8)
                {
                    gameManager.lampBase8 = lampObjects[7];
                    Debug.Log($"Found lampBase8 by tag: {lampObjects[7].name}");
                }

                if (gameManager.lampBase9 == null && lampObjects.Length >= 9)
                {
                    gameManager.lampBase9 = lampObjects[8];
                    Debug.Log($"Found lampBase9 by tag: {lampObjects[8].name}");
                }

                if (gameManager.lampBase10 == null && lampObjects.Length >= 10)
                {
                    gameManager.lampBase10 = lampObjects[9];
                    Debug.Log($"Found lampBase10 by tag: {lampObjects[9].name}");
                }
            }
        }
    }

    // Log the status of all references
    private void LogReferenceStatus()
    {
        Debug.Log("=== Reference Status ===");

        // Log button references
        Debug.Log($"Button1: {(gameManager.button1 != null ? gameManager.button1.name : "NULL")}");
        Debug.Log($"Button2: {(gameManager.button2 != null ? gameManager.button2.name : "NULL")}");
        Debug.Log($"Button3: {(gameManager.button3 != null ? gameManager.button3.name : "NULL")}");
        Debug.Log($"Button4: {(gameManager.button4 != null ? gameManager.button4.name : "NULL")}");
        Debug.Log($"Button5: {(gameManager.button5 != null ? gameManager.button5.name : "NULL")}");

        // Log lamp base references
        Debug.Log($"LampBase1: {(gameManager.lampBase1 != null ? gameManager.lampBase1.name : "NULL")}");
        Debug.Log($"LampBase2: {(gameManager.lampBase2 != null ? gameManager.lampBase2.name : "NULL")}");
        Debug.Log($"LampBase3: {(gameManager.lampBase3 != null ? gameManager.lampBase3.name : "NULL")}");
        Debug.Log($"LampBase4: {(gameManager.lampBase4 != null ? gameManager.lampBase4.name : "NULL")}");
        Debug.Log($"LampBase5: {(gameManager.lampBase5 != null ? gameManager.lampBase5.name : "NULL")}");
        Debug.Log($"LampBase6: {(gameManager.lampBase6 != null ? gameManager.lampBase6.name : "NULL")}");
        Debug.Log($"LampBase7: {(gameManager.lampBase7 != null ? gameManager.lampBase7.name : "NULL")}");
        Debug.Log($"LampBase8: {(gameManager.lampBase8 != null ? gameManager.lampBase8.name : "NULL")}");
        Debug.Log($"LampBase9: {(gameManager.lampBase9 != null ? gameManager.lampBase9.name : "NULL")}");
        Debug.Log($"LampBase10: {(gameManager.lampBase10 != null ? gameManager.lampBase10.name : "NULL")}");

        // Log UI references
        Debug.Log($"InstructionText: {(gameManager.instructionText != null ? gameManager.instructionText.name : "NULL")}");
        Debug.Log($"ProgressText: {(gameManager.progressText != null ? gameManager.progressText.name : "NULL")}");
        Debug.Log($"TriesText: {(gameManager.triesText != null ? gameManager.triesText.name : "NULL")}");
        Debug.Log($"WinPanel: {(gameManager.winPanel != null ? gameManager.winPanel.name : "NULL")}");
        Debug.Log($"LosePanel: {(gameManager.losePanel != null ? gameManager.losePanel.name : "NULL")}");

        // Log analytics UI references
        Debug.Log($"AnalyticsButton: {(gameManager.analyticsButton != null ? gameManager.analyticsButton.name : "NULL")}");
        Debug.Log($"WinAnalyticsButton: {(gameManager.winAnalyticsButton != null ? gameManager.winAnalyticsButton.name : "NULL")}");
        Debug.Log($"LoseAnalyticsButton: {(gameManager.loseAnalyticsButton != null ? gameManager.loseAnalyticsButton.name : "NULL")}");
        Debug.Log($"AnalyticsPanel: {(gameManager.analyticsPanel != null ? gameManager.analyticsPanel.name : "NULL")}");
        Debug.Log($"AnalyticsText: {(gameManager.analyticsText != null ? gameManager.analyticsText.name : "NULL")}");

        // Log try analytics UI references
        Debug.Log($"TryAnalyticsPanel: {(gameManager.tryAnalyticsPanel != null ? gameManager.tryAnalyticsPanel.name : "NULL")}");
        Debug.Log($"TryAnalyticsText: {(gameManager.tryAnalyticsText != null ? gameManager.tryAnalyticsText.name : "NULL")}");

        // Log menu buttons
        Debug.Log($"RetryButton: {(gameManager.retryButton != null ? gameManager.retryButton.name : "NULL")}");
        Debug.Log($"MainMenuButton: {(gameManager.mainMenuButton != null ? gameManager.mainMenuButton.name : "NULL")}");
        Debug.Log($"ExitButton: {(gameManager.exitButton != null ? gameManager.exitButton.name : "NULL")}");

        // Log win panel buttons
        Debug.Log($"WinRetryButton: {(gameManager.winRetryButton != null ? gameManager.winRetryButton.name : "NULL")}");
        Debug.Log($"WinMainMenuButton: {(gameManager.winMainMenuButton != null ? gameManager.winMainMenuButton.name : "NULL")}");
        Debug.Log($"WinExitButton: {(gameManager.winExitButton != null ? gameManager.winExitButton.name : "NULL")}");

        Debug.Log("=== End Reference Status ===");
    }

    // Helper method to find a GameObject by name or tag
    private GameObject FindGameObjectByNameOrTag(string name, string tag)
    {
        // Try to find by name first
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            Debug.Log($"Found GameObject by name: {name}");
            return obj;
        }

        // Try to find by tag
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject taggedObj in taggedObjects)
        {
            if (taggedObj.name.Contains(name))
            {
                Debug.Log($"Found GameObject by tag and name contains: {name}");
                return taggedObj;
            }
        }

        // Try to find by name contains
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject foundObj in allObjects)
        {
            if (foundObj.name.Contains(name))
            {
                Debug.Log($"Found GameObject by name contains: {name}");
                return foundObj;
            }
        }

        Debug.LogWarning($"Could not find GameObject: {name}");
        return null;
    }

    // Helper method to find a missing button
    private void FindMissingButton(string buttonName, ref UnityEngine.UI.Button button)
    {
        if (button == null)
        {
            GameObject buttonObj = GameObject.Find(buttonName);
            if (buttonObj != null)
            {
                button = buttonObj.GetComponent<UnityEngine.UI.Button>();
                Debug.Log($"Found button: {buttonName}");
            }
            else
            {
                // Try to find by name contains
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains(buttonName))
                    {
                        UnityEngine.UI.Button foundButton = obj.GetComponent<UnityEngine.UI.Button>();
                        if (foundButton != null)
                        {
                            button = foundButton;
                            Debug.Log($"Found button by name contains: {buttonName}");
                            break;
                        }
                    }
                }
            }
        }
    }

    // Helper method to cache a GameObject reference
    private void CacheGameObject(string key, GameObject obj)
    {
        if (obj != null)
        {
            gameObjectCache[key] = obj;
            Debug.Log($"Cached GameObject: {key} = {obj.name}");
        }
        else
        {
            Debug.LogWarning($"Attempted to cache null GameObject for key: {key}");
        }
    }

    // Helper method to cache a Component reference
    private void CacheComponent(string key, Component component)
    {
        if (component != null)
        {
            componentCache[key] = component;
            Debug.Log($"Cached Component: {key} = {component.GetType().Name} on {component.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Attempted to cache null Component for key: {key}");
        }
    }

    // Helper method to restore a GameObject reference
    private GameObject RestoreGameObject(string key)
    {
        if (gameObjectCache.TryGetValue(key, out GameObject obj))
        {
            if (obj != null)
            {
                Debug.Log($"Restored GameObject: {key} = {obj.name}");
                return obj;
            }
            else
            {
                Debug.LogWarning($"Cached GameObject for key {key} is null, finding by name");
                // Try to find by name
                GameObject foundObj = GameObject.Find(key);
                if (foundObj != null)
                {
                    Debug.Log($"Found GameObject by name: {key}");
                    return foundObj;
                }
            }
        }

        Debug.LogWarning($"Failed to restore GameObject for key: {key}");
        return null;
    }

    // Helper method to restore a Component reference
    private T RestoreComponent<T>(string key) where T : Component
    {
        if (componentCache.TryGetValue(key, out Component component))
        {
            if (component != null)
            {
                Debug.Log($"Restored Component: {key} = {component.GetType().Name}");
                return component as T;
            }
        }

        Debug.LogWarning($"Failed to restore Component for key: {key}");
        return null;
    }

    // Public method to reset the game state
    public void ResetGameState()
    {
        Debug.Log("Resetting game state in PersistentGameManager");

        // Reset static variables in game scripts
        BasicButtonLampGame.ForceGameStateReset = true;
        SimpleDemoManager.IsGameRestarting = true;

        // Use cached references if available, otherwise find them
        if (demoManager == null)
        {
            demoManager = FindObjectOfType<SimpleDemoManager>();
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<BasicButtonLampGame>();
        }

        // Reset the demo manager
        if (demoManager != null)
        {
            demoManager.RestartDemo();
            Debug.Log("Reset SimpleDemoManager");
        }
        else
        {
            Debug.LogWarning("No SimpleDemoManager found to reset");
        }

        // Reset the game manager
        if (gameManager != null)
        {
            // Check for null analytics buttons and try to find them
            if (gameManager.winAnalyticsButton == null || gameManager.loseAnalyticsButton == null)
            {
                Debug.LogWarning("Analytics buttons are null, trying to find them before resetting game");
                FindAnalyticsButtons();
            }

            // Use reflection to reset private fields if needed
            // This is a fallback in case the public reset methods aren't sufficient
            System.Reflection.FieldInfo gameOverField = typeof(BasicButtonLampGame).GetField("gameOver",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gameOverField != null)
            {
                gameOverField.SetValue(gameManager, false);
                Debug.Log("Reset gameOver field in BasicButtonLampGame");
            }

            // Call the public reset method directly
            gameManager.ResetGame();
            Debug.Log("Called ResetGame method on BasicButtonLampGame");

            // Re-setup button listeners to ensure they work properly
            gameManager.SetupButtonListeners();
        }
        else
        {
            Debug.LogWarning("No BasicButtonLampGame found to reset");
        }

        gameStateReset = true;
        Debug.Log("Game state reset complete");
    }

    // Find analytics buttons if they're null
    private void FindAnalyticsButtons()
    {
        Debug.Log("Searching for analytics buttons...");

        // Find all buttons in the scene
        UnityEngine.UI.Button[] allButtons = FindObjectsOfType<UnityEngine.UI.Button>();

        foreach (UnityEngine.UI.Button button in allButtons)
        {
            // Look for win analytics button
            if (gameManager.winAnalyticsButton == null &&
                (button.name.Contains("Win") && button.name.Contains("Analytics")))
            {
                gameManager.winAnalyticsButton = button;
                Debug.Log($"Found Win Analytics Button: {button.name}");
            }

            // Look for lose analytics button
            if (gameManager.loseAnalyticsButton == null &&
                (button.name.Contains("Lose") && button.name.Contains("Analytics")))
            {
                gameManager.loseAnalyticsButton = button;
                Debug.Log($"Found Lose Analytics Button: {button.name}");
            }
        }

        // If we still couldn't find them, create temporary ones
        if (gameManager.winAnalyticsButton == null)
        {
            Debug.LogWarning("Could not find Win Analytics Button, will create a temporary one");
            CreateTemporaryAnalyticsButton("WinAnalyticsButton", ref gameManager.winAnalyticsButton);
        }

        if (gameManager.loseAnalyticsButton == null)
        {
            Debug.LogWarning("Could not find Lose Analytics Button, will create a temporary one");
            CreateTemporaryAnalyticsButton("LoseAnalyticsButton", ref gameManager.loseAnalyticsButton);
        }
    }

    // Create a temporary analytics button
    private void CreateTemporaryAnalyticsButton(string name, ref UnityEngine.UI.Button buttonRef)
    {
        // Check if we already have an analytics panel
        if (gameManager.analyticsPanel == null)
        {
            // Try to find it
            gameManager.analyticsPanel = GameObject.Find("AnalyticsPanel");

            // If still null, create a temporary one
            if (gameManager.analyticsPanel == null)
            {
                GameObject tempPanel = new GameObject("TemporaryAnalyticsPanel");
                tempPanel.AddComponent<RectTransform>();
                tempPanel.AddComponent<CanvasRenderer>();
                tempPanel.AddComponent<UnityEngine.UI.Image>();
                gameManager.analyticsPanel = tempPanel;

                // Make it inactive by default
                tempPanel.SetActive(false);

                Debug.Log("Created temporary analytics panel");
            }
        }

        // Create a temporary button
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(gameManager.analyticsPanel.transform);

        // Add required components
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(160, 30);

        buttonObj.AddComponent<CanvasRenderer>();
        buttonObj.AddComponent<UnityEngine.UI.Image>();

        // Add button component
        buttonRef = buttonObj.AddComponent<UnityEngine.UI.Button>();

        // Make it inactive by default
        buttonObj.SetActive(false);

        Debug.Log($"Created temporary {name}");
    }

    // Public method to force a complete game restart
    public void ForceCompleteRestart()
    {
        Debug.Log("Forcing complete game restart");

        // If we have preserved references, restore them first
        if (preserveReferences && gameObjectCache.Count > 0)
        {
            // Find game managers if needed
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<BasicButtonLampGame>();
            }

            if (demoManager == null)
            {
                demoManager = FindObjectOfType<SimpleDemoManager>();
            }

            // Restore references
            if (gameManager != null)
            {
                RestoreAllReferences();
            }
        }

        // Reset game state
        ResetGameState();

        // If we don't have preserved references, try to find and reset directly
        if (!preserveReferences || gameObjectCache.Count == 0)
        {
            // Find and reset the BasicButtonLampGame
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<BasicButtonLampGame>();
            }

            if (gameManager != null)
            {
                gameManager.ResetGame();
                Debug.Log("Reset BasicButtonLampGame directly");
            }

            // Find and restart the demo manager
            if (demoManager == null)
            {
                demoManager = FindObjectOfType<SimpleDemoManager>();
            }

            if (demoManager != null)
            {
                demoManager.RestartDemo();
                Debug.Log("Reset SimpleDemoManager directly");
            }
        }

        Debug.Log("Forced complete game restart without scene reload");
    }

    // Public method to reload the current scene
    public void ReloadCurrentScene()
    {
        Debug.Log("Reloading current scene");

        // Save any important data before reloading
        SaveGameState();

        // Get the current scene index
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        // Set a flag to indicate we're reloading the scene
        isReloadingScene = true;

        // Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
    }

    // Save important game state before scene reload
    private void SaveGameState()
    {
        Debug.Log("Saving game state before scene reload");

        // Save any important data to PlayerPrefs or other persistent storage
        if (gameManager != null)
        {
            // Save current tries
            PlayerPrefs.SetInt("CurrentTries", gameManager.currentTries);

            // Save current progress
            PlayerPrefs.SetInt("CurrentProgress", gameManager.currentProgress);

            // Save analytics data if needed
            if (gameManager.analyticsData != null && gameManager.analyticsData.Count > 0)
            {
                // Convert analytics data to JSON
                string analyticsJson = JsonUtility.ToJson(new AnalyticsDataWrapper(gameManager.analyticsData));
                PlayerPrefs.SetString("AnalyticsData", analyticsJson);
            }

            PlayerPrefs.Save();
        }
    }

    // Restore game state after scene reload
    private void RestoreGameState()
    {
        Debug.Log("Restoring game state after scene reload");

        if (gameManager != null && PlayerPrefs.HasKey("CurrentTries"))
        {
            // Restore current tries
            gameManager.currentTries = PlayerPrefs.GetInt("CurrentTries");

            // Restore current progress
            if (PlayerPrefs.HasKey("CurrentProgress"))
            {
                gameManager.currentProgress = PlayerPrefs.GetInt("CurrentProgress");
            }

            // Restore analytics data if needed
            if (PlayerPrefs.HasKey("AnalyticsData"))
            {
                string analyticsJson = PlayerPrefs.GetString("AnalyticsData");
                AnalyticsDataWrapper wrapper = JsonUtility.FromJson<AnalyticsDataWrapper>(analyticsJson);
                if (wrapper != null && wrapper.analyticsEntries != null)
                {
                    gameManager.analyticsData = wrapper.analyticsEntries;
                }
            }
        }
    }

    // Wrapper class for serializing analytics data
    [System.Serializable]
    private class AnalyticsDataWrapper
    {
        public List<string> analyticsEntries;

        public AnalyticsDataWrapper(List<string> entries)
        {
            analyticsEntries = entries;
        }
    }

    // Public method to debug the reference cache
    public void DebugReferences()
    {
        Debug.Log("=== PersistentGameManager Reference Debug ===");
        Debug.Log($"GameObject Cache Count: {gameObjectCache.Count}");
        Debug.Log($"Component Cache Count: {componentCache.Count}");

        Debug.Log("--- GameObject References ---");
        foreach (var entry in gameObjectCache)
        {
            string status = entry.Value != null ? "Valid" : "NULL";
            Debug.Log($"{entry.Key}: {status}");
        }

        Debug.Log("--- Component References ---");
        foreach (var entry in componentCache)
        {
            string status = entry.Value != null ? $"Valid ({entry.Value.GetType().Name})" : "NULL";
            Debug.Log($"{entry.Key}: {status}");
        }

        Debug.Log("=== End Reference Debug ===");
    }
}
