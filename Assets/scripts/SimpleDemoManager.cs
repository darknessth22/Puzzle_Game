using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SimpleDemoManager : MonoBehaviour
{
    public static bool IsGameRestarting = false;

    [Header("Demo Timing")]
    public float initialSceneDelay = 2.0f;
    public float demoDelay = 1.0f;
    public float highlightDuration = 2.0f;
    public float pauseBetweenPairs = 1.0f;

    [Header("Demo Sequence")]
    [Tooltip("If left empty, will automatically use all buttons from ButtonNumberMapping")]
    public GameObject[] demoSequence;

    [Header("Audio")]
    public AudioClip demoCompletionSound;
    public float demoCompletionSoundVolume = 1.0f;

    [Header("Post-Demo Message")]
    public float textDisplayDuration = 3.0f;
    public TextMeshProUGUI demoCompletionTextUI;

    public bool isDemoRunning = false;
    public bool isGameplayActive = false;
    public bool demoCompleted = false;
    public bool isShowingPostDemoMessage = false;

    private AudioSource demoCompletionAudioSource;

    public bool IsDemoRunning()
    {
        return isDemoRunning;
    }

    public bool IsGameplayActive()
    {
        return isGameplayActive;
    }

    public bool IsShowingPostDemoMessage()
    {
        return isShowingPostDemoMessage;
    }

    [Header("Button-Number Mapping")]
    public ButtonNumberMapping buttonNumberMapping;

    public int GetNumberForButton(GameObject button)
    {
        if (buttonNumberMapping != null)
        {
            int numberValue = buttonNumberMapping.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager != null && gameManager.buttonNumberMapping != null)
        {
            int numberValue = gameManager.buttonNumberMapping.GetNumberForButton(button);
            if (numberValue != -1)
            {
                return numberValue;
            }
        }

        return -1;
    }

    public bool IsCorrectMatch(GameObject button, int numberValue)
    {
        if (buttonNumberMapping != null)
        {
            bool isMatch = buttonNumberMapping.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
        if (gameManager != null && gameManager.buttonNumberMapping != null)
        {
            bool isMatch = gameManager.buttonNumberMapping.IsCorrectMatch(button, numberValue);
            if (isMatch)
            {
                return true;
            }
        }

        return false;
    }

    private void Start()
    {
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

        GameObject[] allButtons = buttonList.ToArray();

        SetupButtonGameManagerReferences();

        InitializeButtonNumberMapping();

        if (demoSequence == null || demoSequence.Length == 0)
        {
            demoSequence = allButtons;
        }

        StartCoroutine(StartDemoWithDelay());

        IsGameRestarting = false;

        isDemoRunning = true;
        isGameplayActive = false;
        demoCompleted = false;
    }

    private IEnumerator StartDemoWithDelay()
    {
        // Start demo immediately without initial delay
        yield return StartCoroutine(RunDemoSequence());

        ResetGameManagerState();
    }

    private IEnumerator RunDemoSequence()
    {
        isDemoRunning = true;

        // Start immediately without any delay
        yield return null;

        foreach (GameObject button in demoSequence)
        {
            if (button == null) continue;

            int numberValue = -1;
            GameObject numberObject = null;

            if (buttonNumberMapping != null)
            {
                numberValue = buttonNumberMapping.GetNumberForButton(button);
                numberObject = buttonNumberMapping.GetNumberObjectForButton(button);
            }

            if (numberValue == -1 || numberObject == null)
            {
                BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();
                if (gameManager != null && gameManager.buttonNumberMapping != null)
                {
                    numberValue = gameManager.buttonNumberMapping.GetNumberForButton(button);
                    numberObject = gameManager.buttonNumberMapping.GetNumberObjectForButton(button);
                }
            }

            if (numberValue == -1 || numberObject == null)
            {
                continue;
            }

            Renderer buttonRenderer = button.GetComponent<Renderer>();
            Material originalButtonMaterial = null;
            if (buttonRenderer != null)
            {
                originalButtonMaterial = buttonRenderer.material;

                Material cyanMaterial = new Material(Shader.Find("Standard"));
                cyanMaterial.color = Color.cyan;
                cyanMaterial.EnableKeyword("_EMISSION");
                cyanMaterial.SetColor("_EmissionColor", Color.cyan * 2f);

                buttonRenderer.material = cyanMaterial;
            }

            HighlightObject(button);

            Renderer numberRenderer = null;
            Material originalNumberMaterial = null;
            if (numberObject != null)
            {
                numberRenderer = numberObject.GetComponent<Renderer>();
                if (numberRenderer != null)
                {
                    originalNumberMaterial = numberRenderer.material;

                    Material cyanMaterial = new Material(Shader.Find("Standard"));
                    cyanMaterial.color = Color.cyan;
                    cyanMaterial.EnableKeyword("_EMISSION");
                    cyanMaterial.SetColor("_EmissionColor", Color.cyan * 2f);

                    numberRenderer.material = cyanMaterial;
                }
            }

            yield return new WaitForSeconds(highlightDuration);

            if (buttonRenderer != null && originalButtonMaterial != null)
            {
                buttonRenderer.material = originalButtonMaterial;
            }

            if (numberRenderer != null && originalNumberMaterial != null)
            {
                numberRenderer.material = originalNumberMaterial;
            }

            yield return new WaitForSeconds(pauseBetweenPairs);
        }

        isDemoRunning = false;
        demoCompleted = true;

        StartCoroutine(ShowDemoCompletionTextAndSound());
    }

    private void HighlightObject(GameObject obj)
    {
        BasicButton basicButton = obj.GetComponent<BasicButton>();
        if (basicButton != null)
        {
            PlayButtonAnimation(obj);
            PlayButtonSound(obj);
        }
    }

    private void PlayButtonAnimation(GameObject button)
    {
        Animator buttonAnimator = null;
        BasicButton basicButton = button.GetComponent<BasicButton>();

        if (basicButton != null && basicButton.buttonAnimator != null)
        {
            buttonAnimator = basicButton.buttonAnimator;
        }
        else
        {
            buttonAnimator = button.GetComponent<Animator>();
        }

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

    private void PlayButtonSound(GameObject button)
    {
        AudioSource audioSource = null;
        AudioClip buttonClickSound = null;
        float volume = 1.0f;

        BasicButton basicButton = button.GetComponent<BasicButton>();
        if (basicButton != null)
        {
            audioSource = basicButton.audioSource;
            buttonClickSound = basicButton.buttonClickSound;
            volume = basicButton.volume;
        }

        if (audioSource == null)
        {
            audioSource = button.GetComponent<AudioSource>();
        }

        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, volume);
        }
    }

    private void SetupButtonGameManagerReferences()
    {
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();

        if (gameManager == null)
        {
            return;
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

        foreach (GameObject btn in buttonList)
        {
            BasicButton basicButton = btn.GetComponent<BasicButton>();
            if (basicButton != null)
            {
                basicButton.gameManager = gameManager;
            }
        }

        BasicNumber[] allLampBases = FindObjectsOfType<BasicNumber>();
        foreach (BasicNumber lampBase in allLampBases)
        {
            if (lampBase != null)
            {
                lampBase.gameManager = gameManager;
            }
        }
    }

    private void ResetGameManagerState()
    {
        BasicButtonLampGame gameManager = FindObjectOfType<BasicButtonLampGame>();

        if (gameManager == null)
        {
            return;
        }

        BasicButtonLampGame.ForceGameStateReset = false;

        gameManager.currentTries = gameManager.maxTries;
        gameManager.currentProgress = 0;

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
        }
    }

    public void RestartDemo()
    {
        isDemoRunning = false;
        isGameplayActive = true;
        isShowingPostDemoMessage = false;

        if (demoCompletionTextUI != null)
        {
            demoCompletionTextUI.gameObject.SetActive(false);
        }

        ResetAllObjects();
    }

    private void ResetAllObjects()
    {
        if (buttonNumberMapping != null)
        {
            foreach (var pair in buttonNumberMapping.buttonNumberPairs)
            {
                if (pair.numberObject != null)
                {
                    Renderer renderer = pair.numberObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.material = defaultMaterial;
                    }
                }
            }
        }

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
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = Color.white;
                        renderer.material = defaultMaterial;
                    }
                }
            }
        }
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

    public void ResetDemo()
    {
        isDemoRunning = false;
        isGameplayActive = false;
        demoCompleted = false;
        isShowingPostDemoMessage = false;

        if (demoCompletionTextUI != null)
        {
            demoCompletionTextUI.gameObject.SetActive(false);
        }

        ResetAllObjects();

        InitializeButtonNumberMapping();

        StartCoroutine(StartDemoWithDelay());
    }

    private void PlayDemoCompletionSound()
    {
        if (demoCompletionSound == null)
            return;

        if (demoCompletionAudioSource == null)
        {
            demoCompletionAudioSource = gameObject.AddComponent<AudioSource>();
            demoCompletionAudioSource.playOnAwake = false;
            demoCompletionAudioSource.loop = false;
        }

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        Dictionary<AudioSource, float> originalVolumes = new Dictionary<AudioSource, float>();

        foreach (AudioSource source in allAudioSources)
        {
            if (source != demoCompletionAudioSource)
            {
                originalVolumes[source] = source.volume;
                source.volume = 0;
            }
        }

        demoCompletionAudioSource.clip = demoCompletionSound;
        demoCompletionAudioSource.volume = demoCompletionSoundVolume;
        demoCompletionAudioSource.Play();
        StartCoroutine(RestoreAudioVolumes(originalVolumes, demoCompletionSound.length));
    }

    private IEnumerator RestoreAudioVolumes(Dictionary<AudioSource, float> originalVolumes, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var kvp in originalVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.volume = kvp.Value;
            }
        }
    }

    private IEnumerator ShowDemoCompletionTextAndSound()
    {
        isShowingPostDemoMessage = true;

        float soundDuration = 0f;
        if (demoCompletionSound != null)
        {
            PlayDemoCompletionSound();
            soundDuration = demoCompletionSound.length;
        }

        if (demoCompletionTextUI != null)
        {
            demoCompletionTextUI.gameObject.SetActive(true);

            float waitTime = Mathf.Max(textDisplayDuration, soundDuration);
            yield return new WaitForSeconds(waitTime);

            demoCompletionTextUI.gameObject.SetActive(false);
        }
        else
        {
            if (soundDuration > 0)
            {
                yield return new WaitForSeconds(soundDuration);
            }
        }

        isShowingPostDemoMessage = false;
        isGameplayActive = true;
    }
}
