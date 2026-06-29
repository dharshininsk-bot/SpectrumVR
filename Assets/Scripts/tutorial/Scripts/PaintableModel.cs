using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeRoom.Tutorial
{
    /// <summary>
    /// Attach this to each individual paintable part of the Sketchfab_model.001.
    /// Requires an XRSimpleInteractable and a Collider.
    /// When a ColorWand holding a color clicks this, the part is painted.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(Collider))]
    public class PaintableModel : MonoBehaviour
    {
        [Header("Painting")]
        [Tooltip("The target material property to set color on. Leave as default for URP Lit.")]
        public string colorProperty = "_BaseColor";

        [Tooltip("If true, painting will also update the emission to give a painted glow effect.")]
        public bool useEmissionOnPaint = false;

        [Tooltip("The Renderer to paint. If empty, finds the first Renderer on this object.")]
        public Renderer targetRenderer;

        private XRSimpleInteractable _interactable;
        private Color _originalColor;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();

            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer != null)
                _originalColor = targetRenderer.material.GetColor(colorProperty);
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.selectEntered.AddListener(OnModelClicked);
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.selectEntered.RemoveListener(OnModelClicked);
        }

        private void OnModelClicked(SelectEnterEventArgs args)
        {
            ColorWand wand = ColorWand.FromInteractor(args.interactorObject as XRBaseInteractor);
            if (wand == null)
            {
                Debug.LogWarning($"[PaintableModel] {gameObject.name} clicked but no ColorWand found on interactor.", this);
                return;
            }

            if (!wand.HasColor)
            {
                Debug.Log($"[PaintableModel] {gameObject.name} clicked but wand '{wand.gameObject.name}' has no color.", this);
                return;
            }

            // Consume the color from the wand and paint ourselves
            Color paintColor = wand.ConsumeColor();
            ApplyPaint(paintColor);
        }

        private void ApplyPaint(Color color)
        {
            if (targetRenderer == null)
            {
                Debug.LogWarning($"[PaintableModel] No Renderer found on {gameObject.name}.", this);
                return;
            }

            // Use MaterialPropertyBlock so we don't create a new material instance per-part
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);

            block.SetColor("_BaseColor", color); // URP Lit
            block.SetColor("_Color", color);      // Standard Shader fallback

            if (useEmissionOnPaint)
            {
                block.SetColor("_EmissionColor", color * 0.5f);
                targetRenderer.material.EnableKeyword("_EMISSION");
            }

            targetRenderer.SetPropertyBlock(block);

            Debug.Log($"[PaintableModel] '{gameObject.name}' painted with {color}");
        }

        /// <summary>Call this to reset the part back to its original color.</summary>
        public void ResetColor()
        {
            if (targetRenderer == null) return;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", _originalColor);
            block.SetColor("_Color", _originalColor);
            targetRenderer.SetPropertyBlock(block);
        }
    }
}
