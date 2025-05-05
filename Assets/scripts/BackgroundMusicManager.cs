using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    private static BackgroundMusicManager instance;
    private AudioSource audioSource;

    [Header("Music Settings")]
    public AudioClip musicClip;
    public float volume = 0.5f;
    public bool playOnAwake = true;
    public bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = musicClip;
            audioSource.volume = volume;
            audioSource.loop = true;

            if (playOnAwake && musicClip != null)
            {
                audioSource.Play();
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);

        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public void ChangeMusic(AudioClip newClip)
    {
        if (audioSource != null && newClip != null)
        {
            bool wasPlaying = audioSource.isPlaying;
            audioSource.Stop();
            audioSource.clip = newClip;

            if (wasPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
