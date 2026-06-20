using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 openPositionOffset = new Vector3(0, 3f, 0); // e.g., slides up by 3 meters
    public float speed = 2f;

    private Vector3 closedPosition;
    private Vector3 targetPosition;
    private bool isOpen = false;

    private void Start()
    {
        closedPosition = transform.localPosition;
        targetPosition = closedPosition;
    }

    private void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * speed);
    }

    public void OpenDoor()
    {
        targetPosition = closedPosition + openPositionOffset;
        isOpen = true;
    }

    public void CloseDoor()
    {
        targetPosition = closedPosition;
        isOpen = false;
    }
}
