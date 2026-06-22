using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeRoom
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public class SocketHighlight : MonoBehaviour
    {
        public enum HighlightMode
        {
            GameObjectToggle,
            MaterialSwap,
            EmissionToggle
        }

        [Header("Highlight Settings")]
        [Tooltip("How the socket should be highlighted.")]
        public HighlightMode highlightMode = HighlightMode.EmissionToggle;

        [Header("GameObject Toggle Settings")]
        [Tooltip("GameObject to activate when highlighted (e.g. an outline mesh, hover visual).")]
        public GameObject highlightObject;

        [Header("Material Settings")]
        [Tooltip("The renderer(s) to apply the highlight to. If left empty, will search this object and its children.")]
        public List<Renderer> targetRenderers = new List<Renderer>();

        [Header("Material Swap Settings")]
        [Tooltip("The material to apply when highlighted.")]
        public Material highlightMaterial;

        [Header("Emission Settings")]
        [Tooltip("The emission color to apply when highlighted.")]
        [ColorUsage(true, true)]
        public Color highlightColor = Color.yellow;
        
        [Tooltip("The normal emission color of the material.")]
        [ColorUsage(true, true)]
        public Color normalColor = Color.clear;

        [Header("Activation Triggers")]
        [Tooltip("Highlight the socket when a lens is hovering near it (ready to snap)?")]
        public bool highlightOnHover = true;

        [Tooltip("Highlight the socket when a lens is snapped inside it?")]
        public bool highlightOnSelect = false;

        private XRSocketInteractor socketInteractor;
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private Dictionary<Renderer, Material[]> highlightMaterials = new Dictionary<Renderer, Material[]>();
        private bool isHighlighted = false;

        private void Awake()
        {
            socketInteractor = GetComponent<XRSocketInteractor>();

            // If no renderers are explicitly assigned, find them on this object and children
            if (targetRenderers.Count == 0)
            {
                targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
            }

            CacheMaterials();
            
            // Ensure highlight object is initially disabled
            if (highlightMode == HighlightMode.GameObjectToggle && highlightObject != null)
            {
                highlightObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (socketInteractor != null)
            {
                if (highlightOnHover)
                {
                    socketInteractor.hoverEntered.AddListener(OnHoverEntered);
                    socketInteractor.hoverExited.AddListener(OnHoverExited);
                }
                if (highlightOnSelect)
                {
                    socketInteractor.selectEntered.AddListener(OnSelectEntered);
                    socketInteractor.selectExited.AddListener(OnSelectExited);
                }
            }
        }

        private void OnDisable()
        {
            if (socketInteractor != null)
            {
                socketInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                socketInteractor.hoverExited.RemoveListener(OnHoverExited);
                socketInteractor.selectEntered.RemoveListener(OnSelectEntered);
                socketInteractor.selectExited.RemoveListener(OnSelectExited);
            }
            
            // Only revert highlights if we are playing to avoid prefab and editor errors
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
            // Only turn off highlight if there isn't an item currently selected/snapped (or if highlightOnSelect is false)
            if (!socketInteractor.hasSelection || !highlightOnSelect)
            {
                SetHighlightActive(false);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            SetHighlightActive(true);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
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
