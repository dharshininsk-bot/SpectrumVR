using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PickupItem : MonoBehaviour
{
    [Header("Item Metadata")]
    public string itemName;          // e.g., "CD" or "USB"
    public Sprite itemIcon;          // Drag the matching 2D Sprite texture here in the Inspector
    
    [Header("Optional Settings")]
    public bool destroyOnPickup = false; // Set to false if you just want to hide it (SetActive(false))
    public bool pickupOnGrab = true;     // If true, grabbing this object will automatically add it to inventory

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (pickupOnGrab)
        {
            if (DynamicVRInventory.Instance != null)
            {
                DynamicVRInventory.Instance.TryStoreItem(this);
            }
            else
            {
                DynamicVRInventory inventory = FindObjectOfType<DynamicVRInventory>();
                if (inventory != null)
                {
                    inventory.TryStoreItem(this);
                }
                else
                {
                    Debug.LogWarning($"Grabbed {itemName}, but DynamicVRInventory instance could not be found in the scene.");
                }
            }
        }
    }
}