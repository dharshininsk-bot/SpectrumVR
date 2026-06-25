using System.Collections;
using UnityEngine;

public class RobotTutorialController : MonoBehaviour
{
    public enum TutorialState
    {
        Idle,
        MovingToSpace,
        WaitingForUser,
        Explaining,
        Completed
    }

    [Header("Robot Configuration")]
    [Tooltip("The Transform of the robot that will move. If null, uses this GameObject.")]
    [SerializeField] private Transform robotTransform;
    
    [Tooltip("Movement speed of the robot.")]
    [SerializeField] private float movementSpeed = 2.5f;

    [Tooltip("How close the robot needs to get to the waiting point to be considered arrived.")]
    [SerializeField] private float arrivalThreshold = 0.1f;

    [Tooltip("If your robot's face is on the back or side, adjust this offset (e.g., 180 on Y if it faces backward).")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0, 180, 0);

    [Header("Tutorial Spaces Setup")]
    [Tooltip("The exact spots (Transforms) where the robot should wait for each of the 4 spaces.")]
    [SerializeField] private Transform[] robotWaitingPoints = new Transform[4];

    [Header("Explanation Settings")]
    [Tooltip("How long the robot will spend 'explaining' (printing) before moving to the next space.")]
    [SerializeField] private float explanationDuration = 5.0f;

    [Header("User Reference (Optional check for trigger)")]
    [Tooltip("The user/player transform to detect. If left null, any object tagged 'Player' will trigger the space.")]
    [SerializeField] private Transform userTransform;

    private int currentSpaceIndex = 0; // 0 = Additive, 1 = TV, 2 = Subtractive, 3 = Printer
    private TutorialState currentState = TutorialState.Idle;
    private float explanationTimer = 0f;
    private RobotLookAt robotLookAtComponent;

    // Read-only properties for external scripts (like SpaceTrigger)
    public Transform UserTransform => userTransform;
    public int CurrentSpaceIndex => currentSpaceIndex;
    public TutorialState CurrentState => currentState;

    private void Start()
    {
        if (robotTransform == null)
        {
            robotTransform = transform;
        }

        // Try to find the RobotLookAt component so we can manage it during movement
        robotLookAtComponent = robotTransform.GetComponent<RobotLookAt>();

        // Validate waiting points
        for (int i = 0; i < robotWaitingPoints.Length; i++)
        {
            if (robotWaitingPoints[i] == null)
            {
                Debug.LogError($"[RobotTutorial] Waiting Point for Space {i + 1} is not assigned in the inspector!", this);
            }
        }

        // Start the tutorial by moving to the first space
        StartMovingToSpace(0);
    }

    private void Update()
    {
        switch (currentState)
        {
            case TutorialState.MovingToSpace:
                MoveRobotTowardsTarget();
                break;

            case TutorialState.Explaining:
                UpdateExplanation();
                break;
        }
    }

    private void StartMovingToSpace(int spaceIndex)
    {
        if (spaceIndex >= robotWaitingPoints.Length || robotWaitingPoints[spaceIndex] == null)
        {
            Debug.Log("[RobotTutorial] All spaces completed! Tutorial finished.");
            currentState = TutorialState.Completed;
            return;
        }

        currentSpaceIndex = spaceIndex;
        currentState = TutorialState.MovingToSpace;

        string spaceName = GetSpaceName(spaceIndex);
        Debug.Log($"[RobotTutorial] robo moving to {spaceIndex + 1} ({spaceName})");

        // Temporarily disable RobotLookAt while moving, so the robot can face the direction it is traveling
        if (robotLookAtComponent != null)
        {
            robotLookAtComponent.enabled = false;
        }
    }

    private void MoveRobotTowardsTarget()
    {
        Vector3 targetPosition = robotWaitingPoints[currentSpaceIndex].position;
        
        // Move the robot towards the target position
        robotTransform.position = Vector3.MoveTowards(robotTransform.position, targetPosition, movementSpeed * Time.deltaTime);

        // Smoothly rotate to face the direction of movement
        Vector3 moveDirection = targetPosition - robotTransform.position;
        moveDirection.y = 0; // Keep rotation horizontal
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // Apply the model's rotation offset (e.g. if the face is on the back)
            targetRotation *= Quaternion.Euler(rotationOffset);
            robotTransform.rotation = Quaternion.Slerp(robotTransform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        // Check if arrived
        if (Vector3.Distance(robotTransform.position, targetPosition) <= arrivalThreshold)
        {
            OnArrivedAtSpace();
        }
    }

    private void OnArrivedAtSpace()
    {
        currentState = TutorialState.WaitingForUser;
        string spaceName = GetSpaceName(currentSpaceIndex);
        Debug.Log($"[RobotTutorial] robo came to {currentSpaceIndex + 1} ({spaceName})");

        // Re-enable RobotLookAt so the robot turns to face/track the user while waiting
        if (robotLookAtComponent != null)
        {
            robotLookAtComponent.enabled = true;
        }
    }

    public void OnUserEnteredSpace(int spaceIndex)
    {
        // We only care if the user enters the space the robot is currently waiting at
        if (currentState == TutorialState.WaitingForUser && spaceIndex - 1 == currentSpaceIndex)
        {
            string spaceName = GetSpaceName(currentSpaceIndex);
            Debug.Log($"[RobotTutorial] user came inside {spaceIndex} ({spaceName})");
            
            currentState = TutorialState.Explaining;
            explanationTimer = 0f;
        }
    }

    private void UpdateExplanation()
    {
        explanationTimer += Time.deltaTime;
        if (explanationTimer >= explanationDuration)
        {
            string spaceName = GetSpaceName(currentSpaceIndex);
            Debug.Log($"[RobotTutorial] robo finished explaining Space {currentSpaceIndex + 1} ({spaceName})");
            
            // Move to the next space
            StartMovingToSpace(currentSpaceIndex + 1);
        }
    }

    private string GetSpaceName(int index)
    {
        switch (index)
        {
            case 0: return "Additive teaching";
            case 1: return "TV real life example";
            case 2: return "Subtractive teaching";
            case 3: return "Printer example";
            default: return "Unknown Space";
        }
    }
}
