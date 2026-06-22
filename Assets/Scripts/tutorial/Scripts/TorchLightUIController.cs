using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Controls the UI display of 3 intensity sliders (Red, Green, Blue) corresponding to 3 torches.
/// Shows the UI when any torch is grabbed, and allows adjusting the corresponding slider value (0-255)
/// using the controller thumbstick or keyboard (I and K keys).
/// </summary>
public class TorchLightUIController : MonoBehaviour
{
    [System.Serializable]
    public class TorchSettings
    {
        [Tooltip("Friendly name for this torch configuration.")]
        public string torchName;

        [Tooltip("The grab interactable component on the torch.")]
        public XRGrabInteractable grabInteractable;

        [Tooltip("The Light component associated with this torch (optional).")]
        public Light torchLight;

        [Tooltip("The UI Slider that controls this torch's light intensity.")]
        public Slider intensitySlider;

        [Tooltip("The maximum physical intensity of the light in Unity when the slider is at 255.")]
        public float maxPhysicalIntensity = 5f;
    }

    [Header("Torch Configurations")]
    [SerializeField] private List<TorchSettings> torches = new List<TorchSettings>();

    [Header("UI Panel Settings")]
    [Tooltip("The parent GameObject containing the UI canvas/panel. It will be shown when a torch is grabbed and hidden when all are dropped.")]
    [SerializeField] private GameObject uiPanel;

    [Header("Adjustment Settings")]
    [Tooltip("How fast the slider adjusts when using the thumbstick or keyboard (units per second, 0 to 255 range).")]
    [SerializeField] private float adjustmentSpeed = 100f;

    // Dictionary to track which grabbed torch maps to which XRNode (LeftHand/RightHand)
    private Dictionary<XRGrabInteractable, XRNode> grabbedTorches = new Dictionary<XRGrabInteractable, XRNode>();
    
    // The currently active torch that will be adjusted by inputs
    private TorchSettings activeTorch = null;
    
    // The VR controller hand currently holding the active torch
    private XRNode activeGrabNode = XRNode.RightHand;

    private void Awake()
    {
        // Auto-configure the sliders to range from 0 to 255 if not already set in Inspector
        foreach (var torch in torches)
        {
            if (torch.intensitySlider != null)
            {
                torch.intensitySlider.minValue = 0f;
                torch.intensitySlider.maxValue = 255f;
            }
        }
    }

    private void Start()
    {
        // Initially hide the UI panel
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        // Initialize and bind slider events
        foreach (var torch in torches)
        {
            if (torch.intensitySlider != null)
            {
                // Set the initial slider value based on the current light intensity (if light exists)
                if (torch.torchLight != null)
                {
                    float currentPct = torch.torchLight.intensity / torch.maxPhysicalIntensity;
                    torch.intensitySlider.value = Mathf.Clamp(currentPct * 255f, 0f, 255f);
                }

                // Add slider listener to update light intensity dynamically
                TorchSettings currentTorch = torch;
                torch.intensitySlider.onValueChanged.AddListener((value) =>
                {
                    UpdateLightIntensity(currentTorch, value);
                });
            }

            // Subscribe to grab events
            if (torch.grabInteractable != null)
            {
                torch.grabInteractable.selectEntered.AddListener(OnTorchGrabbed);
                torch.grabInteractable.selectExited.AddListener(OnTorchReleased);
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        foreach (var torch in torches)
        {
            if (torch.grabInteractable != null)
            {
                torch.grabInteractable.selectEntered.RemoveListener(OnTorchGrabbed);
                torch.grabInteractable.selectExited.RemoveListener(OnTorchReleased);
            }
        }
    }

    private void Update()
    {
        if (activeTorch == null || activeTorch.intensitySlider == null) return;

        float adjustment = 0f;

        // 1. Keyboard Inputs ('I' to increase, 'K' to decrease)
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.iKey.isPressed)
            {
                adjustment += adjustmentSpeed * Time.deltaTime;
            }
            if (Keyboard.current.kKey.isPressed)
            {
                adjustment -= adjustmentSpeed * Time.deltaTime;
            }
        }
#else
        if (Input.GetKey(KeyCode.I))
        {
            adjustment += adjustmentSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.K))
        {
            adjustment -= adjustmentSpeed * Time.deltaTime;
        }
#endif

