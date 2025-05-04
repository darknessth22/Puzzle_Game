using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class SimpleDemoManager : MonoBehaviour
{
    // Static flag to indicate the game is being restarted
    public static bool IsGameRestarting = false;

    [Header("Button References")]
    public GameObject button1;
    public GameObject button2;
    public GameObject button3;
    public GameObject button4;
    public GameObject button5;

    [Header("Number References")]
    public GameObject numberObject5;  // The actual number object inside lamp 5
    public GameObject numberObject6;  // The actual number object inside lamp 6
    public GameObject numberObject7;  // The actual number object inside lamp 7
    public GameObject numberObject8;  // The actual number object inside lamp 8
    public GameObject numberObject9;  // The actual number object inside lamp 9

    [Header("Demo Timing")]
    public float initialSceneDelay = 2.0f; // Delay after scene loads before starting demo
    public float demoDelay = 1.0f;        // Delay before starting demo
    public float highlightDuration = 2.0f; // How long each pair is highlighted
    public float pauseBetweenPairs = 1.0f; // Pause between pairs

    [Header("Audio")]
    public AudioClip backgroundMusic;     // Background music to play during the game
    public float musicVolume = 0.5f;      // Volume for background music

    [Header("Demo Sequence Settings")]
    public bool useRandomSequence = true; // Whether to use a random sequence instead of the defined sequence
    public int randomDemoCount = 5;       // How many random demonstrations to show

    [Header("Demo Sequence")]
    public GameObject[] demoSequence;     // Custom sequence of buttons to demonstrate (used if useRandomSequence is false)

    [Header("UI")]
    public TextMeshProUGUI instructionText;

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

    // Audio source for background music
    private AudioSource musicSource;

    private void Start()
    {
        Debug.Log("SimpleDemoManager Start - IsGameRestarting: " + IsGameRestarting);

        // Disabled - no highlight material needed

        // Clear instruction text at the start
        if (instructionText != null)
        {
            instructionText.text = "";
            Debug.Log("Cleared instruction text at start");
        }

        // Create an array of all available buttons
        GameObject[] allButtons = new GameObject[] { button1, button2, button3, button4, button5 };

        // Set up game manager references for all buttons
        SetupButtonGameManagerReferences();

        // If using random sequence, create it now
        if (useRandomSequence)
        {
            // Create a list of available buttons (removing any null ones)
            List<GameObject> availableButtons = new List<GameObject>();
            foreach (GameObject btn in allButtons)
            {
                if (btn != null) availableButtons.Add(btn);
            }

            // Determine how many demonstrations to show
            int demoCount = Mathf.Min(randomDemoCount, availableButtons.Count);

            // Create a new random sequence
            List<GameObject> randomSequence = new List<GameObject>();

            // Shuffle the available buttons
            for (int i = 0; i < demoCount; i++)
            {
                if (availableButtons.Count == 0) break;

                // Pick a random button from the available ones
                int randomIndex = Random.Range(0, availableButtons.Count);
                GameObject randomButton = availableButtons[randomIndex];

                // Add it to our sequence and remove from available buttons
                randomSequence.Add(randomButton);
                availableButtons.RemoveAt(randomIndex);
            }

            // Set the demo sequence to our random sequence
            demoSequence = randomSequence.ToArray();
            Debug.Log($"Created random demo sequence with {demoSequence.Length} buttons");
        }
        // Otherwise, check if we need to create a default sequence
        else if (demoSequence == null || demoSequence.Length == 0)
        {
            // If no custom sequence is provided, create a default one
            demoSequence = allButtons;
            Debug.Log("Using default demo sequence");
        }

        // Setup background music
        SetupBackgroundMusic();

        // Start the demo after a delay
        StartCoroutine(StartDemoWithDelay());

        // Reset the restart flag
        IsGameRestarting = false;

        // Set demo running flag
        isDemoRunning = true;
        isGameplayActive = false;
        demoCompleted = false;
    }

    private void SetupBackgroundMusic()
    {
        // Check if we already have an AudioSource component
        musicSource = GetComponent<AudioSource>();

        // If not, add one
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure the audio source
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            // Play the background music
            musicSource.Play();
            Debug.Log("Started playing background music");
        }
        else if (backgroundMusic == null)
        {
            Debug.LogWarning("No background music clip assigned");
        }
    }

    private IEnumerator StartDemoWithDelay()
    {
        Debug.Log($"Waiting {initialSceneDelay} seconds before starting demo...");
        yield return new WaitForSeconds(initialSceneDelay);

        // Start the demo sequence
        yield return StartCoroutine(RunDemoSequence());

        // After demo completes, activate gameplay
        isDemoRunning = false;
        isGameplayActive = true;

        // Make sure instruction text is still clear
        if (instructionText != null)
        {
            instructionText.text = "";
            Debug.Log("Kept instruction text clear after demo");
        }

        // Reset the game state in BasicButtonLampGame
        ResetGameManagerState();
    }

    // Run the demo sequence
    private IEnumerator RunDemoSequence()
    {
        Debug.Log("Starting demo sequence...");
        isDemoRunning = true;

        // Wait for the demo delay
        yield return new WaitForSeconds(demoDelay);

        // Go through each button in the sequence
        foreach (GameObject button in demoSequence)
        {
            if (button == null) continue;

            Debug.Log($"Demonstrating button: {button.name}");

            // Get the corresponding number for this button
            int numberValue = -1;
            if (button == button1) numberValue = 5;
            else if (button == button2) numberValue = 6;
            else if (button == button3) numberValue = 7;
            else if (button == button4) numberValue = 8;
            else if (button == button5) numberValue = 9;

            // Get the corresponding number object
            GameObject numberObject = null;
            switch (numberValue)
            {
                case 5: numberObject = numberObject5; break;
                case 6: numberObject = numberObject6; break;
                case 7: numberObject = numberObject7; break;
                case 8: numberObject = numberObject8; break;
                case 9: numberObject = numberObject9; break;
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
                Debug.Log($"Applied cyan material to button: {button.name}");
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
                    Debug.Log($"Applied cyan material to number: {numberObject.name}");
                }
            }

            // Wait for the highlight duration
            yield return new WaitForSeconds(highlightDuration);

            // Reset the button material
            if (buttonRenderer != null && originalButtonMaterial != null)
            {
                buttonRenderer.material = originalButtonMaterial;
                Debug.Log($"Reset material for button: {button.name}");
            }

            // Reset the number material
            if (numberRenderer != null && originalNumberMaterial != null)
            {
                numberRenderer.material = originalNumberMaterial;
                Debug.Log($"Reset material for number: {numberObject.name}");
            }

            // Wait for the pause between pairs
            yield return new WaitForSeconds(pauseBetweenPairs);
        }

        Debug.Log("Demo sequence completed");
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
                Debug.Log($"Playing animation for button {button.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to play 'switch' animation: {e.Message}");

                // If that fails, try to play any state
                if (buttonAnimator.runtimeAnimatorController != null)
                {
                    AnimationClip[] clips = buttonAnimator.runtimeAnimatorController.animationClips;
                    if (clips.Length > 0)
                    {
                        buttonAnimator.Play(clips[0].name, 0, 0f);
                        Debug.Log($"Playing fallback animation {clips[0].name} for button {button.name}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"No animator found on button {button.name}");
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
            Debug.Log($"Playing sound for button {button.name}");
        }
        else if (buttonClickSound == null)
        {
            Debug.LogWarning($"No button click sound assigned for button {button.name}");
        }
        else if (audioSource == null)
        {
            Debug.LogWarning($"No audio source found for button {button.name}");
        }
    }

    // Public method to adjust music volume
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume); // Ensure volume is between 0 and 1

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            Debug.Log($"Background music volume set to {musicVolume}");
        }
    }

    // Called when the object is destroyed
    private void OnDestroy()
    {
        // Clean up resources if needed
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // Set up game manager references for all buttons and lamp bases
    private void SetupButtonGameManagerReferences()
    {
        // Find the game manager
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();

        if (gameManager == null)
        {
            Debug.LogError("No BasicButtonLampGame found in the scene!");
            return;
        }

        // Set up references for all buttons
        GameObject[] allButtons = new GameObject[] { button1, button2, button3, button4, button5 };

        foreach (GameObject btn in allButtons)
        {
            if (btn != null)
            {
                BasicButton basicButton = btn.GetComponent<BasicButton>();
                if (basicButton != null)
                {
                    basicButton.gameManager = gameManager;
                    Debug.Log($"Set game manager reference for button: {btn.name}");
                }
            }
        }

        // Set up references for all lamp bases
        BasicNumber[] allLampBases = FindObjectsOfType<BasicNumber>();
        foreach (BasicNumber lampBase in allLampBases)
        {
            if (lampBase != null)
            {
                lampBase.gameManager = gameManager;
                Debug.Log($"Set game manager reference for lamp base: {lampBase.gameObject.name}");
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
            Debug.LogError("No BasicButtonLampGame found in the scene!");
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
                Debug.Log("Successfully reset gameOver flag to false");
            }
            else
            {
                Debug.LogWarning("Could not find gameOver field in BasicButtonLampGame");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error resetting gameOver flag: " + e.Message);
        }

        Debug.Log("Game manager state reset after demo completion");
    }

    // Public method to restart the game without reloading the scene
    public void RestartDemo()
    {
        Debug.Log("RestartDemo called - starting game without demo");

        // Reset state
        isDemoRunning = false;
        isGameplayActive = true;

        // Reset all objects to their original state
        ResetAllObjects();

        // Keep instruction text clear
        if (instructionText != null)
        {
            instructionText.text = "";
            Debug.Log("Kept instruction text clear in RestartDemo");
        }
    }

    // Reset all objects to their original state
    private void ResetAllObjects()
    {
        Debug.Log("Resetting all objects to original state");
    }

    // Public method to completely reset the demo
    public void ResetDemo()
    {
        Debug.Log("ResetDemo called - resetting demo state");

        // Reset state flags
        isDemoRunning = false;
        isGameplayActive = false;
        demoCompleted = false;

        // Reset all objects
        ResetAllObjects();

        // Start the demo sequence again
        StartCoroutine(StartDemoWithDelay());

        Debug.Log("Demo reset complete - demo will start again after delay");
    }
}
