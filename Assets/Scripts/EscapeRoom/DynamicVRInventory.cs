using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DynamicVRInventory : MonoBehaviour
{
    public static DynamicVRInventory Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Header("VR Raycast Tracking")]
    public Transform vrRaycastOrigin;       // Position and forward direction of your VR hand/pointing ray
    public float pickupDistance = 3.0f;     // Interaction range max distance
    public LayerMask interactableLayer;     // Ensure your items are on this layer

    [Header("UI Grid Layout Setup")]
    public Image[] slotIcons = new Image[8]; // Drag your 8 child "Icon_Display" images here in order
    private string[] storedItemNames = new string[8];

    [Header("Input Settings")]
    [Tooltip("Input action to trigger item pickup. Map this to VR controller buttons (e.g. Grip, Trigger, Select) or keyboard keys.")]
    public InputActionProperty pickupAction;
    private bool hasWarnedAboutInput = false;

    private void OnEnable()
    {
        pickupAction.action?.Enable();
    }

    private void OnDisable()
    {
        pickupAction.action?.Disable();
    }

    private void Start()
    {
        // Auto-disable slot icons if they don't have a sprite assigned, marking them as empty
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] != null)
            {
                if (slotIcons[i].sprite == null)
                {
                    slotIcons[i].enabled = false;
                }
                else
                {
                    slotIcons[i].enabled = true;
                }
            }
            storedItemNames[i] = null;
        }
    }

    void Update()
    {
        // Safety guard: ensure the raycast origin is assigned
        if (vrRaycastOrigin == null)
        {
            Debug.LogWarning("DynamicVRInventory: vrRaycastOrigin is not assigned in the Inspector!", this);
            return;
        }

        // 1. Shoot a laser ray forward from the VR tracking hand controller
        Ray ray = new Ray(vrRaycastOrigin.position, vrRaycastOrigin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance, interactableLayer))
        {
            // 2. See if the targeted object has our modular PickupItem script attached
            PickupItem itemData = hit.collider.GetComponent<PickupItem>();

            if (itemData != null)
            {
                // 3. Check for pickup button press (either via InputAction or Keyboard G fallback)
                bool isPickupPressed = false;

                if (pickupAction.action != null)
                {
                    isPickupPressed = pickupAction.action.WasPressedThisFrame();
                }
                else if (Keyboard.current != null)
                {
                    isPickupPressed = Keyboard.current.gKey.wasPressedThisFrame;
                }
                else
                {
                    // Fallback log to help debug if Keyboard.current is null and no action is bound
                    if (!hasWarnedAboutInput)
                    {
                        Debug.LogWarning("DynamicVRInventory: No keyboard detected and pickupAction is not configured. Please assign pickupAction in the inspector.");
                        hasWarnedAboutInput = true;
                    }
                }

                if (isPickupPressed)
                {
                    TryStoreItem(itemData);
                }
            }
        }
    }

    public bool TryStoreItem(PickupItem item)
    {
        // Loop through all 8 available slots from top-left to bottom-right
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] == null) continue;

            // A slot is free if its image component is disabled, its sprite is null, or no item name is tracked
            if (!slotIcons[i].enabled || slotIcons[i].sprite == null || string.IsNullOrEmpty(storedItemNames[i]))
            {
                // DYNAMIC MAPPING: Grab the exact sprite loaded onto the physical object itself!
                slotIcons[i].sprite = item.itemIcon;
                slotIcons[i].enabled = true; // Make the UI image visible
                
                // Force alpha to 1.0f in case the image was set to transparent in the inspector
                Color c = slotIcons[i].color;
                slotIcons[i].color = new Color(c.r, c.g, c.b, 1.0f);
                
                storedItemNames[i] = item.itemName; // Track item name in index

                Debug.Log($"Successfully mapped and added {item.itemName} to inventory slot {i + 1}!");

                // 4. Handle the physical world space model state
                if (item.destroyOnPickup)
                {
                    Destroy(item.gameObject);
                }
                else
                {
                    item.gameObject.SetActive(false); // Keeps it hidden in the scene graph for puzzle logic usage later
                }

                return true; // Break the loop so it only populates ONE single slot
            }
        }
        Debug.LogWarning($"Inventory is full! Could not add {item.itemName}.", this);
        return false;
    }

    /// <summary>
    /// Checks if the inventory contains an item by name.
    /// </summary>
    public bool HasItem(string itemName)
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i].enabled && storedItemNames[i] == itemName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the inventory contains an item and removes it if found.
    /// </summary>
    public bool ConsumeItem(string itemName)
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i].enabled && storedItemNames[i] == itemName)
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
                storedItemNames[i] = null;
                Debug.Log($"Consumed item {itemName} from slot {i + 1}.");
                return true;
            }
        }
        return false;
    }
}