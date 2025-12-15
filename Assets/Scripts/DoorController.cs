using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Animator doorAnimator;
    private bool isOpen = false;

    public float closeDelay = 10f;

    [Header("Door Audio")]
    public AudioSource audioSource;          // assign in inspector
    public AudioClip[] doorOpenClips;        // array of 15 clips

    private void Start()
    {
        doorAnimator = GetComponent<Animator>();

        // Safety check
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpen)
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;

        doorAnimator.ResetTrigger("Door_Close");  // prevent conflicts
        doorAnimator.SetTrigger("Door_Open");

        // Play random door open sound
        PlayRandomDoorSound();

        StartCoroutine(CloseDoorAfterDelay());
    }

    private void PlayRandomDoorSound()
    {
        if (doorOpenClips == null || doorOpenClips.Length == 0) return;

        int index = Random.Range(0, doorOpenClips.Length);
        audioSource.clip = doorOpenClips[index];
        audioSource.Play();
    }

    private System.Collections.IEnumerator CloseDoorAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);

        doorAnimator.ResetTrigger("Door_Open");
        doorAnimator.SetTrigger("Door_Close");

        yield return new WaitForSeconds(1f); // wait for close animation

        isOpen = false;
    }
}
