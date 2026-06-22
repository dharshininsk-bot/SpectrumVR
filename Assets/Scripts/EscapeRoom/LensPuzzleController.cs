using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

namespace EscapeRoom
{
    public class LensPuzzleController : MonoBehaviour
    {
        [Header("Required Objects")]
        [Tooltip("The list of active sockets inside the safe.")]
        [SerializeField] private List<XRSocketInteractor> safeSockets = new List<XRSocketInteractor>();

        [Tooltip("The Light component representing the spotlight inside the safe.")]
        [SerializeField] private Light safeLight;

        [Tooltip("Optional mesh renderer for a light beam volume/cone visual.")]
        [SerializeField] private MeshRenderer lightBeamRenderer;

        [Header("Puzzle Settings")]
        [Tooltip("The target color of the light needed to solve the puzzle and open the safe.")]
        [SerializeField] private Color targetColor = Color.green;

        [Tooltip("How close the combined color must be to the target color to solve the puzzle (tolerance).")]
        [SerializeField] private float colorMatchTolerance = 0.1f;

        [Tooltip("Default color when no lenses are inserted.")]
        [SerializeField] private Color defaultLightColor = Color.white;

        [Header("Events")]
        [Tooltip("Event triggered when the safe is successfully unlocked.")]
        public UnityEvent OnSafeUnlocked;

        [Tooltip("Event triggered when the light color is incorrect.")]
        public UnityEvent OnSafeLocked;

        private void OnEnable()
        {
            foreach (var socket in safeSockets)
            {
                if (socket != null)
                {
                    socket.selectEntered.AddListener(OnSocketChanged);
                    socket.selectExited.AddListener(OnSocketChanged);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var socket in safeSockets)
            {
                if (socket != null)
                {
                    socket.selectEntered.RemoveListener(OnSocketChanged);
                    socket.selectExited.RemoveListener(OnSocketChanged);
                }
            }
        }

        private void Start()
        {
            UpdatePuzzleState();
        }

        private void OnSocketChanged(BaseInteractionEventArgs args)
        {
            UpdatePuzzleState();
        }

        private void UpdatePuzzleState()
        {
            Color combinedColor = defaultLightColor;
            bool hasAnyLens = false;

            // Compute subtractive color mixing by multiplying the color values of all active lenses
            foreach (var socket in safeSockets)
            {
                if (socket != null && socket.hasSelection)
                {
                    if (socket.interactablesSelected.Count > 0)
                    {
                        var interactable = socket.interactablesSelected[0];
                        if (interactable != null && interactable.transform.TryGetComponent<TintedLens>(out var lens))
                        {
                            if (!hasAnyLens)
                            {
                                combinedColor = lens.filterColor;
                                hasAnyLens = true;
                            }
                            else
                            {
                                // Subtractive mixing: multiply the color values channel by channel
                                combinedColor.r *= lens.filterColor.r;
                                combinedColor.g *= lens.filterColor.g;
                                combinedColor.b *= lens.filterColor.b;
                            }
                        }
                    }
                }
            }

            // Set final color
            SetLightColor(combinedColor);

            // Verify target color
            if (hasAnyLens && ColorsMatch(combinedColor, targetColor))
            {
                Debug.Log($"[LensPuzzle] Combined color is {combinedColor} - Matches Target Color! Unlocking safe...");
                OnSafeUnlocked?.Invoke();
            }
            else
            {
                OnSafeLocked?.Invoke();
            }
        }

        private bool ColorsMatch(Color a, Color b)
        {
            // Compare RGB values ignoring Alpha
            float rDiff = Mathf.Abs(a.r - b.r);
            float gDiff = Mathf.Abs(a.g - b.g);
            float bDiff = Mathf.Abs(a.b - b.b);
            return (rDiff < colorMatchTolerance && gDiff < colorMatchTolerance && bDiff < colorMatchTolerance);
        }

        private void SetLightColor(Color color)
        {
            if (safeLight != null)
            {
                safeLight.color = color;
                safeLight.enabled = true;
            }

            if (lightBeamRenderer != null)
            {
                lightBeamRenderer.gameObject.SetActive(true);
                Material mat = lightBeamRenderer.material;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.3f));
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", new Color(color.r, color.g, color.b, 0.3f));
                }
            }
        }
    }
}
