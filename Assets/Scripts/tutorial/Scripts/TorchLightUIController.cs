using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls a single torch's light intensity and syncs it with a specific UI slider.
/// Attach this script directly to each of your torch GameObjects (Red, Green, Blue).
/// Pushing the thumbstick Up/Down (I/K keys) on the grabbing controller adjusts this torch's light.
/// </summary>
public class TorchLightUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The shared head-locked Canvas GameObject.")]
    [SerializeField] private Canvas torchUiCanvas;

    [Tooltip("The Slider UI component for this specific torch (e.g. Red Slider).")]
    [SerializeField] private Slider slider;

    [Tooltip("The label Graphic (Text/Image) for this specific torch in the UI.")]
    [SerializeField] private Graphic label;

    [Header("Light Reference")]
    [Tooltip("The Light component of this torch.")]
    [SerializeField] private Light torchLight;

    [Tooltip("The maximum intensity this light can reach when the slider is at max.")]
    [SerializeField] private float maxLightIntensity = 500f;

    [Header("Interactable Reference")]
    [Tooltip("The XRGrabInteractable component of this torch. Left blank to auto-detect on this GameObject.")]
    [SerializeField] private XRGrabInteractable torchInteractable;

    [Header("Input Configuration")]
#if UNITY_INPUT_SYSTEM
    [Tooltip("Optional custom action reference for the Left Hand Thumbstick (2D Vector). If left unassigned, it will auto-detect the controller.")]
    [SerializeField] private InputActionProperty leftThumbstickAction;

    [Tooltip("Optional custom action reference for the Right Hand Thumbstick (2D Vector). If left unassigned, it will auto-detect the controller.")]
    [SerializeField] private InputActionProperty rightThumbstickAction;
