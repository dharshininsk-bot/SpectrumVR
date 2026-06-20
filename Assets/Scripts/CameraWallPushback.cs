using UnityEngine;

namespace SpectrumVR
{
    /// <summary>
    /// Prevents the player from physically walking (room-scale) through walls 
    /// by pushing the XR Origin back when the camera collides with a wall.
    /// </summary>
    public class CameraWallPushback : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The XR Origin (parent of the Camera Offset/Main Camera).")]
        public Transform xrOrigin;
        
        [Tooltip("The Main Camera (headset).")]
        public Transform mainCamera;

        [Header("Collision Settings")]
        [Tooltip("Layer mask containing the walls/obstacles.")]
        public LayerMask obstacleLayers;

        [Tooltip("Radius of the player's head collision sphere.")]
        public float headRadius = 0.15f;

        private Vector3 m_LastCameraPosition;

        private void Start()
        {
            // Auto-assign references if left empty
            if (xrOrigin == null)
            {
                // Try to find the parent or look for the XR Origin in the parent hierarchy
                xrOrigin = GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()?.transform ?? transform.parent;
            }
                
            if (mainCamera == null)
            {
                mainCamera = Camera.main != null ? Camera.main.transform : transform;
            }

            m_LastCameraPosition = mainCamera.position;
        }

        private void LateUpdate()
        {
            if (mainCamera == null || xrOrigin == null) return;

            Vector3 currentCamPos = mainCamera.position;
            Vector3 moveDirection = currentCamPos - m_LastCameraPosition;
            float moveDistance = moveDirection.magnitude;

            if (moveDistance > 0.0001f)
            {
                // Cast a sphere from the last camera position to the current camera position to see if we hit a wall
                if (Physics.SphereCast(m_LastCameraPosition, headRadius, moveDirection.normalized, out RaycastHit hit, moveDistance, obstacleLayers))
                {
                    // Calculate the point of contact
                    Vector3 contactPoint = m_LastCameraPosition + moveDirection.normalized * hit.distance;
                    
                    // The overshoot is how far the camera passed past the contact point
                    Vector3 overshoot = currentCamPos - contactPoint;
                    
                    // Push the XR Origin back in the opposite direction of the overshoot
                    // This forces the virtual camera to stop exactly at the contact point
                    xrOrigin.position -= overshoot;
                }
            }

            // Keep track of the updated camera position for the next frame
            m_LastCameraPosition = mainCamera.position;
        }
    }
}
