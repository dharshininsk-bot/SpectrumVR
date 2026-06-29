using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EscapeRoom.Tutorial
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class PaintBlob : MonoBehaviour
    {
        [Header("Paint Settings")]
        [Tooltip("The current color of this paint blob.")]
        public Color currentColor = Color.white;

        [Tooltip("The tag of the model parts that can be painted (e.g., 'Paintable'). If empty, it paints anything with a Renderer.")]
        public string paintableTag = "";

        [Header("Visuals")]
        [Tooltip("The renderer of this paint blob to show its color. If empty, will try to get the first Renderer on this object.")]
        public Renderer blobRenderer;

        private void Start()
        {
            if (blobRenderer == null)
            {
                blobRenderer = GetComponentInChildren<Renderer>();
            }
            
            UpdateVisualColor();
        }

        public void SetColor(Color newColor)
        {
            currentColor = newColor;
            UpdateVisualColor();
        }

        private void UpdateVisualColor()
        {
            if (blobRenderer != null)
            {
                // Create a material instance so we don't overwrite shared materials
                blobRenderer.material.color = currentColor;
                
                // If it has emission, update emission as well
                if (blobRenderer.material.HasProperty("_EmissionColor"))
                {
                    blobRenderer.material.SetColor("_EmissionColor", currentColor);
                    blobRenderer.material.EnableKeyword("_EMISSION");
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if we hit the specific Sketchfab model or any paintable object
            if (string.IsNullOrEmpty(paintableTag) || collision.gameObject.CompareTag(paintableTag))
            {
                Renderer targetRenderer = collision.gameObject.GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    // Check if it's not another paint blob or a palette slot
                    if (collision.gameObject.GetComponent<PaintBlob>() == null && 
                        collision.gameObject.GetComponent<PaletteSlot>() == null)
                    {
                        PaintTarget(targetRenderer);
                    }
                }
            }
        }

        private void PaintTarget(Renderer targetRenderer)
        {
            // Apply color to the target
            if (targetRenderer.material.HasProperty("_BaseColor"))
            {
                // URP/HDRP Lit material
                targetRenderer.material.SetColor("_BaseColor", currentColor);
            }
            else if (targetRenderer.material.HasProperty("_Color"))
            {
                // Standard material
                targetRenderer.material.color = currentColor;
            }

            Debug.Log($"[PaintBlob] Painted {targetRenderer.gameObject.name} with color {currentColor}");
        }
    }
}
