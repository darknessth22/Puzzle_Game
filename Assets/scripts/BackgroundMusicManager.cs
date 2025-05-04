using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    // Singleton instance
    private static BackgroundMusicManager instance;

    // Audio source component
    private AudioSource audioSource;

    [Header("Music Settings")]
    public AudioClip musicClip;
    public float volume = 0.5f;
    public bool playOnAwake = true;
    public bool dontDestroyOnLoad = true;

    private void Awake()
    {
        // Singleton pattern to ensure only one music manager exists
        if (instance == null)
        {
            instance = this;
            
            // Don't destroy this object when loading a new scene
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // Get or add audio source component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Configure audio source
            audioSource.clip = musicClip;
            audioSource.volume = volume;
            audioSource.loop = true;
            
            // Play music if set to play on awake
            if (playOnAwake && musicClip != null)
            {
                audioSource.Play();
                Debug.Log("Background music started playing");
            }
        }
        else if (instance != this)
        {
            // If another instance exists, destroy this one
            Destroy(gameObject);
        }
    }

    // Public method to play music
    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying && audioSource.clip != null)
        {
            audioSource.Play();
            Debug.Log("Background music started playing");
        }
    }

    // Public method to stop music
    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Background music stopped");
        }
    }

    // Public method to adjust volume
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume); // Ensure volume is between 0 and 1
        
        if (audioSource != null)
        {
            audioSource.volume = volume;
            Debug.Log($"Background music volume set to {volume}");
        }
    }

    // Public method to change the music clip
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
                Debug.Log($"Changed background music to {newClip.name}");
            }
        }
    }
}
