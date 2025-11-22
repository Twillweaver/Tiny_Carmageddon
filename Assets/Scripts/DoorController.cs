using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Animator doorAnimator;
    private bool isOpen = false;

    public float closeDelay = 10f;

    private void Start()
    {
        doorAnimator = GetComponent<Animator>();
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

        StartCoroutine(CloseDoorAfterDelay());
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
