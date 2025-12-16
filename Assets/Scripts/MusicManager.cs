using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    // Singleton instance for global access
    public static MusicManager Instance;
    // AudioSource that plays the background music
    public AudioSource musicSource;

    private void Awake()
    {
        // Singleton pattern: ensure only one MusicManager exists
        if (Instance != null)
        {
            // Destroy duplicate
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Persist across scene loads
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Start playing music automatically if an AudioSource is assigned
        if (musicSource != null)
        {
            // Loop the music indefinitely
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // Immediately stops the music
    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    // Starts fading out the music over a specified duration
    public void FadeOut(float duration)
    {
        if (musicSource != null)
            StartCoroutine(FadeOutRoutine(duration));
    }

    // Coroutine to gradually reduce volume to zero over time
    private IEnumerator FadeOutRoutine(float duration)
    {
        if (musicSource == null) yield break;

        // Record the initial volume
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Lerp volume from starting value to 0 over the duration
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);

            // Wait for next frame
            yield return null;
        }

        // Stop music completely after fade
        musicSource.Stop();

        // Reset volume for next play
        musicSource.volume = startVolume;
    }
}
