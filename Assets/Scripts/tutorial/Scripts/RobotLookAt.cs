using UnityEngine;

public class RobotLookAt : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Drag the VR camera (e.g., CenterEyeAnchor or Main Camera) here. If left empty, it will automatically find the Main Camera.")]
    [SerializeField] private Transform userTarget; 

    [Header("Rotation Settings")]
    [Tooltip("How fast the robot rotates to face the target.")]
    [SerializeField] private float rotationSpeed = 5.0f;

    [Tooltip("If true, the robot will tilt its body up/down to face the user. If false, it only rotates horizontally.")]
    [SerializeField] private bool tiltBody = true;

    [Tooltip("If your robot's face is on the back or side, adjust this offset (e.g., 180 on Y if it faces backward).")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0, 180, 0);

    private void Start()
    {
        // Automatically find the Main Camera if no target is assigned
        if (userTarget == null && Camera.main != null)
        {
            userTarget = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (userTarget == null) return;

        // Calculate the direction towards the user
        Vector3 targetDirection = userTarget.position - transform.position;

        if (!tiltBody)
        {
            // Lock rotation to the Y-axis (ignore height differences)
            targetDirection.y = 0;
        }

        // Prevent errors if the user is somehow perfectly overlapping
        if (targetDirection != Vector3.zero) 
        {
            // LookRotation with Vector3.up as the upwards parameter keeps the robot from rolling sideways
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            
            // Apply the model's rotation offset (e.g. if the face is on the back)
            targetRotation *= Quaternion.Euler(rotationOffset);

            // Smoothly rotate towards the target
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}