#endif

    [Header("Visual Feedback Settings")]
    [Tooltip("Scale multiplier for this slider when the torch is held.")]
    [SerializeField] private float selectedScaleMultiplier = 1.15f;

    [Tooltip("Speed of transition for scale and color interpolation.")]
    [SerializeField] private float transitionSpeed = 8f;

    [Tooltip("Color of the label when this torch is held.")]
    [SerializeField] private Color activeLabelColor = Color.white;

    [Tooltip("Color of the label when this torch is not held.")]
    [SerializeField] private Color inactiveLabelColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

    private Vector3 defaultSliderScale;
    private bool isHeld = false;
    private bool isLeftHandHolding = false;

    // Static variables shared across all instances to coordinate UI and locomotion
    private static int activeTorchCount = 0;
    private static int locomotionDisableCount = 0;
    private static List<LocomotionProvider> disabledLocomotionProviders = new List<LocomotionProvider>();

    private void Start()
    {
        // Auto-detect the interactable on this GameObject if not assigned
        if (torchInteractable == null)
        {
            torchInteractable = GetComponent<XRGrabInteractable>();
        }

        // Register grab and drop events
        if (torchInteractable != null)
        {
            torchInteractable.selectEntered.AddListener(OnGrabbed);
            torchInteractable.selectExited.AddListener(OnDropped);
        }

        // Synchronize the slider changes with the light component
        if (slider != null)
        {
            defaultSliderScale = slider.transform.localScale;
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // Read the initial light intensity to initialize the slider value
        InitializeSliderValue();

        // Initially hide the canvas at start (if no torches are held)
        if (torchUiCanvas != null && activeTorchCount == 0)
        {
            torchUiCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unregister listeners to avoid memory leaks
        if (torchInteractable != null)
        {
            torchInteractable.selectEntered.RemoveListener(OnGrabbed);
            torchInteractable.selectExited.RemoveListener(OnDropped);
        }
    }

    private void OnEnable()
    {
#if UNITY_INPUT_SYSTEM
        // Ensure input actions are active
        if (leftThumbstickAction.action != null) leftThumbstickAction.action.Enable();
        if (rightThumbstickAction.action != null) rightThumbstickAction.action.Enable();
#endif
    }

    private void Update()
    {
        // Always smoothly update visual states (even when not held, to ease scale and color back to normal)
        UpdateVisuals();

        if (!isHeld) return;

        // Get the active hand's thumbstick input
        Vector2 thumbstick = GetThumbstickInput();

        // Adjust the value of this slider using the vertical axis (I and K keys in simulator)
        HandleVerticalAdjustment(thumbstick);
    }

    /// <summary>
    /// Reads the thumbstick value from the input action, direct input system device, or fallback.
    /// </summary>
    private Vector2 GetThumbstickInput()
    {
#if UNITY_INPUT_SYSTEM
        // 1. Try reading from assigned input actions
        InputActionProperty activeAction = isLeftHandHolding ? leftThumbstickAction : rightThumbstickAction;
        if (activeAction.action != null)
        {
            return activeAction.action.ReadValue<Vector2>();
        }

        // 2. Fallback: Search active InputSystem devices for the controller
        foreach (var device in InputSystem.devices)
        {
            if (device is UnityEngine.InputSystem.XR.XRController xrController)
            {
                bool isLeftDevice = xrController.usages.Contains(UnityEngine.InputSystem.CommonUsages.LeftHand) || 
                                    xrController.name.ToLower().Contains("left");
                
                if (isLeftDevice == isLeftHandHolding)
                {
                    var thumbstickControl = xrController.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("thumbstick");
                    if (thumbstickControl == null)
                    {
                        thumbstickControl = xrController.GetChildControl<UnityEngine.InputSystem.Controls.Vector2Control>("primary2DAxis");
                    }

                    if (thumbstickControl != null)
                    {
                        return thumbstickControl.ReadValue();
                    }
                }
            }
        }
#endif

        // 3. Fallback: Legacy Input Device based input (for projects not using Input System)
        InputDevice legacyDevice = InputDevices.GetDeviceAtXRNode(isLeftHandHolding ? XRNode.LeftHand : XRNode.RightHand);
        if (legacyDevice.isValid)
        {
            if (legacyDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value))
            {
                return value;
            }
        }

        return Vector2.zero;
    }

    /// <summary>
    /// Adjusts the value of this slider using the vertical thumbstick axis (I and K keys).
    /// </summary>
    private void HandleVerticalAdjustment(Vector2 thumbstick)
    {
        if (Mathf.Abs(thumbstick.y) > 0.05f && slider != null)
        {
            // Dynamically scale movement speed based on slider range.
            // A division by 2 means it will take exactly 2 seconds of full input to go from 0 to 255.
            float range = slider.maxValue - slider.minValue;
            float speed = range > 0 ? range / 2f : 1f;

            float change = thumbstick.y * speed * Time.deltaTime;
            slider.value = Mathf.Clamp(slider.value + change, slider.minValue, slider.maxValue);
        }
    }

    /// <summary>
    /// Smoothly updates the visual representation of active vs. inactive slider and label.
    /// </summary>
    private void UpdateVisuals()
    {
        // Smoothly scale the slider
        if (slider != null)
        {
            Vector3 targetScale = defaultSliderScale * (isHeld ? selectedScaleMultiplier : 1.0f);
            slider.transform.localScale = Vector3.Lerp(slider.transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
        }

        // Smoothly transition the color of the label
        if (label != null)
        {
            Color targetColor = isHeld ? activeLabelColor : inactiveLabelColor;
            label.color = Color.Lerp(label.color, targetColor, Time.deltaTime * transitionSpeed);
        }
    }

    /// <summary>
    /// Syncs the light component intensity when the slider value is changed.
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        if (slider != null && torchLight != null)
        {
            float range = slider.maxValue - slider.minValue;
            float normalized = range > 0 ? (value - slider.minValue) / range : 0f;
            torchLight.intensity = normalized * maxLightIntensity;
        }
    }

    /// <summary>
    /// Initializes the slider value based on the starting intensity of the light source.
    /// </summary>
    private void InitializeSliderValue()
    {
        if (slider != null && torchLight != null)
        {
            float normalized = Mathf.Clamp01(torchLight.intensity / maxLightIntensity);
            slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, normalized);
        }
    }

    /// <summary>
    /// Finds and disables all locomotion providers in the scene.
    /// Uses a static counter to ensure locomotion is only disabled once when the first torch is grabbed.
    /// </summary>
    private static void DisableLocomotionGlobal()
    {
        locomotionDisableCount++;
        if (locomotionDisableCount == 1)
        {
            disabledLocomotionProviders.Clear();
            LocomotionProvider[] providers = FindObjectsOfType<LocomotionProvider>();
            foreach (var provider in providers)
            {
                if (provider != null && provider.enabled)
                {
                    provider.enabled = false;
                    disabledLocomotionProviders.Add(provider);
                }
            }
        }
    }

    /// <summary>
    /// Restores locomotion providers that were disabled.
    /// Restores locomotion only when all torches have been dropped.
    /// </summary>
    private static void RestoreLocomotionGlobal()
    {
        locomotionDisableCount = Mathf.Max(0, locomotionDisableCount - 1);
        if (locomotionDisableCount == 0)
        {
            foreach (var provider in disabledLocomotionProviders)
            {
                if (provider != null)
                {
                    provider.enabled = true;
                }
            }
            disabledLocomotionProviders.Clear();
        }
    }

    /// <summary>
    /// Callback triggered when this torch is grabbed.
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject != null)
        {
            GameObject interactorGO = args.interactorObject.transform.gameObject;
            isLeftHandHolding = interactorGO.name.ToLower().Contains("left");
        }

        activeTorchCount++;
        
        // Show UI Canvas
        if (torchUiCanvas != null)
        {
            torchUiCanvas.gameObject.SetActive(true);
        }

        // Disable locomotion
        DisableLocomotionGlobal();

        isHeld = true;
    }

    /// <summary>
    /// Callback triggered when this torch is dropped.
    /// </summary>
    private void OnDropped(SelectExitEventArgs args)
    {
        activeTorchCount = Mathf.Max(0, activeTorchCount - 1);

        // Hide UI Canvas only if no torches are held
        if (activeTorchCount == 0 && torchUiCanvas != null)
        {
            torchUiCanvas.gameObject.SetActive(false);
        }

        // Restore locomotion
        RestoreLocomotionGlobal();

        isHeld = false;
    }
}
