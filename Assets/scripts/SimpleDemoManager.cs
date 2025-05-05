using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleDemoManager : MonoBehaviour
{
    // Static flag to indicate the game is being restarted
    public static bool IsGameRestarting = false;

    // Buttons are now defined through the ButtonNumberMapping component in the Inspector

    // Number objects are now defined through the ButtonNumberMapping component in the Inspector

    [Header("Demo Timing")]
    public float initialSceneDelay = 2.0f; // Delay after scene loads before starting demo
    public float demoDelay = 1.0f;        // Delay before starting demo
    public float highlightDuration = 2.0f; // How long each pair is highlighted
    public float pauseBetweenPairs = 1.0f; // Pause between pairs

    [Header("Demo Sequence")]
    [Tooltip("If left empty, will automatically use all buttons from ButtonNumberMapping")]
    public GameObject[] demoSequence;     // Custom sequence of buttons to demonstrate

    // Game state tracking

    // Game state
    public bool isDemoRunning = false;
    public bool isGameplayActive = false;
    public bool demoCompleted = false;

    // Public method to check if demo is running
    public bool IsDemoRunning()
    {
        return isDemoRunning;
    }

    // Public method to check if gameplay is active
    public bool IsGameplayActive()
    {
        return isGameplayActive;
    }

    // Button-to-number mapping component
    [Header("Button-Number Mapping")]
    public ButtonNumberMapping buttonNumberMapping;

    // Public method to get the number associated with a button
    public int GetNumberForButton(GameObject button)
    {
        // First try our own ButtonNumberMapping
        if (buttonNumberMapping != null)
        {
            int numberValue = buttonNumberMapping.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        // If not found, try the BasicButtonLampGame's ButtonNumberMapping
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager != null && gameManager.buttonNumberMapping != null)
        {
            int numberValue = gameManager.buttonNumberMapping.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        return -1; // Return -1 if no mapping exists
    }

    // Public method to check if a button and number match according to the current mapping
    public bool IsCorrectMatch(GameObject button, int numberValue)
    {
        // First try our own ButtonNumberMapping
        if (buttonNumberMapping != null)
        {
            bool isMatch = buttonNumberMapping.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

        // If not found, try the BasicButtonLampGame's ButtonNumberMapping
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager != null && gameManager.buttonNumberMapping != null)
        {
            bool isMatch = gameManager.buttonNumberMapping.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

        return false; // No mapping exists, so it's not a correct match
    }

    private void Start()
    {
        // Disabled - no highlight material needed



        // Get buttons from ButtonNumberMapping
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

        GameObject[] allButtons = buttonList.ToArray();

        // Set up game manager references for all buttons
        SetupButtonGameManagerReferences();

        // Initialize button-number mapping if not already set
        InitializeButtonNumberMapping();

        // Use default sequence if none is provided
        if (demoSequence == null || demoSequence.Length == 0)
        {
            // If no custom sequence is provided, create a default one with all buttons
            demoSequence = allButtons;
        }

        // Start the demo after a delay
        StartCoroutine(StartDemoWithDelay());

        // Reset the restart flag
        IsGameRestarting = false;

        // Set demo running flag
        isDemoRunning = true;
        isGameplayActive = false;
        demoCompleted = false;
    }



    private IEnumerator StartDemoWithDelay()
    {
        yield return new WaitForSeconds(initialSceneDelay);

        // Start the demo sequence
        yield return StartCoroutine(RunDemoSequence());

        // After demo completes, activate gameplay
        isDemoRunning = false;
        isGameplayActive = true;



        // Reset the game state in BasicButtonLampGame
        ResetGameManagerState();
    }

    // Run the demo sequence
    private IEnumerator RunDemoSequence()
    {
        isDemoRunning = true;

        // Wait for the demo delay
        yield return new WaitForSeconds(demoDelay);

        // Go through each button in the sequence
        foreach (GameObject button in demoSequence)
        {
            if (button == null) continue;

            // Get the corresponding number for this button from our mapping
            int numberValue = -1;
            GameObject numberObject = null;

            // First try to get mapping from this SimpleDemoManager's ButtonNumberMapping
            if (buttonNumberMapping != null)
            {
                numberValue = buttonNumberMapping.GetNumberForButton(button);
                numberObject = buttonNumberMapping.GetNumberObjectForButton(button);
            }

            // If not found, try to get mapping from BasicButtonLampGame
            if (numberValue == -1 || numberObject == null)
            {
                BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
                if (gameManager != null && gameManager.buttonNumberMapping != null)
                {
                    numberValue = gameManager.buttonNumberMapping.GetNumberForButton(button);
                    numberObject = gameManager.buttonNumberMapping.GetNumberObjectForButton(button);

                }
            }

            // If still not found, skip this button
            if (numberValue == -1 || numberObject == null)
            {
                continue; // Skip this button if no mapping is found
            }

            // Highlight the button with cyan color
            Renderer buttonRenderer = button.GetComponent<Renderer>();
            Material originalButtonMaterial = null;
            if (buttonRenderer != null)
            {
                // Store original material
                originalButtonMaterial = buttonRenderer.material;

                // Create a cyan material
                Material cyanMaterial = new Material(Shader.Find("Standard"));
                cyanMaterial.color = Color.cyan;
                cyanMaterial.EnableKeyword("_EMISSION");
                cyanMaterial.SetColor("_EmissionColor", Color.cyan * 2f);

                // Apply cyan material to button
                buttonRenderer.material = cyanMaterial;
            }

            // Play button animation and sound
            HighlightObject(button);

            // Highlight the number object if found
            Renderer numberRenderer = null;
            Material originalNumberMaterial = null;
            if (numberObject != null)
            {
                // Make the number visible with cyan color
                numberRenderer = numberObject.GetComponent<Renderer>();
                if (numberRenderer != null)
                {
                    // Store original material
                    originalNumberMaterial = numberRenderer.material;

                    // Create a cyan material
                    Material cyanMaterial = new Material(Shader.Find("Standard"));
                    cyanMaterial.color = Color.cyan;
                    cyanMaterial.EnableKeyword("_EMISSION");
                    cyanMaterial.SetColor("_EmissionColor", Color.cyan * 2f);

                    // Apply cyan material to number
                    numberRenderer.material = cyanMaterial;
                }
            }

            // Wait for the highlight duration
            yield return new WaitForSeconds(highlightDuration);

            // Reset the button material
            if (buttonRenderer != null && originalButtonMaterial != null)
            {
                buttonRenderer.material = originalButtonMaterial;
            }

            // Reset the number material
            if (numberRenderer != null && originalNumberMaterial != null)
            {
                numberRenderer.material = originalNumberMaterial;
            }

            // Wait for the pause between pairs
            yield return new WaitForSeconds(pauseBetweenPairs);
        }

        isDemoRunning = false;
        isGameplayActive = true;
        demoCompleted = true;
    }

    private void HighlightObject(GameObject obj)
    {
        // Disabled - no highlighting during demo

        // Check if this is a button (not a number object)
        BasicButton basicButton = obj.GetComponent<BasicButton>();
        if (basicButton != null)
        {
            // Play button animation
            PlayButtonAnimation(obj);

            // Play button sound
            PlayButtonSound(obj);
        }
    }

    // Play the button animation during demo
    private void PlayButtonAnimation(GameObject button)
    {
        // Try to get the animator from the BasicButton component
        Animator buttonAnimator = null;
        BasicButton basicButton = button.GetComponent<BasicButton>();

        if (basicButton != null && basicButton.buttonAnimator != null)
        {
            buttonAnimator = basicButton.buttonAnimator;
        }
        else
        {
            // Try to get the animator directly from the button
            buttonAnimator = button.GetComponent<Animator>();
        }

        // Play the animation if we have an Animator
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

    // Play the button sound during demo
    private void PlayButtonSound(GameObject button)
    {
        AudioSource audioSource = null;
        AudioClip buttonClickSound = null;
        float volume = 1.0f;

        // Try to get audio settings from BasicButton component
        BasicButton basicButton = button.GetComponent<BasicButton>();
        if (basicButton != null)
        {
            audioSource = basicButton.audioSource;
            buttonClickSound = basicButton.buttonClickSound;
            volume = basicButton.volume;
        }

        // If no AudioSource is assigned but we have a component on the button
        if (audioSource == null)
        {
            audioSource = button.GetComponent<AudioSource>();
        }

        // Play the sound if we have both an AudioSource and a clip
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, volume);
        }
    }



    // Set up game manager references for all buttons and lamp bases
    private void SetupButtonGameManagerReferences()
    {
        // Find the game manager
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();

        if (gameManager == null)
        {
            return;
        }

        // Get buttons from ButtonNumberMapping
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

        foreach (GameObject btn in buttonList)
        {
            BasicButton basicButton = btn.GetComponent<BasicButton>();
            if (basicButton != null)
            {
                basicButton.gameManager = gameManager;
            }
        }

        // Set up references for all lamp bases
        BasicNumber[] allLampBases = FindObjectsOfType<BasicNumber>();
        foreach (BasicNumber lampBase in allLampBases)
        {
            if (lampBase != null)
            {
                lampBase.gameManager = gameManager;
            }
        }
    }

    // Reset the game state in BasicButtonLampGame
    private void ResetGameManagerState()
    {
        // Find the game manager
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();

        if (gameManager == null)
        {
            return;
        }

        // Reset the ForceGameStateReset flag
        BasicButtonLampGame.ForceGameStateReset = false;

        // Reset game state without calling ResetGame which would trigger the demo
        gameManager.currentTries = gameManager.maxTries;
        gameManager.currentProgress = 0;

        // Explicitly set the gameOver field to false using reflection
        // This is a more aggressive approach to ensure the game state is reset
        try
        {
            System.Reflection.FieldInfo gameOverField = typeof(BasicButtonLampGame).GetField("gameOver",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (gameOverField != null)
            {
                gameOverField.SetValue(gameManager, false);
            }
        }
        catch (System.Exception)
        {
            // Silently fail if reflection doesn't work
        }
    }

    // Public method to restart the game without reloading the scene
    public void RestartDemo()
    {
        // Reset state
        isDemoRunning = false;
        isGameplayActive = true;

        // Reset all objects to their original state
        ResetAllObjects();
    }

    // Reset all objects to their original state
    private void ResetAllObjects()
    {
        // Reset all number objects from ButtonNumberMapping
        if (buttonNumberMapping != null)
        {
            foreach (var pair in buttonNumberMapping.buttonNumberPairs)
            {
                if (pair.numberObject != null)
                {
                    Renderer renderer = pair.numberObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Create a default material if needed
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.material = defaultMaterial;
                    }
                }
            }
        }

        // Also try to reset from BasicButtonLampGame's ButtonNumberMapping
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager != null && gameManager.buttonNumberMapping != null)
        {
            foreach (var pair in gameManager.buttonNumberMapping.buttonNumberPairs)
            {
                if (pair.numberObject != null)
                {
                    Renderer renderer = pair.numberObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Create a default material if needed
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.material = defaultMaterial;
                    }
                }
            }
        }
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

    // Public method to completely reset the demo
    public void ResetDemo()
    {
        // Reset state flags
        isDemoRunning = false;
        isGameplayActive = false;
        demoCompleted = false;

        // Reset all objects
        ResetAllObjects();

        // Reinitialize button-number mapping
        InitializeButtonNumberMapping();

        // Start the demo sequence again
        StartCoroutine(StartDemoWithDelay());
    }
}
