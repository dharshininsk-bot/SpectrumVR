using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class CDDrawerDropZone : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("The item name required to activate this drop zone (e.g. 'CD').")]
    public string requiredItemName = "CD";

    [Header("Visual Feedback")]
    [Tooltip("An optional GameObject representing the CD in place (activated when CD is placed).")]
    public GameObject placedItemVisual;

    [Header("Events")]
    [Tooltip("Triggered when the player successfully inserts/uses the CD.")]
    public UnityEvent OnItemPlaced;

    [Tooltip("Triggered if the player attempts to interact but does not have the required item.")]
    public UnityEvent OnPlacementFailed;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
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
        Debug.Log($"[CDDrawerDropZone] Click/Interaction registered on {gameObject.name} by interactor {args.interactorObject.transform.name}", this);
        TryUseItem();
    }

    /// <summary>
    /// Attempts to consume the required item from the inventory and activate the drop zone.
    /// </summary>
    public void TryUseItem()
    {
        if (DynamicVRInventory.Instance != null)
        {
            bool hasItem = DynamicVRInventory.Instance.HasItem(requiredItemName);
            Debug.Log($"[CDDrawerDropZone] Checking inventory for '{requiredItemName}'. Found: {hasItem}", this);

            if (hasItem)
            {
                // Consume the item from the inventory
                DynamicVRInventory.Instance.ConsumeItem(requiredItemName);

                // Show the physical item on the drawer/drop zone
                if (placedItemVisual != null)
                {
                    placedItemVisual.SetActive(true);
                }

                // Trigger successful placement events
                OnItemPlaced?.Invoke();
                Debug.Log($"[CDDrawerDropZone] Successfully placed {requiredItemName} in the drop zone!");
            }
            else
            {
                OnPlacementFailed?.Invoke();
                Debug.LogWarning($"[CDDrawerDropZone] Cannot activate drop zone: missing required item '{requiredItemName}'.");
            }
        }
        else
        {
            Debug.LogWarning("[CDDrawerDropZone] DynamicVRInventory instance could not be found.");
        }
    }
}
