using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EscapeRoom
{
    /// <summary>
    /// Attach to the RGB_Panel GameObject (child of each stair step).
    ///
    /// Expected hierarchy under RGB_Panel:
    ///   RGB_Panel  ← this script lives here
    ///   ├── CylinderR  (Renderer turns white when ON)
    ///   │   └── ButtonR  (XRSimpleInteractable — player presses this)
    ///   ├── CylinderG
    ///   │   └── ButtonG
    ///   └── CylinderB
    ///       └── ButtonB
    ///
    /// Inspector setup:
    ///   - cylinderR/G/B     → drag the three cylinder GameObjects here.
    ///   - targetR/G/B       → tick which buttons must be ON to solve this step.
    ///   - onColor           → colour the cylinder glows when its button is ON (default: white).
    ///   - onEmission        → HDR emission strength of the ON state.
    /// </summary>
    public class RGBButtonPanel : MonoBehaviour
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("Cylinder References")]
        [Tooltip("The Red cylinder GameObject. Its Renderer will turn ON/OFF.")]
        public GameObject cylinderR;

        [Tooltip("The Green cylinder GameObject.")]
        public GameObject cylinderG;

        [Tooltip("The Blue cylinder GameObject.")]
        public GameObject cylinderB;

        [Header("Target Combination")]
        [Tooltip("Should the Red button be ON to solve this step?")]
        public bool targetR = true;

        [Tooltip("Should the Green button be ON to solve this step?")]
        public bool targetG = false;

        [Tooltip("Should the Blue button be ON to solve this step?")]
        public bool targetB = false;

        [Header("Visual Settings")]
        [Tooltip("Colour the cylinder glows when switched ON.")]
        public Color onColor = Color.white;

        [Tooltip("HDR emission strength for the ON state. Keep between 1.5 and 3.")]
        [Range(0.5f, 5f)]
        public float onEmission = 2f;

        [Tooltip("Base albedo colour when a cylinder is OFF (near-black keeps emission clear).")]
        public Color offColor = new Color(0.05f, 0.05f, 0.05f, 1f);

        [Header("Events")]
        [Tooltip("Fired when the current ON/OFF state of R, G, B matches the target combination.")]
        public UnityEvent OnCorrectCombination;

        [Tooltip("Fired when a button is toggled but the combination is still wrong.")]
        public UnityEvent OnWrongCombination;

        // ─── Private State ─────────────────────────────────────────────────────

        private bool stateR = false;
        private bool stateG = false;
        private bool stateB = false;

        private XRSimpleInteractable btnR;
        private XRSimpleInteractable btnG;
        private XRSimpleInteractable btnB;

        private Material matR;
        private Material matG;
        private Material matB;

        private bool isSolved = false;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Grab button interactables from each cylinder's first child
            btnR = GetButtonFromCylinder(cylinderR);
            btnG = GetButtonFromCylinder(cylinderG);
            btnB = GetButtonFromCylinder(cylinderB);

            // Cache instanced materials so cylinders don't share state
            matR = GetCylinderMaterial(cylinderR);
            matG = GetCylinderMaterial(cylinderG);
            matB = GetCylinderMaterial(cylinderB);

            // Start all cylinders OFF
            ApplyCylinderVisual(matR, false);
            ApplyCylinderVisual(matG, false);
            ApplyCylinderVisual(matB, false);
        }

        private void OnEnable()
        {
            SubscribeButton(btnR, OnPressR);
            SubscribeButton(btnG, OnPressG);
            SubscribeButton(btnB, OnPressB);
        }

        private void OnDisable()
        {
            UnsubscribeButton(btnR, OnPressR);
            UnsubscribeButton(btnG, OnPressG);
            UnsubscribeButton(btnB, OnPressB);
        }

        // ─── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables all three buttons on this panel.
        /// Call this from StairStep.SetInteractable().
        /// </summary>
        public void SetButtonsInteractable(bool enable)
        {
            if (btnR != null) btnR.enabled = enable;
            if (btnG != null) btnG.enabled = enable;
            if (btnB != null) btnB.enabled = enable;

            Debug.Log($"[RGBButtonPanel] '{name}' buttons interactable = {enable}.", this);
        }

        /// <summary>
        /// Locks the panel in the solved state — buttons disabled, cylinders frozen ON.
        /// </summary>
        public void LockSolved()
        {
            isSolved = true;
            SetButtonsInteractable(false);
        }

        // ─── Button Callbacks ──────────────────────────────────────────────────

        private void OnPressR(SelectEnterEventArgs args)
        {
            if (isSolved) return;
            stateR = !stateR;
            ApplyCylinderVisual(matR, stateR);
            Debug.Log($"[RGBButtonPanel] R toggled → {stateR}", this);
            EvaluateCombination();
        }

        private void OnPressG(SelectEnterEventArgs args)
        {
            if (isSolved) return;
            stateG = !stateG;
            ApplyCylinderVisual(matG, stateG);
            Debug.Log($"[RGBButtonPanel] G toggled → {stateG}", this);
            EvaluateCombination();
        }

        private void OnPressB(SelectEnterEventArgs args)
        {
            if (isSolved) return;
            stateB = !stateB;
            ApplyCylinderVisual(matB, stateB);
            Debug.Log($"[RGBButtonPanel] B toggled → {stateB}", this);
            EvaluateCombination();
        }

        // ─── Core Logic ────────────────────────────────────────────────────────

        private void EvaluateCombination()
        {
            bool correct = (stateR == targetR) && (stateG == targetG) && (stateB == targetB);

            if (correct)
            {
                Debug.Log($"[RGBButtonPanel] CORRECT combination on '{name}'!", this);
                OnCorrectCombination?.Invoke();
            }
            else
            {
                OnWrongCombination?.Invoke();
            }
        }

        // ─── Visual Helpers ────────────────────────────────────────────────────

        private void ApplyCylinderVisual(Material mat, bool isOn)
        {
            if (mat == null) return;

            Color baseCol = isOn ? onColor : offColor;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseCol);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", baseCol);

            if (isOn)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", onColor * onEmission);
            }
            else
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }

        // ─── Setup Helpers ─────────────────────────────────────────────────────

        private XRSimpleInteractable GetButtonFromCylinder(GameObject cylinder)
        {
            if (cylinder == null) return null;

            // The button is the first child of the cylinder
            if (cylinder.transform.childCount > 0)
            {
                var btn = cylinder.transform.GetChild(0)
                                  .GetComponent<XRSimpleInteractable>();
                if (btn == null)
                    Debug.LogWarning($"[RGBButtonPanel] Child of '{cylinder.name}' has no XRSimpleInteractable!", this);
                return btn;
            }

            // Fallback: check cylinder itself
            return cylinder.GetComponent<XRSimpleInteractable>();
        }

        private Material GetCylinderMaterial(GameObject cylinder)
        {
            if (cylinder == null) return null;
            var rend = cylinder.GetComponent<Renderer>();
            if (rend == null) rend = cylinder.GetComponentInChildren<Renderer>();
            if (rend == null)
            {
                Debug.LogWarning($"[RGBButtonPanel] No Renderer found on '{cylinder?.name}'!", this);
                return null;
            }
            // .material creates a per-instance copy — cylinders on different steps won't share
            return rend.material;
        }

        private void SubscribeButton(XRSimpleInteractable btn,
                                     System.Action<SelectEnterEventArgs> callback)
        {
            if (btn == null) return;
            btn.selectEntered.AddListener(new UnityEngine.Events.UnityAction<SelectEnterEventArgs>(callback));
        }

        private void UnsubscribeButton(XRSimpleInteractable btn,
                                       System.Action<SelectEnterEventArgs> callback)
        {
            if (btn == null) return;
            btn.selectEntered.RemoveListener(new UnityEngine.Events.UnityAction<SelectEnterEventArgs>(callback));
        }
    }
}
