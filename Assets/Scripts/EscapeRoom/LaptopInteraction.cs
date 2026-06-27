using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EscapeRoom
{
    /// <summary>
    /// Attach this directly to the Laptop GameObject (alongside an XRBaseInteractable).
    ///
    /// Puzzle flow:
    ///   1. Player clicks the Laptop → script checks inventory for the Pendrive.
    ///   2. If found: consumes Pendrive, shows the inserted-pendrive visual, opens the Screen Canvas.
    ///   3. Player clicks Print on the canvas → model activates at the 3D Printer.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class LaptopInteraction : MonoBehaviour
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("Inventory Check")]
        [Tooltip("Item name to look for in inventory. Must match PickupItem.itemName on the Pendrive pickup.")]
        public string requiredItemName = "Pendrive";

        [Header("Pendrive Visual")]
        [Tooltip("Duplicate pendrive mesh already parented inside the laptop — hidden by default, shown when inserted.")]
        public GameObject insertedPendriveVisual;

        [Header("Laptop Screen")]
        [Tooltip("World-Space Canvas that appears on the laptop screen. Should be inactive by default.")]
        public GameObject laptopScreenCanvas;

        [Tooltip("UI Image inside the canvas that shows the blueprint / model preview.")]
        public Image blueprintImage;

        [Tooltip("Sprite to display on the screen (e.g. blueprint or model preview image).")]
        public Sprite blueprintSprite;

        [Header("3D Printer Output")]
        [Tooltip("GameObject at the 3D Printer that becomes active after Print is pressed. Should be inactive by default.")]
        public GameObject printedModelObject;

        [Tooltip("Optional ParticleSystem on the 3D Printer that plays when printing starts.")]
        public ParticleSystem printerEffect;

        [Header("Events")]
        [Tooltip("Fired when the pendrive is successfully inserted and the screen opens.")]
        public UnityEvent OnPendriveInserted;

        [Tooltip("Fired when the Print button is pressed.")]
        public UnityEvent OnPrintPressed;

        [Tooltip("Fired when the player clicks the laptop but doesn't have the pendrive.")]
        public UnityEvent OnInsertionFailed;

        // ─── Private State ─────────────────────────────────────────────────────

        private XRBaseInteractable interactable;
        private bool pendriveInserted = false;
        private bool hasPrinted = false;

        // ─── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
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

        private void Start()
        {
            // Ensure visuals are hidden at startup
            if (laptopScreenCanvas != null)
                laptopScreenCanvas.SetActive(false);

            if (insertedPendriveVisual != null)
                insertedPendriveVisual.SetActive(false);

            if (printedModelObject != null)
                printedModelObject.SetActive(false);

            // Apply the blueprint sprite if already assigned
            if (blueprintImage != null && blueprintSprite != null)
                blueprintImage.sprite = blueprintSprite;
        }

        // ─── XR Interaction ───────────────────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log($"[LaptopInteraction] Laptop clicked by {args.interactorObject.transform.name}", this);

            if (pendriveInserted)
            {
                // Pendrive already in — toggle the screen open/closed
                if (laptopScreenCanvas != null)
                {
                    bool nowOpen = !laptopScreenCanvas.activeSelf;
                    laptopScreenCanvas.SetActive(nowOpen);
                    Debug.Log($"[LaptopInteraction] Screen toggled {(nowOpen ? "open" : "closed")}.", this);
                }
                return;
            }

            // First click — try to insert from inventory
            TryInsertPendrive();
        }

        // ─── Core Logic ───────────────────────────────────────────────────────

        /// <summary>
        /// Checks inventory for the Pendrive. If found: consumes it, shows the inserted visual, opens the screen.
        /// </summary>
        public void TryInsertPendrive()
        {
            if (pendriveInserted)
                return;

            if (DynamicVRInventory.Instance == null)
            {
                Debug.LogWarning("[LaptopInteraction] DynamicVRInventory instance not found in scene.", this);
                return;
            }

            bool hasItem = DynamicVRInventory.Instance.HasItem(requiredItemName);
            Debug.Log($"[LaptopInteraction] Inventory check for '{requiredItemName}': {hasItem}", this);

            if (hasItem)
            {
                DynamicVRInventory.Instance.ConsumeItem(requiredItemName);

                if (insertedPendriveVisual != null)
                    insertedPendriveVisual.SetActive(true);

                if (laptopScreenCanvas != null)
                    laptopScreenCanvas.SetActive(true);

                pendriveInserted = true;

                // Disable the interactable so the player cannot re-grab the laptop
                // and can instead interact with the Print button on the screen canvas.
                if (interactable != null)
                    interactable.enabled = false;

                OnPendriveInserted?.Invoke();

                Debug.Log("[LaptopInteraction] Pendrive inserted — screen opened. Laptop interaction disabled.", this);
            }
            else
            {
                OnInsertionFailed?.Invoke();
                Debug.LogWarning($"[LaptopInteraction] Laptop clicked but '{requiredItemName}' not in inventory.", this);
            }
        }

        /// <summary>
        /// Call this from the Print button's OnClick inside the Laptop Screen Canvas.
        /// Closes the screen and activates the model at the 3D Printer.
        /// </summary>
        public void Print()
        {
            if (!pendriveInserted)
            {
                Debug.LogWarning("[LaptopInteraction] Print() called before pendrive was inserted.", this);
                return;
            }

            if (hasPrinted)
            {
                Debug.Log("[LaptopInteraction] Already printed — ignoring duplicate call.", this);
                return;
            }

            hasPrinted = true;

            if (laptopScreenCanvas != null)
                laptopScreenCanvas.SetActive(false);

            if (printedModelObject != null)
                printedModelObject.SetActive(true);

            if (printerEffect != null)
                printerEffect.Play();

            OnPrintPressed?.Invoke();
            Debug.Log("[LaptopInteraction] Printing — model now visible at the 3D Printer.", this);
        }

        /// <summary>
        /// Closes the screen without printing. Wire this to a Cancel/Close button if needed.
        /// </summary>
        public void CloseScreen()
        {
            if (laptopScreenCanvas != null)
                laptopScreenCanvas.SetActive(false);

            Debug.Log("[LaptopInteraction] Screen closed.", this);
        }
    }
}
