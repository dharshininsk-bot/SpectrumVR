using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this script to your torch GameObjects (or any grabbable objects).
/// It ensures that when the user releases/drops the object, it stops completely
/// and floats in space at its release position.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StayInSpaceOnDrop : MonoBehaviour
{
    [Header("Freeze Settings")]
    [Tooltip("If checked, the object will start frozen/floating in space when the scene starts.")]
    [SerializeField] private bool freezeOnStart = true;

    [Tooltip("If checked, gravity will be temporarily enabled while the object is held.")]
    [SerializeField] private bool useGravityWhileGrabbed = false;

    [Header("Grab Offset Settings")]
    [Tooltip("Adjust the position offset of the torch when held by the player.")]
    [SerializeField] private Vector3 grabPositionOffset = Vector3.zero;

    [Tooltip("Adjust the rotation offset (Euler angles) of the torch when held by the player. Adjust this to align the torch direction (e.g. project on the wall).")]
    [SerializeField] private Vector3 grabRotationOffset = Vector3.zero;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Transform customAttachTransform;

    private bool originalUseGravity;
    private bool originalIsKinematic;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (rb != null)
        {
            originalUseGravity = rb.useGravity;
            originalIsKinematic = rb.isKinematic;
        }

        SetupAttachTransform();
    }

    private void Start()
    {
        if (freezeOnStart)
        {
            Freeze();
        }
    }

    private void SetupAttachTransform()
    {
        if (grabInteractable == null) return;

        // If the grabInteractable does not have an attach transform, or if it points to the root transform,
        // we create a dynamic attach transform as a child to apply the offsets.
        if (grabInteractable.attachTransform == null || grabInteractable.attachTransform == transform)
        {
            Transform existingChild = transform.Find("DynamicGrabAttachPoint");
            if (existingChild != null)
            {
                customAttachTransform = existingChild;
            }
            else
            {
                GameObject attachObj = new GameObject("DynamicGrabAttachPoint");
                attachObj.transform.SetParent(transform, false);
                customAttachTransform = attachObj.transform;
            }
            grabInteractable.attachTransform = customAttachTransform;
        }
        else
        {
            // If they already have configured an attach transform, use it directly
            customAttachTransform = grabInteractable.attachTransform;
        }

        UpdateAttachTransform();
    }

    private void UpdateAttachTransform()
    {
        if (customAttachTransform != null)
        {
            customAttachTransform.localPosition = grabPositionOffset;
            customAttachTransform.localRotation = Quaternion.Euler(grabRotationOffset);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (customAttachTransform != null)
        {
            UpdateAttachTransform();
        }
        else if (grabInteractable != null && grabInteractable.attachTransform != null)
        {
            grabInteractable.attachTransform.localPosition = grabPositionOffset;
            grabInteractable.attachTransform.localRotation = Quaternion.Euler(grabRotationOffset);
        }
    }
#endif

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnDropped);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnDropped);
        }
    }

    /// <summary>
    /// Call this when the object is grabbed.
    /// Can also be called via UnityEvents or custom grab scripts.
    /// </summary>
    public void Grab()
    {
        if (rb != null)
        {
            rb.isKinematic = originalIsKinematic;
            rb.useGravity = useGravityWhileGrabbed;
        }
    }

    /// <summary>
    /// Call this when the object is dropped.
    /// Can also be called via UnityEvents or custom grab scripts.
    /// </summary>
    public void Drop()
    {
        Freeze();
    }

    private void Freeze()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            
            // Clear velocities to stop any physics movement instantly
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Grab();
    }

    private void OnDropped(SelectExitEventArgs args)
    {
        Drop();
    }
}