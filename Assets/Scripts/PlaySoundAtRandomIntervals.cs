using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundAtRandomIntervals : MonoBehaviour
{
    public float minSeconds = 5f; // Minimum interval to wait before playing sound
    public float maxSeconds = 15f; // Maximum interval to wait before playing sound

    private AudioSource audioSource;

    private void Start()
    {
        // Get the AudioSource component attached to the same GameObject
        audioSource = GetComponent<AudioSource>();

        // Start the coroutine that plays the sound repeatedly at random intervals
        StartCoroutine(PlaySound());
    }

    // Coroutine that handles playing the sound at random intervals
    private IEnumerator PlaySound()
    {
        // Infinite loop to keep playing sounds indefinitely
        while (true)
        {
            // Pick a random interval between minSeconds and maxSeconds
            float waitTime = Random.Range(minSeconds, maxSeconds);

            // Wait for the chosen interval before playing the sound
            yield return new WaitForSeconds(waitTime);

            // Play the audio clip attached to the AudioSource
            audioSource.Play();
        }
    }
}