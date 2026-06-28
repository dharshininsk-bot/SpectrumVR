using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.XR.CoreUtils;

namespace EscapeRoom
{
    /// <summary>
    /// Attach to each stair step GameObject.
    ///
    /// Responsibilities:
    ///   - Owns the RGBButtonPanel for this step.
    ///   - Listens for OnCorrectCombination from the panel.
    ///   - When solved: glows the glass surface, teleports the player onto this step,
    ///     then tells StairManager to unlock the NEXT step.
    ///
    /// Inspector setup:
    ///   - rgbButtonPanel      → the RGBButtonPanel child of this step.
    ///   - glassPanelRenderer  → MeshRenderer of the glass surface on top of this step.
    ///   - playerStandPoint    → empty Transform placed where the player should stand
    ///                           after being teleported to this step.
    ///   - solvedEmissionColor → HDR colour the glass panel glows when the step is solved.
    ///   - solvedEmissionIntensity → HDR multiplier (1.5 – 3 works well).
    ///   - teleportDelay       → seconds to wait after solve before teleporting the player
    ///                           (gives time to see the glow animation).
    /// </summary>
    public class StairStep : MonoBehaviour
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("RGB Panel")]
        [Tooltip("The RGBButtonPanel component that is a child of this step.")]
        public RGBButtonPanel rgbButtonPanel;

        [Header("Glass Surface")]
        [Tooltip("MeshRenderer of the glass panel on top of this step (optional — for the surface light glow).")]
        public MeshRenderer glassPanelRenderer;

        [Tooltip("HDR colour the glass panel glows with once the step is solved.")]
        [ColorUsage(true, true)]
        public Color solvedEmissionColor = Color.white;

        [Tooltip("HDR multiplier for the solved glow (1.5 – 3 recommended).")]
        [Range(0.5f, 4f)]
        public float solvedEmissionIntensity = 2f;

        [Header("Player Teleport")]
        [Tooltip("Empty GameObject placed on top of this step where the player should stand. " +
                 "Position it at floor level so the player's feet land correctly.")]
        public Transform playerStandPoint;

        [Tooltip("Seconds to wait after the correct combination fires before teleporting.")]
        [Range(0f, 3f)]
        public float teleportDelay = 0.8f;

        [Header("Events")]
        [Tooltip("Fired when this step's puzzle is solved and the player has been teleported.")]
        public UnityEvent OnStepSolved;

        // ─── Private State ─────────────────────────────────────────────────────

        private bool isSolved = false;
        private Material glassMaterial;
        private XROrigin xrOrigin;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Instance the glass material so each step's emission is independent
            if (glassPanelRenderer != null)
            {
                glassMaterial = glassPanelRenderer.material;

                // Dark base so only emission shows colour (prevents white wash-out)
                Color darkBase = new Color(0.04f, 0.04f, 0.04f, 0.45f);
                if (glassMaterial.HasProperty("_BaseColor"))
                    glassMaterial.SetColor("_BaseColor", darkBase);
                else if (glassMaterial.HasProperty("_Color"))
                    glassMaterial.SetColor("_Color", darkBase);

                glassMaterial.EnableKeyword("_EMISSION");
                glassMaterial.SetColor("_EmissionColor", Color.black);
            }

            // Buttons are locked until StairManager calls SetInteractable(true)
            if (rgbButtonPanel != null)
            {
                rgbButtonPanel.SetButtonsInteractable(false);
                rgbButtonPanel.OnCorrectCombination.AddListener(OnCombinationCorrect);
                rgbButtonPanel.OnButtonStateChanged += OnButtonStateChanged;
            }
        }

        private void Start()
        {
            // Cache XROrigin (the XR camera rig) for teleportation
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
                Debug.LogWarning($"[StairStep] '{name}': XROrigin not found in scene — teleport will not work.", this);
        }

        private void OnDestroy()
        {
            if (rgbButtonPanel != null)
            {
                rgbButtonPanel.OnCorrectCombination.RemoveListener(OnCombinationCorrect);
                rgbButtonPanel.OnButtonStateChanged -= OnButtonStateChanged;
            }
        }

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by StairManager to lock or unlock this step's RGB buttons.
        /// Steps start locked and are unlocked one at a time in sequence.
        /// </summary>
        public void SetInteractable(bool enable)
        {
            if (isSolved) return;

            if (rgbButtonPanel != null)
                rgbButtonPanel.SetButtonsInteractable(enable);

            // Apply a faint idle glow on the glass panel when unlocked
            if (glassMaterial != null)
            {
                if (enable)
                {
                    glassMaterial.EnableKeyword("_EMISSION");
                    // Gentle tinted idle glow using the solved colour at low power
                    glassMaterial.SetColor("_EmissionColor", solvedEmissionColor * 0.06f);
                }
                else
                {
                    glassMaterial.SetColor("_EmissionColor", Color.black);
                    glassMaterial.DisableKeyword("_EMISSION");
                }
            }

            Debug.Log($"[StairStep] '{name}' interactable = {enable}.", this);
        }

        // ─── Combination Listener ──────────────────────────────────────────────

        private void OnCombinationCorrect()
        {
            if (isSolved) return;
            isSolved = true;

            Debug.Log($"[StairStep] '{name}' — correct combination! Solving step.", this);

            // Lock the panel visually in solved state
            if (rgbButtonPanel != null)
                rgbButtonPanel.LockSolved();

            // Glow the glass surface
            SetGlassSolved();

            // Teleport player and then notify manager
            StartCoroutine(TeleportThenNotify());
        }

        // ─── Core Logic ────────────────────────────────────────────────────────

        private void SetGlassSolved()
        {
            if (glassMaterial == null) return;
            glassMaterial.EnableKeyword("_EMISSION");
            glassMaterial.SetColor("_EmissionColor", solvedEmissionColor * solvedEmissionIntensity);
        }

        private IEnumerator TeleportThenNotify()
        {
            // Brief pause so the player sees the glow before being moved
            if (teleportDelay > 0f)
                yield return new WaitForSeconds(teleportDelay);

            // ── Teleport ─────────────────────────────────────────────────────
            if (playerStandPoint != null && xrOrigin != null)
            {
                // MoveCameraToWorldLocation shifts the XR Origin so the camera
                // (player's head) ends up at the target position at its current height.
                // We want the FEET to land on the stand point, so we pass the position
                // directly and let XROrigin account for camera height automatically.
                xrOrigin.MoveCameraToWorldLocation(playerStandPoint.position);

                // Match the player's facing direction to the stand point's forward
                xrOrigin.MatchOriginUpCameraForward(Vector3.up, playerStandPoint.forward);

                Debug.Log($"[StairStep] '{name}' — player teleported to stand point.", this);
            }
            else
            {
                if (playerStandPoint == null)
                    Debug.LogWarning($"[StairStep] '{name}': playerStandPoint not assigned!", this);
                if (xrOrigin == null)
                    Debug.LogWarning($"[StairStep] '{name}': XROrigin not found!", this);
            }

            // ── Notify manager to unlock next step ────────────────────────────
            OnStepSolved?.Invoke();

            if (StairManager.Instance != null)
                StairManager.Instance.ReportStepSolved(this);
        }

        private void OnButtonStateChanged(bool r, bool g, bool b)
        {
            if (isSolved) return;

            // Combine active colors additively:
            Color activeColor = Color.black;
            if (r) activeColor += Color.red;
            if (g) activeColor += Color.green;
            if (b) activeColor += Color.blue;

            if (!r && !g && !b)
            {
                // Fallback: show the idle dim glow if no buttons are active
                SetEmission(solvedEmissionColor * 0.06f);
            }
            else
            {
                // Scale by a strong intensity multiplier for the blending look
                SetEmission(activeColor * 1.5f);
            }
        }

        private void SetEmission(Color color)
        {
            if (glassMaterial == null) return;
            glassMaterial.EnableKeyword("_EMISSION");
            glassMaterial.SetColor("_EmissionColor", color);
        }
    }
}
