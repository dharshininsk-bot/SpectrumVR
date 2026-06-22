using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

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
        SetHighlightActive(true);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"[InteractableHighlight] Deselected/Released {gameObject.name}", this);
        SetHighlightActive(false);
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
