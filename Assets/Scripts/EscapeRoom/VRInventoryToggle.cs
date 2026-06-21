using UnityEngine;
using UnityEngine.InputSystem;

public class VRInventoryToggle : MonoBehaviour
{
    [Header("Inventory Canvas Reference")]
    [Tooltip("The canvas or UI panel that represents the inventory. Usually VR_Inventory_Canvas or its child panel.")]
    public GameObject inventoryPanel;

    [Header("Input Setup")]
    [Tooltip("Action used to toggle the inventory. You can bind this to menu buttons, primary/secondary buttons, or trackpad press on both controllers.")]
    public InputActionProperty toggleAction;

    [Header("Settings")]
    [Tooltip("If true, inventory starts hidden.")]
    public bool startHidden = true;

    [Tooltip("Optional: Sound to play when inventory toggles.")]
    public AudioClip toggleSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!startHidden);
        }
    }

    private void OnEnable()
    {
        toggleAction.action?.Enable();
    }

    private void OnDisable()
    {
        toggleAction.action?.Disable();
    }

    private void Update()
    {
        if (inventoryPanel == null) return;

        if (toggleAction.action != null && toggleAction.action.WasPressedThisFrame())
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        bool nextState = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(nextState);

        if (toggleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(toggleSound);
        }

        Debug.Log($"Inventory toggle clicked. Visible: {nextState}");
    }
}
