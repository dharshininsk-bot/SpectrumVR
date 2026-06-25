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
        // Automatically find the controller if not assigned
        if (tutorialController == null)
        {
            tutorialController = FindFirstObjectByType<RobotTutorialController>();
        }

        // Ensure Box Collider is set up as a trigger
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && !boxCollider.isTrigger)
        {
            boxCollider.isTrigger = true;
            Debug.LogWarning($"[SpaceTrigger] Box Collider on {gameObject.name} was not set to 'Is Trigger'. Automatically enabled it.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
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

        if (isUser)
        {
            tutorialController.OnUserEnteredSpace(spaceIndex);
        }
    }
}
