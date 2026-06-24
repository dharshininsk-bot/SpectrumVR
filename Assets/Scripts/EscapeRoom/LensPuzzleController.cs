using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;
using System.Collections.Generic;

namespace EscapeRoom
{
    public class LensPuzzleController : MonoBehaviour
    {
        public static LensPuzzleController Instance { get; private set; }

        [Header("Required Objects")]
        [Tooltip("The list of duplicate lenses inside the safe.")]
        [SerializeField] private List<TintedLens> safeLenses = new List<TintedLens>();

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

        private float defaultLightIntensity = -1f;  // -1 means not yet captured
        private bool hasCapturedIntensity = false;

        [Tooltip("Multiplies the light's intensity by this factor when a lens is active (helps compensate for subtractive darkening).")]
        [SerializeField] private float intensityMultiplier = 2f;

        [Tooltip("If true, automatically scales the mixed color channels so the highest channel is 1.0 (maintaining hue but maximizing brightness).")]
        [SerializeField] private bool normalizeColorBrightness = true;

        [Tooltip("Transparency alpha of the light beam visual (0 = invisible, 1 = fully opaque).")]
        [Range(0f, 1f)]
        [SerializeField] private float beamAlpha = 0.5f;

        [Header("Safe Door Animation")]
        [Tooltip("Animator that plays the 'safe_open' clip (on the safe body / lock mechanism).")]
        [SerializeField] private Animator safeDoorAnimator;

        [Tooltip("Exact name of the open animation STATE in the safeDoorAnimator controller.")]
        [SerializeField] private string safeOpenStateName = "safe_open";

        [Tooltip("Animator that plays the 'safe_door_hinge' clip (on the door pivot / hinge object).")]
        [SerializeField] private Animator safeHingeAnimator;

        [Tooltip("Exact name of the hinge animation STATE in the safeHingeAnimator controller.")]
        [SerializeField] private string safeHingeStateName = "safe_door_hinge";

        [Header("Events")]
        [Tooltip("Event triggered when the safe is successfully unlocked.")]
        public UnityEvent OnSafeUnlocked;

        [Tooltip("Event triggered when the light color is incorrect.")]
        public UnityEvent OnSafeLocked;

        // New events for door animation steps
        [Tooltip("Event fired after the safe_open animation completes.")]
        public UnityEvent OnDoorOpen;

        [Tooltip("Event fired after the safe_door_hinge animation completes.")]
        public UnityEvent OnDoorHinge;

        private bool puzzleSolved = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            // Disable BOTH animators immediately so nothing auto-plays before the puzzle is solved
            if (safeDoorAnimator != null) safeDoorAnimator.enabled = false;
            if (safeHingeAnimator != null) safeHingeAnimator.enabled = false;
        }

        private void Start()
        {
            CaptureIntensityIfNeeded();
            UpdatePuzzleState();
        }

        private void CaptureIntensityIfNeeded()
        {
            if (!hasCapturedIntensity && safeLight != null)
            {
                defaultLightIntensity = safeLight.intensity;
                hasCapturedIntensity = true;
                Debug.Log($"[LensPuzzle] Captured spotlight baseline intensity: {defaultLightIntensity}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void UpdatePuzzleState()
        {
            Color combinedColor = defaultLightColor;
            bool hasAnyLens = false;

            // Compute subtractive color mixing by multiplying the color values of all active lenses in the safe
            foreach (var lens in safeLenses)
            {
                if (lens != null && lens.gameObject.activeInHierarchy)
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

            // Set final color
            SetLightColor(combinedColor, hasAnyLens);

            // Verify target color
            if (hasAnyLens && ColorsMatch(combinedColor, targetColor))
            {
                if (!puzzleSolved)
                {
                    puzzleSolved = true;
                    Debug.Log($"[LensPuzzle] Combined color is {combinedColor} - Matches Target Color! Unlocking safe...");
                    OnSafeUnlocked?.Invoke();
                    StartCoroutine(PlaySafeOpenAndHingeAnimation());
                }
            }
            else
            {
                OnSafeLocked?.Invoke();
            }
        }

        /// <summary>
        /// Plays safe_open (via safeDoorAnimator) then safe_door_hinge (via safeHingeAnimator) in sequence.
        /// </summary>
        private IEnumerator PlaySafeOpenAndHingeAnimation()
        {
            // ── Step 1: safe_open ────────────────────────────────────────────
            if (safeDoorAnimator != null)
            {
                safeDoorAnimator.enabled = true;
                safeDoorAnimator.speed   = 1f;
                yield return null; // let Animator wake up

                safeDoorAnimator.Play(safeOpenStateName, 0, 0f);
                yield return null; // let state register

                AnimatorStateInfo openInfo;
                do
                {
                    yield return null;
                    openInfo = safeDoorAnimator.GetCurrentAnimatorStateInfo(0);
                } while (!openInfo.IsName(safeOpenStateName) || openInfo.normalizedTime < 1f);

                safeDoorAnimator.enabled = false; // freeze door in open pose
                Debug.Log("[LensPuzzle] safe_open animation completed.");
                OnDoorOpen?.Invoke();
            }
            else
            {
                Debug.LogWarning("[LensPuzzle] safeDoorAnimator not assigned – skipping safe_open.");
            }

            // ── Step 2: safe_door_hinge ──────────────────────────────────────
            if (safeHingeAnimator != null)
            {
                safeHingeAnimator.enabled = true;
                safeHingeAnimator.speed   = 1f;
                yield return null; // let Animator wake up

                safeHingeAnimator.Play(safeHingeStateName, 0, 0f);
                yield return null; // let state register

                AnimatorStateInfo hingeInfo;
                do
                {
                    yield return null;
                    hingeInfo = safeHingeAnimator.GetCurrentAnimatorStateInfo(0);
                } while (!hingeInfo.IsName(safeHingeStateName) || hingeInfo.normalizedTime < 1f);

                safeHingeAnimator.enabled = false; // freeze door in swung-open pose
                Debug.Log("[LensPuzzle] safe_door_hinge animation completed.");
                OnDoorHinge?.Invoke();
            }
            else
            {
                Debug.LogWarning("[LensPuzzle] safeHingeAnimator not assigned – skipping safe_door_hinge.");
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

        private void SetLightColor(Color color, bool hasAnyLens)
        {
            // Always capture intensity on-demand before first write, in case called before Start()
            CaptureIntensityIfNeeded();

            Color finalColor = color;

            if (normalizeColorBrightness && hasAnyLens)
            {
                float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                if (maxChannel > 0f)
                {
                    finalColor.r /= maxChannel;
                    finalColor.g /= maxChannel;
                    finalColor.b /= maxChannel;
                }
            }

            if (safeLight != null)
            {
                safeLight.color = finalColor;
                float targetIntensity = hasAnyLens
                    ? (defaultLightIntensity * intensityMultiplier)
                    : defaultLightIntensity;
                safeLight.intensity = targetIntensity;
                safeLight.enabled = true;
            }

            if (lightBeamRenderer != null)
            {
                lightBeamRenderer.gameObject.SetActive(true);
                Material mat = lightBeamRenderer.material;
                Color beamColor = new Color(finalColor.r, finalColor.g, finalColor.b, beamAlpha);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", beamColor);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", beamColor);
                }
            }
        }
    }
}
