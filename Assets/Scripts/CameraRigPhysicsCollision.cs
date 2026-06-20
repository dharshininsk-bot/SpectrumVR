using UnityEngine;
using Unity.XR.CoreUtils;

namespace SpectrumVR
{
    /// <summary>
    /// Combines character controller driving (height/center tracking) with room-scale collision pushback.
    /// Centering the capsule under the camera can cause "double-movement" drift/jitter.
    /// This script offsets the Camera Floor Offset child transform in the opposite direction of the movement,
    /// keeping the camera's world position perfectly stable while keeping the physics capsule aligned.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(XROrigin))]
    public class CameraRigPhysicsCollision : MonoBehaviour
    {
        private XROrigin m_XROrigin;
        private CharacterController m_CharacterController;
        private Transform m_CameraTransform;
        private Transform m_FloorOffsetTransform;

        private void Start()
        {
            m_XROrigin = GetComponent<XROrigin>();
            m_CharacterController = GetComponent<CharacterController>();
            
            if (m_XROrigin != null)
            {
                if (m_XROrigin.Camera != null)
                    m_CameraTransform = m_XROrigin.Camera.transform;
                    
                if (m_XROrigin.CameraFloorOffsetObject != null)
                    m_FloorOffsetTransform = m_XROrigin.CameraFloorOffsetObject.transform;
            }

            // Fallbacks if not auto-assigned by XROrigin
            if (m_CameraTransform == null)
            {
                m_CameraTransform = Camera.main != null ? Camera.main.transform : null;
            }

            if (m_FloorOffsetTransform == null)
            {
                m_FloorOffsetTransform = transform.Find("Camera Offset") ?? transform.Find("FloorOffset");
            }

            if (m_CameraTransform == null || m_FloorOffsetTransform == null)
            {
                Debug.LogError("CameraRigPhysicsCollision: Missing Camera or Floor Offset references!");
            }
        }

        private void Update()
        {
            AlignColliderWithCamera();
        }

        private void AlignColliderWithCamera()
        {
            if (m_XROrigin == null || m_CharacterController == null || m_CameraTransform == null || m_FloorOffsetTransform == null) 
                return;

            // 1. Calculate camera position relative to the XR Origin in world space
            Vector3 cameraWorldPos = m_CameraTransform.position;
            
            // Project the camera position to the horizontal plane of the XR Origin
            Vector3 desiredCenterWorld = new Vector3(cameraWorldPos.x, transform.position.y, cameraWorldPos.z);
            Vector3 displacement = desiredCenterWorld - transform.position;

            // 2. Adjust the character controller height to match the player's height
            float cameraHeight = m_XROrigin.CameraInOriginSpaceHeight;
            m_CharacterController.height = Mathf.Max(cameraHeight, m_CharacterController.radius * 2f);
            
            // Adjust the capsule center in Y axis only
            Vector3 localCenter = Vector3.zero;
            localCenter.y = m_CharacterController.height / 2f + m_CharacterController.skinWidth;
            m_CharacterController.center = localCenter;

            // 3. Move the character controller to center it under the camera
            if (displacement.magnitude > 0.001f)
            {
                Vector3 preMovePos = transform.position;
                
                // Move the character controller (respects wall collisions)
                m_CharacterController.Move(displacement);
                
                Vector3 postMovePos = transform.position;
                Vector3 actualMove = postMovePos - preMovePos;
                
                // Crucial step to prevent double-movement and camera passing:
                // Shift the Camera Floor Offset in the opposite direction of the desired displacement.
                // This locks the camera's world position at the point of collision.
                Vector3 localShift = transform.InverseTransformDirection(displacement);
                m_FloorOffsetTransform.localPosition -= localShift;
            }
        }
    }
}