        // 2. VR Thumbstick Input (Primary2DAxis Y value of the grabbing controller)
        UnityEngine.XR.InputDevice device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(activeGrabNode);
        if (device.isValid)
        {
            Vector2 thumbstickValue;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out thumbstickValue))
            {
                // Check if the input is outside a deadzone to prevent stick drift
                if (Mathf.Abs(thumbstickValue.y) > 0.1f)
                {
                    adjustment += thumbstickValue.y * adjustmentSpeed * Time.deltaTime;
                }
            }
        }

        // Apply adjustment to the active slider
        if (Mathf.Abs(adjustment) > 0.01f)
        {
            float targetValue = Mathf.Clamp(activeTorch.intensitySlider.value + adjustment, 0f, 255f);
            activeTorch.intensitySlider.value = targetValue;
        }
    }

    /// <summary>
    /// Updates the intensity of the torch's Light component relative to the 0-255 slider value.
    /// </summary>
    private void UpdateLightIntensity(TorchSettings torch, float sliderValue)
    {
        if (torch.torchLight != null)
        {
            torch.torchLight.intensity = (sliderValue / 255f) * torch.maxPhysicalIntensity;
        }
    }

    /// <summary>
    /// Event handler when a torch is grabbed.
    /// </summary>
    private void OnTorchGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactableObject == null) return;

        TorchSettings torch = GetTorchSettings(args.interactableObject);
        if (torch == null) return;

        // Determine which hand grabbed the torch
        XRNode grabNode = DetermineGrabNode(args.interactorObject);

        // Track the grab
        if (torch.grabInteractable != null)
        {
            grabbedTorches[torch.grabInteractable] = grabNode;
        }

        // Set the active torch and active node
        activeTorch = torch;
        activeGrabNode = grabNode;

        // Display the UI panel
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Event handler when a torch is released.
    /// </summary>
    private void OnTorchReleased(SelectExitEventArgs args)
    {
        if (args.interactableObject == null) return;

        TorchSettings torch = GetTorchSettings(args.interactableObject);
        if (torch == null) return;

        if (torch.grabInteractable != null && grabbedTorches.ContainsKey(torch.grabInteractable))
        {
            grabbedTorches.Remove(torch.grabInteractable);
        }

        // If there are other torches currently held, switch active focus to one of them
        if (grabbedTorches.Count > 0)
        {
            var enumerator = grabbedTorches.GetEnumerator();
            enumerator.MoveNext(); // Move to the first item
            
            XRGrabInteractable remainingInteractable = enumerator.Current.Key;
            XRNode remainingNode = enumerator.Current.Value;

            activeTorch = GetTorchSettings(remainingInteractable);
            activeGrabNode = remainingNode;
        }
        else
        {
            // No torches are currently grabbed
            activeTorch = null;

            // Hide the UI panel
            if (uiPanel != null)
            {
                uiPanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Helper to find settings associated with a specific interactable.
    /// </summary>
    private TorchSettings GetTorchSettings(IXRSelectInteractable interactable)
    {
        return torches.Find(t => t.grabInteractable != null && (t.grabInteractable == interactable as XRGrabInteractable || t.grabInteractable == interactable));
    }

    /// <summary>
    /// Utility to check whether the interactor belongs to the left or right controller.
    /// </summary>
    private XRNode DetermineGrabNode(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
    {
        if (interactor == null) return XRNode.RightHand;

        // Check the hierarchy or GameObject names for clues (e.g. "left", "right")
        string interactorName = interactor.transform.name.ToLower();
        if (interactor.transform.parent != null)
        {
            interactorName += " " + interactor.transform.parent.name.ToLower();
        }

        if (interactorName.Contains("left"))
        {
            return XRNode.LeftHand;
        }
        if (interactorName.Contains("right"))
        {
            return XRNode.RightHand;
        }

        // Fallback: Default to Right Hand if cannot determine
        return XRNode.RightHand;
    }
}
