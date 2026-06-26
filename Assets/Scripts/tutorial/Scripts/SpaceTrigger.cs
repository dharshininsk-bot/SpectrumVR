using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SpaceTrigger : MonoBehaviour
{
    [Header("Trigger Configuration")]
    [Tooltip("The space index (1 = Additive, 2 = TV, 3 = Subtractive, 4 = Printer)")]
    [Range(1, 4)]
    [SerializeField] private int spaceIndex = 1;

    [Tooltip("Reference to the RobotTutorialController. If left empty, will search the scene.")]
    [SerializeField] private RobotTutorialController tutorialController;

    private void Awake()
    {
        if (tutorialController == null)
        {
            tutorialController = FindFirstObjectByType<RobotTutorialController>();
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && !boxCollider.isTrigger)
        {
            boxCollider.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckAndNotifyUser(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckAndNotifyUser(other);
    }

    private void CheckAndNotifyUser(Collider other)
    {
        if (tutorialController == null) return;

        bool isUser = false;

        // 1. Check if it matches the configured UserTransform
        if (tutorialController.UserTransform != null && other.transform == tutorialController.UserTransform)
        {
            isUser = true;
        }
        // 2. Or check if it has the "Player" tag
        else if (other.CompareTag("Player"))
        {
            isUser = true;
        }
        // 3. Fallback: Check if it represents the VR camera or player rig
        else if (other.GetComponentInChildren<Camera>() != null || other.name.Contains("Player") || other.name.Contains("Camera"))
        {
            isUser = true;
        }

        // ONLY print if it is confirmed to be the player entering the zone
        if (isUser)
        {
            // We only print if the robot is actually ready and waiting for this specific step
            if (tutorialController.CurrentState == RobotTutorialController.TutorialState.WaitingForUser && 
                (spaceIndex - 1) == tutorialController.CurrentSpaceIndex)
            {
                Debug.Log($"<color=green>[SpaceTrigger Success]</color> Player detected inside {gameObject.name}. Activating step {spaceIndex}!");
            }

            tutorialController.OnUserEnteredSpace(spaceIndex);
        }
    }
}