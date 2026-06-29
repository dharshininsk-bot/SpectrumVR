using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeRoom.Tutorial
{
    /// <summary>
    /// Attach ONE instance of this to your Left and Right XR Controller GameObjects.
    /// It acts as a "color holder" — the controller's invisible paint hand.
    /// Palette slots and paintable objects check this to read/write the current held color.
    /// </summary>
    public class ColorWand : MonoBehaviour
    {
        [Header("State (Read Only in Inspector)")]
        [SerializeField] private bool _hasColor = false;
        [SerializeField] private Color _currentColor = Color.clear;
        [SerializeField] private bool _hasWater = false;

        [Header("Visual Feedback")]
        [Tooltip("Optional: A small sphere child object that lights up with the held color.")]
        public Renderer wandIndicatorRenderer;

        [Tooltip("Optional: A LineRenderer on the ray interactor. If assigned, its start color will reflect the held color.")]
        public LineRenderer rayLineRenderer;

        public bool HasColor => _hasColor;
        public Color CurrentColor => _currentColor;
        public bool HasWater => _hasWater;

        // Static accessors so slots/model can find the active wand without needing a reference
        public static ColorWand LeftWand;
        public static ColorWand RightWand;

        private XRBaseInteractor _interactor;

        private void Awake()
        {
            _interactor = GetComponent<XRBaseInteractor>();

            // Auto-register based on name convention
            string n = gameObject.name.ToLower();
            if (n.Contains("left"))
                LeftWand = this;
            else if (n.Contains("right"))
                RightWand = this;
        }

        private void OnDestroy()
        {
            if (LeftWand == this) LeftWand = null;
            if (RightWand == this) RightWand = null;
        }

        /// <summary>Returns the ColorWand that triggered the XR event, by checking the interactor's owner.</summary>
        public static ColorWand FromInteractor(XRBaseInteractor interactor)
        {
            if (interactor == null) return null;
            return interactor.GetComponentInParent<ColorWand>();
        }

        /// <summary>Give a color to this wand's "hand".</summary>
        public void PickUpColor(Color color)
        {
            _hasColor = true;
            _currentColor = color;
            RefreshVisuals();
            Debug.Log($"[ColorWand] {gameObject.name} picked up color: {color}");
        }

        /// <summary>Consume and return the color held by this wand.</summary>
        public Color ConsumeColor()
        {
            Color c = _currentColor;
            _hasColor = false;
            _currentColor = Color.clear;
            RefreshVisuals();
            Debug.Log($"[ColorWand] {gameObject.name} consumed color: {c}");
            return c;
        }

        /// <summary>Load water into this wand — clears any held paint color first.</summary>
        public void PickUpWater()
        {
            _hasColor = false;
            _currentColor = Color.clear;
            _hasWater = true;
            RefreshVisuals();
            Debug.Log($"[ColorWand] {gameObject.name} picked up water (slot eraser).");
        }

        /// <summary>Consume the water from this wand. Returns true if water was held.</summary>
        public bool ConsumeWater()
        {
            if (!_hasWater) return false;
            _hasWater = false;
            RefreshVisuals();
            Debug.Log($"[ColorWand] {gameObject.name} used water to clean a slot.");
            return true;
        }

        private void RefreshVisuals()
        {
            if (wandIndicatorRenderer != null)
            {
                if (_hasColor)
                {
                    // Show paint color
                    wandIndicatorRenderer.enabled = true;
                    wandIndicatorRenderer.material.color = _currentColor;
                    if (wandIndicatorRenderer.material.HasProperty("_EmissionColor"))
                    {
                        wandIndicatorRenderer.material.SetColor("_EmissionColor", _currentColor * 1.5f);
                        wandIndicatorRenderer.material.EnableKeyword("_EMISSION");
                    }
                }
                else if (_hasWater)
                {
                    // Show water — translucent blue-white
                    wandIndicatorRenderer.enabled = true;
                    Color waterColor = new Color(0.5f, 0.85f, 1f, 0.6f);
                    wandIndicatorRenderer.material.color = waterColor;
                    if (wandIndicatorRenderer.material.HasProperty("_EmissionColor"))
                    {
                        wandIndicatorRenderer.material.SetColor("_EmissionColor", waterColor * 0.8f);
                        wandIndicatorRenderer.material.EnableKeyword("_EMISSION");
                    }
                }
                else
                {
                    wandIndicatorRenderer.enabled = false;
                }
            }

            if (rayLineRenderer != null)
            {
                Color lineColor = _hasWater  ? new Color(0.5f, 0.85f, 1f)
                               : _hasColor  ? _currentColor
                               : Color.white;
                rayLineRenderer.startColor = lineColor;
                rayLineRenderer.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
            }
        }
    }
}
