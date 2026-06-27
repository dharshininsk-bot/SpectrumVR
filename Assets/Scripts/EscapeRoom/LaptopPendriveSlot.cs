using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EscapeRoom
{
    /// <summary>
    /// Attach this to the Laptop's USB slot interactable.
    /// When the player interacts and has the Pendrive in inventory, it:
    ///   1. Consumes the Pendrive from inventory
    ///   2. Activates the duplicate "inserted" pendrive visual on the laptop
    ///   3. Opens the Laptop Screen Canvas (image + Print button)
    ///
    /// The Print button on the canvas should call LaptopPendriveSlot.Print() via UnityEvent.
    /// On Print, the 3D Printer output model is activated.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class LaptopPendriveSlot : MonoBehaviour
    {
        // ─── Inspector Fields ──────────────────────────────────────────────────

        [Header("Inventory Check")]
        [Tooltip("The item name to look for in inventory. Must match PickupItem.itemName on the Pendrive.")]
        public string requiredItemName = "Pendrive";

        [Header("Pendrive Visual")]
        [Tooltip("The duplicate pendrive mesh already parented inside the laptop — starts inactive, shown when inserted.")]
        public GameObject insertedPendriveVisual;

        [Header("Laptop Screen")]
        [Tooltip("The World-Space Canvas that appears on the laptop screen.")]
        public GameObject laptopScreenCanvas;

        [Tooltip("The UI Image inside the canvas that displays the blueprint/model image.")]
        public Image blueprintImage;

        [Tooltip("Sprite to display on the laptop screen (e.g. a blueprint or model preview).")]
        public Sprite blueprintSprite;

        [Header("3D Printer Output")]
        [Tooltip("The GameObject representing the printed model at the 3D Printer — starts inactive, activated on Print.")]
        public GameObject printedModelObject;

        [Tooltip("Optional: ParticleSystem on the 3D Printer that plays when printing starts.")]
        public ParticleSystem printerEffect;

        [Header("Events")]
        [Tooltip("Fired when the pendrive is successfully inserted (screen opens).")]
        public UnityEvent OnPendriveInserted;

        [Tooltip("Fired when the player presses the Print button.")]
        public UnityEvent OnPrintPressed;

        [Tooltip("Fired if the player tries to interact without the required item.")]
        public UnityEvent OnInsertionFailed;

        // ─── Private State ─────────────────────────────────────────────────────

        private XRBaseInteractable interactable;
        private bool pendriveInserted = false;
        private bool printed = false;

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
            // Ensure screen and printer model are hidden at the start
            if (laptopScreenCanvas != null)
                laptopScreenCanvas.SetActive(false);

            if (insertedPendriveVisual != null)
                insertedPendriveVisual.SetActive(false);

            if (printedModelObject != null)
                printedModelObject.SetActive(false);

            // Assign blueprint sprite if both references are set
            if (blueprintImage != null && blueprintSprite != null)
                blueprintImage.sprite = blueprintSprite;
        }

        // ─── XR Interaction ───────────────────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log($"[LaptopPendriveSlot] Interaction registered on {gameObject.name} by {args.interactorObject.transform.name}", this);

            if (pendriveInserted)
            {
                // If already inserted, re-open the screen (toggle)
                if (laptopScreenCanvas != null)
                {
                    bool isOpen = laptopScreenCanvas.activeSelf;
                    laptopScreenCanvas.SetActive(!isOpen);
                    Debug.Log($"[LaptopPendriveSlot] Screen toggled {(!isOpen ? "open" : "closed")}.", this);
                }
                return;
            }

            TryInsertPendrive();
        }

        // ─── Core Logic ───────────────────────────────────────────────────────

        /// <summary>
        /// Checks inventory for the pendrive, consumes it, and opens the laptop screen.
        /// Can also be called directly (e.g. from a separate trigger).
        /// </summary>
        public void TryInsertPendrive()
        {
            if (pendriveInserted)
            {
                Debug.Log("[LaptopPendriveSlot] Pendrive already inserted.", this);
                return;
            }

            if (DynamicVRInventory.Instance == null)
            {
                Debug.LogWarning("[LaptopPendriveSlot] DynamicVRInventory instance not found in scene.", this);
                return;
            }

            bool hasItem = DynamicVRInventory.Instance.HasItem(requiredItemName);
            Debug.Log($"[LaptopPendriveSlot] Checking inventory for '{requiredItemName}'. Found: {hasItem}", this);

            if (hasItem)
            {
                // Consume pendrive from inventory
                DynamicVRInventory.Instance.ConsumeItem(requiredItemName);

                // Show the already-placed duplicate model inside the laptop
                if (insertedPendriveVisual != null)
                    insertedPendriveVisual.SetActive(true);

                // Open the laptop screen
                if (laptopScreenCanvas != null)
                    laptopScreenCanvas.SetActive(true);

                pendriveInserted = true;

                OnPendriveInserted?.Invoke();
                Debug.Log("[LaptopPendriveSlot] Pendrive inserted — screen is now open.", this);
            }
            else
            {
                OnInsertionFailed?.Invoke();
                Debug.LogWarning($"[LaptopPendriveSlot] Cannot insert: '{requiredItemName}' not found in inventory.", this);
            }
        }

        /// <summary>
        /// Call this from the Print button's OnClick event on the laptop screen canvas.
        /// Closes the screen and activates the printed model at the 3D Printer.
        /// </summary>
        public void Print()
        {
            if (!pendriveInserted)
            {
                Debug.LogWarning("[LaptopPendriveSlot] Print() called but pendrive has not been inserted yet.", this);
                return;
            }

            if (printed)
            {
                Debug.Log("[LaptopPendriveSlot] Already printed. Ignoring duplicate Print() call.", this);
                return;
            }

            printed = true;

            // Close the laptop screen
            if (laptopScreenCanvas != null)
                laptopScreenCanvas.SetActive(false);

            // Activate the model at the 3D printer
            if (printedModelObject != null)
                printedModelObject.SetActive(true);

            // Play printer particle/animation effect
            if (printerEffect != null)
                printerEffect.Play();

            OnPrintPressed?.Invoke();
            Debug.Log("[LaptopPendriveSlot] Print confirmed — model is now appearing at the 3D Printer.", this);
        }

        /// <summary>
        /// Closes the laptop screen without printing (for a close/cancel button, if needed).
        /// </summary>
        public void CloseScreen()
        {
            if (laptopScreenCanvas != null)
                laptopScreenCanvas.SetActive(false);

            Debug.Log("[LaptopPendriveSlot] Laptop screen closed.", this);
        }
    }
}
