using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

namespace EscapeRoom
{
    /// <summary>
    /// Component that attaches to duplicate lens objects to handle swapping between locations
    /// (e.g. floor vs safe) upon selection, and highlighting upon pointer hover.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class LensToggleInteractable : MonoBehaviour
    {
        public enum HighlightMode
        {
            GameObjectToggle,
            MaterialSwap,
            EmissionToggle
        }

        [Header("Toggle Settings")]
        [Tooltip("The duplicate partner lens object in the other location (e.g. safe or floor).")]
        public GameObject partnerLens;

        [Header("Highlight Settings")]
        [Tooltip("Method used to highlight when hovered/pointed.")]
        public HighlightMode highlightMode = HighlightMode.EmissionToggle;

        [Tooltip("GameObject to activate when highlighted (e.g. an outline mesh or child visual).")]
        public GameObject highlightObject;

        [Tooltip("The renderer(s) to apply the highlight to. If left empty, will search this object and its children.")]
        public List<Renderer> targetRenderers = new List<Renderer>();

        [Tooltip("The material to apply when hovered (if MaterialSwap).")]
        public Material highlightMaterial;

        [ColorUsage(true, true)]
        [Tooltip("The emission color to apply when highlighted (if EmissionToggle).")]
        public Color highlightColor = Color.yellow;

        [ColorUsage(true, true)]
        [Tooltip("The normal emission color of the material (if EmissionToggle).")]
        public Color normalColor = Color.clear;

        private XRBaseInteractable interactable;
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private Dictionary<Renderer, Material[]> highlightMaterials = new Dictionary<Renderer, Material[]>();
        private bool isHighlighted = false;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();

            // Auto-gather renderers if empty
            if (targetRenderers.Count == 0)
            {
                targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
            }

            CacheMaterials();

            // Make sure highlight object is initially disabled if in GameObjectToggle mode
            if (highlightMode == HighlightMode.GameObjectToggle && highlightObject != null)
            {
                highlightObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.hoverExited.AddListener(OnHoverExited);
                interactable.selectEntered.AddListener(OnSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.hoverExited.RemoveListener(OnHoverExited);
                interactable.selectEntered.RemoveListener(OnSelectEntered);
            }

            if (Application.isPlaying)
            {
                SetHighlightActive(false);
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            SetHighlightActive(true);
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            SetHighlightActive(false);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Reset highlight before disabling the object
            SetHighlightActive(false);

            if (partnerLens != null)
            {
                // Toggle visibility: activate partner and deactivate self
                partnerLens.SetActive(true);
                
                Debug.Log($"[LensToggle] Toggled lens position. Activated {partnerLens.name}, deactivated {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[LensToggle] Cannot toggle. Partner lens is not assigned on {gameObject.name}!", this);
            }

            gameObject.SetActive(false);
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
}
