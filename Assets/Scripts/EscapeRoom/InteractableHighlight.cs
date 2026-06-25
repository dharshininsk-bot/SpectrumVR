using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeRoom;

[RequireComponent(typeof(XRBaseInteractable))]
public class InteractableHighlight : MonoBehaviour
{
    public enum HighlightMode
    {
        GameObjectToggle,
        MaterialSwap,
        EmissionToggle
    }

    [Header("Highlight Settings")]
    [Tooltip("How the interactable should be highlighted when hovered.")]
    public HighlightMode highlightMode = HighlightMode.EmissionToggle;

    [Header("GameObject Toggle Settings")]
    [Tooltip("GameObject to activate when hovered (e.g. an outline mesh or child visual).")]
    public GameObject highlightObject;

    [Header("Material Settings")]
    [Tooltip("The renderer(s) to apply the highlight to. If left empty, will search this object and its children.")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Header("Material Swap Settings")]
    [Tooltip("The material to apply when hovered.")]
    public Material highlightMaterial;

    [Header("Emission Settings")]
    [Tooltip("The emission color to apply when highlighted.")]
    [ColorUsage(true, true)]
    public Color highlightColor = Color.yellow;
    
    [Tooltip("The normal emission color of the material.")]
    [ColorUsage(true, true)]
    public Color normalColor = Color.clear;

    [Header("Activation Triggers")]
    [Tooltip("Should it highlight when the controller points at it (hover)?")]
    public bool highlightOnHover = true;

    [Tooltip("Should it highlight when the controller clicks/grabs it (select)?")]
    public bool highlightOnSelect = true;

    private XRBaseInteractable interactable;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> highlightMaterials = new Dictionary<Renderer, Material[]>();
    private bool isHighlighted = false;

    private XRSocketInteractor lastSocket = null;
    private GameObject startingSocket = null;
    private GameObject safeSocket = null;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        // If this is a lens, dynamically configure it
        if (gameObject.name.StartsWith("Lens_") && !gameObject.name.Contains("Socket"))
        {
            highlightOnSelect = true;

            // Ensure BoxCollider is not a trigger so XR controllers can hover/grab it
            var boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = false;
            }

