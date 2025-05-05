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
        PlayButtonSound();
        PlayButtonAnimation();
        FindGameManager();

        if (gameManager != null)
        {
            gameManager.ButtonClicked(gameObject);
        }
    }

    private void OnEnable()
    {
        FindGameManager();
    }

    private void Start()
    {
        FindGameManager();
        EnsureButtonTag();
    }

    private void EnsureButtonTag()
    {
        if (gameObject.tag == "Untagged" || gameObject.tag != "Button")
        {
            gameObject.tag = "Button";
        }
    }

    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<BasicButtonLampGame>();
        }
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, volume);
        }
        else if (buttonClickSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound, volume);
            }
        }
    }

    private void PlayButtonAnimation()
    {
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponent<Animator>();
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
}
