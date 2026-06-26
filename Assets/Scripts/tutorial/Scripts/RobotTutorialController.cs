using System.Collections;
using UnityEngine;
using UnityEngine.Video;

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

    [Header("Media Setup (Two Players, One Render Texture)")]
    [Tooltip("The Video Player that holds your looping Blinking/Idle video clip.")]
    [SerializeField] private VideoPlayer idleVideoPlayer;

    [Tooltip("The Video Player that holds your looping Talking video clip.")]
    [SerializeField] private VideoPlayer talkingVideoPlayer;

    [Tooltip("The Audio Source component attached to the robot for voiceovers.")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("Voiceover Tracks")]
    [Tooltip("Assign your 4 audio files here in sequence (0=Additive, 1=TV, 2=Subtractive, 3=Printer).")]
    [SerializeField] private AudioClip[] spaceVoiceovers = new AudioClip[4];

    [Header("User Reference (Optional check for trigger)")]
    [Tooltip("The user/player transform to detect. If left null, any object tagged 'Player' will trigger the space.")]
    [SerializeField] private Transform userTransform;

    private int currentSpaceIndex = 0; 
    private TutorialState currentState = TutorialState.Idle;
    private RobotLookAt robotLookAtComponent;

    public Transform UserTransform => userTransform;
    public int CurrentSpaceIndex => currentSpaceIndex;
    public TutorialState CurrentState => currentState;

    private void Start()
    {
        if (robotTransform == null) robotTransform = transform;

        robotLookAtComponent = robotTransform.GetComponent<RobotLookAt>();
        if (voiceAudioSource == null) voiceAudioSource = robotTransform.GetComponent<AudioSource>();

        // Ensure proper starting state for the video faces
        InitializeVideoPlayers();

        // Start the tutorial sequence
        StartMovingToSpace(0);
    }

    private void Update()
    {
        switch (currentState)
        {
            case TutorialState.MovingToSpace:
                MoveRobotTowardsTarget();
                break;
        }
    }

    private void InitializeVideoPlayers()
    {
        if (idleVideoPlayer != null && talkingVideoPlayer != null)
        {
            // Set both to loop internally
            idleVideoPlayer.isLooping = true;
            talkingVideoPlayer.isLooping = true;

            // Start with only the blinking/idle face running
            idleVideoPlayer.Play();
            talkingVideoPlayer.Stop(); 
        }
        else
        {
            Debug.LogError("[RobotTutorial] Please assign BOTH the Idle Video Player and Talking Video Player in the Inspector!", this);
        }
    }

    private void StartMovingToSpace(int spaceIndex)
    {
        if (spaceIndex >= robotWaitingPoints.Length || robotWaitingPoints[spaceIndex] == null)
        {
            Debug.Log("[RobotTutorial] All spaces completed! Tutorial finished.");
            currentState = TutorialState.Completed;
            
            // Revert back to happy blinking face at the end
            if (idleVideoPlayer != null) idleVideoPlayer.Play();
            if (talkingVideoPlayer != null) talkingVideoPlayer.Stop();
            return;
        }

        currentSpaceIndex = spaceIndex;
        currentState = TutorialState.MovingToSpace;

        if (robotLookAtComponent != null) robotLookAtComponent.enabled = false;
    }

    private void MoveRobotTowardsTarget()
    {
        Vector3 targetPosition = robotWaitingPoints[currentSpaceIndex].position;
        robotTransform.position = Vector3.MoveTowards(robotTransform.position, targetPosition, movementSpeed * Time.deltaTime);

        Vector3 moveDirection = targetPosition - robotTransform.position;
        moveDirection.y = 0; 
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(rotationOffset);
            robotTransform.rotation = Quaternion.Slerp(robotTransform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        if (Vector3.Distance(robotTransform.position, targetPosition) <= arrivalThreshold)
        {
            OnArrivedAtSpace();
        }
    }

    private void OnArrivedAtSpace()
    {
        currentState = TutorialState.WaitingForUser;
        if (robotLookAtComponent != null) robotLookAtComponent.enabled = true;
    }

    public void OnUserEnteredSpace(int spaceIndex)
    {
        if (currentState == TutorialState.WaitingForUser && (spaceIndex - 1) == currentSpaceIndex)
        {
            currentState = TutorialState.Explaining;
            StartCoroutine(PlayExplanationSequence());
        }
    }

    private IEnumerator PlayExplanationSequence()
    {
        string spaceName = GetSpaceName(currentSpaceIndex);
        Debug.Log($"[RobotTutorial] Robo starting speech for Space {currentSpaceIndex + 1} ({spaceName})");

        // 1. SWAP FACE: Stop the idle player, start the talking player
        if (idleVideoPlayer != null) idleVideoPlayer.Stop();
        if (talkingVideoPlayer != null) talkingVideoPlayer.Play();

        // 2. Play the matching audio clip
        if (voiceAudioSource != null && currentSpaceIndex < spaceVoiceovers.Length && spaceVoiceovers[currentSpaceIndex] != null)
        {
            AudioClip currentClip = spaceVoiceovers[currentSpaceIndex];
            voiceAudioSource.clip = currentClip;
            voiceAudioSource.Play();

            // 3. Keep running the talking video until the audio timeline track finishes completely
            yield return new WaitWhile(() => voiceAudioSource.isPlaying);
        }
        else
        {
            Debug.LogWarning($"[RobotTutorial] Audio track missing for Space {currentSpaceIndex + 1}! Defaulting to 5 seconds.");
            yield return new WaitForSeconds(5.0f);
        }

        Debug.Log($"[RobotTutorial] Robo finished audio for Space {currentSpaceIndex + 1}. Swapping back to idle face.");

        // 4. SWAP FACE BACK: Stop talking player, resume blinking/idle player
        if (talkingVideoPlayer != null) talkingVideoPlayer.Stop();
        if (idleVideoPlayer != null) idleVideoPlayer.Play();

        // 5. Advance automatically to the next target point
        StartMovingToSpace(currentSpaceIndex + 1);
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