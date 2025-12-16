using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Animator doorAnimator;

    // Tracks whether the door is currently open
    private bool isOpen = false;

    // How long the door stays open before automatically closing
    public float closeDelay = 10f;

    [Header("Door Audio")]
    // AudioSource to play door sounds
    public AudioSource audioSource;          
    public AudioClip[] doorOpenClips;       

    private void Start()
    {
        doorAnimator = GetComponent<Animator>();

        // Safety check: if no AudioSource assigned, add one automatically
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the player enters the trigger and the door is not already open
        if (other.CompareTag("Player") && !isOpen)
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        // Mark door as open to prevent repeated triggers
        isOpen = true;

        // Reset the close trigger to avoid animation conflicts
        doorAnimator.ResetTrigger("Door_Close");

        // Trigger the open animation
        doorAnimator.SetTrigger("Door_Open");

        // Play random door open sound
        PlayRandomDoorSound();

        // Start coroutine to automatically close the door after a delay
        StartCoroutine(CloseDoorAfterDelay());
    }

    private void PlayRandomDoorSound()
    {
        // Safety check: exit if there are no clips
        if (doorOpenClips == null || doorOpenClips.Length == 0) return;

        // Pick a random clip from the array
        int index = Random.Range(0, doorOpenClips.Length);

        // Assign and play the chosen clip
        audioSource.clip = doorOpenClips[index];
        audioSource.Play();
    }

    private System.Collections.IEnumerator CloseDoorAfterDelay()
    {
        // Wait for the specified delay before closing the door
        yield return new WaitForSeconds(closeDelay);

        // Reset the open trigger to avoid animation conflicts
        doorAnimator.ResetTrigger("Door_Open");

        // Trigger the close animation
        doorAnimator.SetTrigger("Door_Close");

        // Wait for the close animation to complete
        yield return new WaitForSeconds(1f);

        // Mark door as closed so it can be opened again
        isOpen = false;
    }
}
