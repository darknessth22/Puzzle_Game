using UnityEngine;

public class BasicButton : MonoBehaviour
{
    public BasicButtonLampGame gameManager;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public float volume = 1.0f;

    [Header("Animation Settings")]
    public Animator buttonAnimator;

    private void OnMouseDown()
    {
        // Play sound effect
        PlayButtonSound();

        // Play animation
        PlayButtonAnimation();

        // Make sure we have a game manager reference
        FindGameManager();

        // Notify the game manager that this button was clicked
        if (gameManager != null)
        {
            gameManager.ButtonClicked(gameObject);
        }
        else
        {
            Debug.LogError("No game manager assigned to button: " + gameObject.name);
        }
    }

    // Called when the object is enabled
    private void OnEnable()
    {
        // Always try to find the game manager when enabled
        // This is important for when the game is restarted
        FindGameManager();
    }

    // Called when the object starts
    private void Start()
    {
        // Make sure we have a game manager reference
        FindGameManager();

        // Make sure this object has the Button tag
        EnsureButtonTag();
    }

    // Ensure this object has the Button tag
    private void EnsureButtonTag()
    {
        if (gameObject.tag == "Untagged" || gameObject.tag != "Button")
        {
            gameObject.tag = "Button";
        }
    }

    // Find the game manager
    private void FindGameManager()
    {
        // Try to find the game manager if not already assigned
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<BasicButtonLampGame>();
        }
    }

    // Play the button click sound
    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, volume);
        }
        else if (buttonClickSound != null)
        {
            // If no AudioSource is assigned but we have a clip, try to get the component
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound, volume);
            }
            else
            {
                Debug.LogWarning("No AudioSource found on button: " + gameObject.name);
            }
        }
    }

    // Play the button animation
    private void PlayButtonAnimation()
    {
        // Get the Animator component if not already assigned
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponent<Animator>();
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
}
