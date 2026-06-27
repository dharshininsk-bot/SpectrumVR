using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EscapeRoom
{
    /// <summary>
    /// Singleton that manages sequential stair step unlocking.
    ///
    /// Flow:
    ///   1. All steps start LOCKED (buttons non-interactable).
    ///   2. CrystalHolderInteraction.OnCrystalPlaced → wire to UnlockFirstStep() in Inspector.
    ///   3. Step 0 unlocks — player solves it — player is teleported to step 0 stand point.
    ///   4. StairStep calls ReportStepSolved(step) → Step 1 unlocks automatically.
    ///   5. Continues until all steps are solved → OnAllStepsSolved fires.
    ///
    /// Inspector setup:
    ///   - stairSteps → assign StairStep components in ORDER (Step 0 = first to unlock).
    /// </summary>
    public class StairManager : MonoBehaviour
    {
        // ─── Singleton ─────────────────────────────────────────────────────────

        public static StairManager Instance { get; private set; }

        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("Stair Steps (in unlock order)")]
        [Tooltip("Assign StairStep components in the order they should unlock. " +
                 "Index 0 = first step (unlocked when crystal is placed).")]
        [SerializeField] private List<StairStep> stairSteps = new List<StairStep>();

        [Header("Events")]
        [Tooltip("Fired when ALL steps have been solved.")]
        public UnityEvent OnAllStepsSolved;

        // ─── Private State ─────────────────────────────────────────────────────

        private int currentStepIndex = -1; // -1 = none unlocked yet

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Lock every step at startup
            foreach (var step in stairSteps)
            {
                if (step != null)
                    step.SetInteractable(false);
            }

            Debug.Log($"[StairManager] Initialised — {stairSteps.Count} steps locked.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Wire this to CrystalHolderInteraction.OnCrystalPlaced in the Inspector.
        /// Unlocks the very first step so the player can start the puzzle.
        /// </summary>
        public void UnlockFirstStep()
        {
            if (currentStepIndex >= 0)
            {
                Debug.Log("[StairManager] First step already unlocked — ignoring.", this);
                return;
            }

            if (stairSteps == null || stairSteps.Count == 0)
            {
                Debug.LogWarning("[StairManager] stairSteps list is empty!", this);
                return;
            }

            Debug.Log("[StairManager] Crystal placed — unlocking Step 0.", this);
            UnlockStep(0);
        }

        /// <summary>
        /// Called by StairStep when it is solved and the player has been teleported.
        /// Automatically unlocks the next step in sequence.
        /// </summary>
        public void ReportStepSolved(StairStep solvedStep)
        {
            int solvedIndex = stairSteps.IndexOf(solvedStep);
            Debug.Log($"[StairManager] Step {solvedIndex} ('{solvedStep.name}') solved.", this);

            int nextIndex = solvedIndex + 1;

            if (nextIndex < stairSteps.Count)
            {
                Debug.Log($"[StairManager] Unlocking Step {nextIndex}.", this);
                UnlockStep(nextIndex);
            }
            else
            {
                Debug.Log("[StairManager] All steps solved!", this);
                OnAllStepsSolved?.Invoke();
            }
        }

        // ─── Internal ──────────────────────────────────────────────────────────

        private void UnlockStep(int index)
        {
            if (index < 0 || index >= stairSteps.Count) return;

            currentStepIndex = index;
            var step = stairSteps[index];

            if (step != null)
            {
                step.SetInteractable(true);
                Debug.Log($"[StairManager] Step {index} ('{step.name}') is now interactable.", this);
            }
        }
    }
}