            // Ensure TintedLens component is attached and configured
            var tintedLens = GetComponent<EscapeRoom.TintedLens>();
            if (tintedLens == null)
            {
                tintedLens = gameObject.AddComponent<EscapeRoom.TintedLens>();
                
                string objName = gameObject.name;
                if (objName.Contains("Blue"))
                {
                    tintedLens.lensColor = EscapeRoom.LensColor.Blue;
                    tintedLens.filterColor = new Color(0f, 0f, 1f, 1f);
                }
                else if (objName.Contains("Yellow"))
                {
                    tintedLens.lensColor = EscapeRoom.LensColor.Yellow;
                    tintedLens.filterColor = new Color(1f, 1f, 0f, 1f);
                }
                else if (objName.Contains("Cyan"))
                {
                    tintedLens.lensColor = EscapeRoom.LensColor.Cyan;
                    tintedLens.filterColor = new Color(0f, 1f, 1f, 1f);
                }
                else if (objName.Contains("Orange"))
                {
                    tintedLens.lensColor = EscapeRoom.LensColor.Orange;
                    tintedLens.filterColor = new Color(1f, 0.5f, 0f, 1f);
                }
                else if (objName.Contains("Magenta"))
                {
                    tintedLens.lensColor = EscapeRoom.LensColor.Magenta;
                    tintedLens.filterColor = new Color(1f, 0f, 1f, 1f);
                }
            }
        }

        // If no renderers are explicitly assigned, find them on this object and children
        if (targetRenderers.Count == 0)
        {
            targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
            Debug.Log($"[InteractableHighlight] Found {targetRenderers.Count} renderers on {gameObject.name} and children.", this);
        }

        // Cache original materials and prepare highlight materials if needed
        CacheMaterials();
        
        // Ensure highlight object is initially disabled
        if (highlightMode == HighlightMode.GameObjectToggle && highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (gameObject.name.StartsWith("Lens_") && !gameObject.name.Contains("Socket"))
        {
            startingSocket = FindStartingSocket();
            safeSocket = FindSafeSocket();
        }
    }

    private GameObject FindStartingSocket()
    {
        var sockets = GameObject.FindObjectsOfType<XRSocketInteractor>();
        XRSocketInteractor closest = null;
        float minDist = float.MaxValue;
        foreach (var socket in sockets)
        {
            if (socket.name.StartsWith("Lens_Socket") && !socket.name.Contains("safe"))
            {
                float dist = Vector3.Distance(transform.position, socket.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = socket;
                }
            }
        }
        return closest != null ? closest.gameObject : null;
    }

    private GameObject FindSafeSocket()
    {
        if (startingSocket == null) return null;
        
        string socketName = startingSocket.name;
        string numberStr = "";
        for (int i = 0; i < socketName.Length; i++)
        {
            if (char.IsDigit(socketName[i]))
            {
                numberStr += socketName[i];
            }
        }

        if (string.IsNullOrEmpty(numberStr)) return null;

        string targetSocketName = "Lens_Socket_safe" + numberStr;
        return GameObject.Find(targetSocketName);
    }

    private System.Collections.IEnumerator SocketNextFrame(XRSocketInteractor targetSocket, XRInteractionManager manager)
    {
        yield return new WaitForEndOfFrame();

        if (manager != null && targetSocket != null && interactable != null)
        {
            // Safely deselect from hand/ray controllers to prepare for snapping
            var controllersToDeselect = new List<IXRSelectInteractor>();
            foreach (var interactor in interactable.interactorsSelecting)
            {
                if (interactor != null && !(interactor is XRSocketInteractor))
                {
                    controllersToDeselect.Add(interactor);
                }
            }
            foreach (var interactor in controllersToDeselect)
            {
                manager.SelectExit(interactor, interactable);
            }

            // Instantly position and orient at the target socket to prevent collision/physics glitches
            Transform targetTransform = targetSocket.attachTransform != null ? targetSocket.attachTransform : targetSocket.transform;
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;

            // Stabilize Rigidbody physics
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Socket programmatically
            manager.SelectEnter((IXRSelectInteractor)targetSocket, (IXRSelectInteractable)interactable);
            SetHighlightActive(false);
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            if (highlightOnHover)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.hoverExited.AddListener(OnHoverExited);
            }
            if (highlightOnSelect)
            {
                interactable.selectEntered.AddListener(OnSelectEntered);
                interactable.selectExited.AddListener(OnSelectExited);
            }
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }
        
        // Only revert highlights if we are playing to avoid prefab and editor errors
        if (Application.isPlaying)
        {
            SetHighlightActive(false);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[InteractableHighlight] Hover Entered on {gameObject.name} by interactor {args.interactorObject.transform.name}", this);
        SetHighlightActive(true);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"[InteractableHighlight] Hover Exited on {gameObject.name}", this);
        SetHighlightActive(false);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[InteractableHighlight] Clicked/Selected {gameObject.name} by interactor {args.interactorObject.transform.name}", this);
        
        if (gameObject.name.StartsWith("Lens_") && !gameObject.name.Contains("Socket"))
        {
            // If grabbed by a controller (not a socket)
            if (!(args.interactorObject is XRSocketInteractor))
            {
                // Determine which socket we are currently at (closest one)
                bool isAtSafeSocket = false;
                if (safeSocket != null && startingSocket != null)
                {
                    float distToSafe = Vector3.Distance(transform.position, safeSocket.transform.position);
                    float distToStarting = Vector3.Distance(transform.position, startingSocket.transform.position);
                    isAtSafeSocket = (distToSafe < distToStarting);
                }
                
                GameObject targetSocketObj = isAtSafeSocket ? startingSocket : safeSocket;
                if (targetSocketObj != null)
                {
                    var targetSocket = targetSocketObj.GetComponent<XRSocketInteractor>();
                    if (targetSocket != null)
                    {
                        StartCoroutine(SocketNextFrame(targetSocket, interactable.interactionManager));
                    }
                }
            }
            else
            {
                // Snap selection by a socket - make sure lens itself doesn't highlight
                SetHighlightActive(false);
                
                // Record the socket
                lastSocket = args.interactorObject as XRSocketInteractor;
                
                // Turn OFF guidance highlight
                TriggerGuidanceHighlight(lastSocket.gameObject.name, false);
            }
        }
        else
        {
            // Default select highlight behavior for non-lens objects
            SetHighlightActive(true);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"[InteractableHighlight] Deselected/Released {gameObject.name}", this);
        
        if (gameObject.name.StartsWith("Lens_") && !gameObject.name.Contains("Socket"))
        {
            // Turn off frame highlight
            SetHighlightActive(false);
            
            // If dropped (not entering another select interactor)
            if (!interactable.isSelected || interactable.interactorsSelecting.Count == 0)
            {
                // Turn off guidance highlights
                if (lastSocket != null)
                {
                    TriggerGuidanceHighlight(lastSocket.gameObject.name, false);
                }
                else if (startingSocket != null)
                {
                    TriggerGuidanceHighlight(startingSocket.name, false);
                }
            }
        }
        else
        {
            // Default select exit behavior
            SetHighlightActive(false);
        }
    }

    private void TriggerGuidanceHighlight(string socketName, bool enable)
    {
        // Extract numbers from socket name (e.g. Lens_Socket1 -> 1)
        string numberStr = "";
        for (int i = 0; i < socketName.Length; i++)
        {
            if (char.IsDigit(socketName[i]))
            {
                numberStr += socketName[i];
            }
        }

        if (string.IsNullOrEmpty(numberStr)) return;

        string targetSocketName;
        if (socketName.Contains("safe"))
        {
            targetSocketName = "Lens_Socket" + numberStr;
        }
        else
        {
            targetSocketName = "Lens_Socket_safe" + numberStr;
        }

        var targetSocketObj = GameObject.Find(targetSocketName);
        if (targetSocketObj != null)
        {
            var highlight = targetSocketObj.GetComponent<SocketHighlight>();
            if (highlight != null)
            {
                highlight.SetGuidanceHighlight(enable);
            }
        }
    }

    private void CacheMaterials()
    {
        foreach (var rend in targetRenderers)
        {
            if (rend == null) continue;
            originalMaterials[rend] = rend.sharedMaterials;

            if (highlightMode == HighlightMode.MaterialSwap && highlightMaterial != null)
            {
                Material[] swapped = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < swapped.Length; i++)
                {
                    swapped[i] = highlightMaterial;
                }
                highlightMaterials[rend] = swapped;
            }
        }
    }

    private void SetHighlightActive(bool active)
    {
        if (!Application.isPlaying) return;
        if (active == isHighlighted) return;
        isHighlighted = active;

        switch (highlightMode)
        {
            case HighlightMode.GameObjectToggle:
                if (highlightObject != null)
                {
                    highlightObject.SetActive(active);
                }
                break;

            case HighlightMode.MaterialSwap:
                if (highlightMaterial != null)
                {
                    foreach (var rend in targetRenderers)
                    {
                        if (rend == null) continue;
                        
                        // Verify we have cached materials to swap back to
                        if (highlightMaterials.ContainsKey(rend) && originalMaterials.ContainsKey(rend))
                        {
                            rend.materials = active ? highlightMaterials[rend] : originalMaterials[rend];
                        }
                    }
                }
                break;

            case HighlightMode.EmissionToggle:
                foreach (var rend in targetRenderers)
                {
                    if (rend == null) continue;
                    
                    var mats = rend.materials;
                    if (mats == null) continue;

                    foreach (var mat in mats)
                    {
                        if (mat == null) continue;
                        if (active)
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", highlightColor);
                        }
                        else
                        {
                            mat.SetColor("_EmissionColor", normalColor);
                            // If the normal color is clear or black, we can disable keyword
                            if (normalColor == Color.clear || normalColor == Color.black)
                            {
                                mat.DisableKeyword("_EMISSION");
                            }
                        }
                    }
                }
                break;
        }
    }
}
