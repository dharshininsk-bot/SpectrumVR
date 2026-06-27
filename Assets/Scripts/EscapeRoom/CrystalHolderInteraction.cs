using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EscapeRoom
{
    /// <summary>
    /// Attach to the Crystal Holder GameObject alongside an XRSimpleInteractable.
    ///
    /// Puzzle flow:
    ///   1. Player picks up the Crystal and it goes into their inventory.
    ///   2. Player points at / pokes the Crystal Holder and selects it.
    ///   3. Script checks the inventory for the crystal item by name.
    ///   4. If found: consumes the crystal, shows the crystal child visual (glowing),
    ///      and plays the stair retraction Animator state.
    ///   5. If not found: logs a warning (you can wire OnCrystalMissing to show UI feedback).
    ///
    /// Inspector setup:
    ///   - requiredItemName    → must match the PickupItem.itemName on the Crystal pickup
    ///                          (default: "Crystal").
    ///   - crystalChildVisual  → the child duplicate of the printed object (inactive by default).
    ///   - emissionColor       → HDR glow colour applied to the crystal visual when placed.
    ///   - stairAnimator       → Animator that controls the stair retraction animation.
    ///   - stairAnimStateName  → exact name of the Animator STATE to play (e.g. "StairRetract").
    ///                          Leave blank to use SetTrigger("Retract") instead.
    ///   - stairRetractTrigger → trigger parameter name on the Animator (used when
    ///                          stairAnimStateName is blank).
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class CrystalHolderInteraction : MonoBehaviour
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("Inventory Check")]
        [Tooltip("Must match PickupItem.itemName on the Crystal pickup object exactly.")]
        public string requiredItemName = "Crystal";

        [Header("Crystal Visual")]
        [Tooltip("Child GameObject (duplicate of the printed object) — inactive by default. " +
                 "Activated and made to glow when the crystal is successfully placed.")]
        public GameObject crystalChildVisual;

        [Tooltip("HDR glow colour applied to the crystal child visual on placement.")]
        [ColorUsage(true, true)]
        public Color emissionColor = new Color(0.3f, 1f, 2.5f, 1f); // vivid cyan-blue

        [Header("Stair Animation")]
        [Tooltip("Animator that owns the stair retraction animation.")]
        public Animator stairAnimator;

        [Tooltip("Exact name of the Animator STATE to play (e.g. 'StairRetract'). " +
                 "When filled, Animator.Play() is used. Leave blank to use SetTrigger instead.")]
        public string stairAnimStateName = "";

        [Tooltip("Trigger parameter name on the Animator — only used when stairAnimStateName is blank.")]
        public string stairRetractTrigger = "Retract";

        [Header("Settings")]
        [Tooltip("Allow the crystal to be placed only once (recommended).")]
        public bool oneTimeActivation = true;

        [Header("Events")]
        [Tooltip("Fired when the crystal is successfully placed.")]
        public UnityEvent OnCrystalPlaced;

        [Tooltip("Fired when the player interacts but does not have the crystal in inventory.")]
        public UnityEvent OnCrystalMissing;

        // ─── Private State ─────────────────────────────────────────────────────

        private XRSimpleInteractable interactable;
        private bool isActivated = false;
        private Renderer[] crystalRenderers;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();

            // Crystal child hidden at startup
            if (crystalChildVisual != null)
            {
                crystalChildVisual.SetActive(false);
                crystalRenderers = crystalChildVisual.GetComponentsInChildren<Renderer>();
            }

            // Disable stair animator until we need it
            if (stairAnimator != null)
                stairAnimator.enabled = false;
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        // ─── XR Callback ──────────────────────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log($"[CrystalHolder] Selected by '{args.interactorObject.transform.name}'.", this);
            TryPlaceCrystal();
        }

        // ─── Core Logic ───────────────────────────────────────────────────────

        /// <summary>
        /// Checks the inventory for the crystal. On success: consumes it, shows the
        /// crystal visual, and fires the stair retraction animation.
        /// </summary>
        public void TryPlaceCrystal()
        {
            if (oneTimeActivation && isActivated)
            {
                Debug.Log("[CrystalHolder] Crystal already placed — ignoring.", this);
                return;
            }

            // ── 1. Check inventory ────────────────────────────────────────────
            if (DynamicVRInventory.Instance == null)
            {
                Debug.LogWarning("[CrystalHolder] DynamicVRInventory not found in scene.", this);
                return;
            }

            bool hasCrystal = DynamicVRInventory.Instance.HasItem(requiredItemName);
            Debug.Log($"[CrystalHolder] Inventory check for '{requiredItemName}': {hasCrystal}", this);

            if (!hasCrystal)
            {
                Debug.LogWarning("[CrystalHolder] Crystal not in inventory — cannot place.", this);
                OnCrystalMissing?.Invoke();
                return;
            }

            // ── 2. Consume the crystal from inventory ─────────────────────────
            DynamicVRInventory.Instance.ConsumeItem(requiredItemName);
            isActivated = true;

            // ── 3. Show and illuminate the crystal child visual ───────────────
            if (crystalChildVisual != null)
            {
                crystalChildVisual.SetActive(true);
                ApplyCrystalGlow();
                Debug.Log("[CrystalHolder] Crystal visual activated and glowing.", this);
            }

            // ── 4. Play the stair retraction animation ────────────────────────
            PlayStairAnimation();

            // ── 5. Lock the holder so it can't be re-triggered ───────────────
            if (oneTimeActivation && interactable != null)
                interactable.enabled = false;

            OnCrystalPlaced?.Invoke();
            Debug.Log("[CrystalHolder] Crystal placed successfully.", this);
        }

        // ─── Animation ────────────────────────────────────────────────────────

        private void PlayStairAnimation()
        {
            if (stairAnimator == null)
            {
                Debug.LogWarning("[CrystalHolder] stairAnimator is not assigned! " +
                                 "Assign the Animator that controls the stair retraction.", this);
                return;
            }

            stairAnimator.enabled = true;
            stairAnimator.speed   = 1f;

            if (!string.IsNullOrEmpty(stairAnimStateName))
            {
                // Play a specific named state from the beginning
                StartCoroutine(PlayStateNextFrame(stairAnimStateName));
                Debug.Log($"[CrystalHolder] Playing stair animation state '{stairAnimStateName}'.", this);
            }
            else if (!string.IsNullOrEmpty(stairRetractTrigger))
            {
                // Fire a trigger parameter instead
                stairAnimator.SetTrigger(stairRetractTrigger);
                Debug.Log($"[CrystalHolder] Set stair animator trigger '{stairRetractTrigger}'.", this);
            }
            else
            {
                Debug.LogWarning("[CrystalHolder] Neither stairAnimStateName nor stairRetractTrigger " +
                                 "is set — stair animation will not play.", this);
            }
        }

        /// <summary>
        /// Waits one frame for the Animator to wake up before calling Play(),
        /// matching the pattern used in LensPuzzleController.
        /// </summary>
        private IEnumerator PlayStateNextFrame(string stateName)
        {
            yield return null; // let the Animator enable and initialise
            stairAnimator.Play(stateName, 0, 0f);
        }

        // ─── Emission ─────────────────────────────────────────────────────────

        private void ApplyCrystalGlow()
        {
            if (crystalRenderers == null) return;
            foreach (var rend in crystalRenderers)
            {
                if (rend == null) continue;
                foreach (var mat in rend.materials)
                {
                    if (mat == null) continue;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor);
                }
            }
        }
    }
}
