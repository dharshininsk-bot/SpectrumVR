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
/// Controls monitor color dynamically based on Red, Green, and Blue subpixel cube grab interactions.
/// Grabbed cubes remain in place, and their corresponding slider is adjusted using keyboard (I/K) or thumbstick.
/// </summary>
public class PixelColorController : MonoBehaviour
{
    public enum ColorChannel
    {
        None,
        Red,
        Green,
        Blue
    }

    [Header("Subpixel Cube Interactables")]
    [Tooltip("The Red subpixel cube interactable.")]
    [SerializeField] private XRGrabInteractable redCube;

    [Tooltip("The Green subpixel cube interactable.")]
    [SerializeField] private XRGrabInteractable greenCube;

    [Tooltip("The Blue subpixel cube interactable.")]
    [SerializeField] private XRGrabInteractable blueCube;

    [Header("Monitor Settings")]
    [Tooltip("The MeshRenderer/Renderer of the monitor (e.g. plane mesh).")]
    [SerializeField] private Renderer monitorRenderer;

    [Header("UI Slider Settings")]
    [Tooltip("The parent GameObject containing the UI canvas/panel.")]
    [SerializeField] private GameObject uiPanel;

    [Tooltip("The Red intensity slider (0-255).")]
    [SerializeField] private Slider redSlider;

    [Tooltip("The Green intensity slider (0-255).")]
    [SerializeField] private Slider greenSlider;

    [Tooltip("The Blue intensity slider (0-255).")]
    [SerializeField] private Slider blueSlider;

    [Header("Adjustment Settings")]
    [Tooltip("How fast the slider adjusts when using the thumbstick or keyboard (units per second, 0 to 255 range).")]
    [SerializeField] private float adjustmentSpeed = 100f;

    // Track active grabbing states
    private Dictionary<XRGrabInteractable, XRNode> grabbedCubes = new Dictionary<XRGrabInteractable, XRNode>();
    private ColorChannel activeChannel = ColorChannel.None;
    private XRNode activeGrabNode = XRNode.RightHand;

    // Cached material of the monitor to change color
    private Material monitorMaterial;

    private void Awake()
    {
        // Auto-configure the sliders to range from 0 to 255 if not already set in Inspector
        ConfigureSlider(redSlider);
        ConfigureSlider(greenSlider);
        ConfigureSlider(blueSlider);
    }

    private void ConfigureSlider(Slider slider)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 255f;
        }
    }

    private void Start()
    {
        // Hide UI panel initially
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        // Cache monitor material
        if (monitorRenderer != null)
        {
            monitorMaterial = monitorRenderer.material;
        }

        // Apply initial color to monitor
        UpdateMonitorColor();

        // Setup sliders listeners
        if (redSlider != null) redSlider.onValueChanged.AddListener((val) => UpdateMonitorColor());
        if (greenSlider != null) greenSlider.onValueChanged.AddListener((val) => UpdateMonitorColor());
        if (blueSlider != null) blueSlider.onValueChanged.AddListener((val) => UpdateMonitorColor());

        // Setup cube interactables
        SetupCube(redCube, OnRedGrabbed, OnRedReleased);
        SetupCube(greenCube, OnGreenGrabbed, OnGreenReleased);
        SetupCube(blueCube, OnBlueGrabbed, OnBlueReleased);
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid leaks
        CleanupCube(redCube, OnRedGrabbed, OnRedReleased);
        CleanupCube(greenCube, OnGreenGrabbed, OnGreenReleased);
        CleanupCube(blueCube, OnBlueGrabbed, OnBlueReleased);
    }

    private void SetupCube(XRGrabInteractable cube, UnityEngine.Events.UnityAction<SelectEnterEventArgs> grabAction, UnityEngine.Events.UnityAction<SelectExitEventArgs> releaseAction)
    {
        if (cube != null)
        {
            // Crucial: Prevent the cube from moving or rotating when grabbed
            cube.trackPosition = false;
            cube.trackRotation = false;

            cube.selectEntered.AddListener(grabAction);
            cube.selectExited.AddListener(releaseAction);
        }
    }

    private void CleanupCube(XRGrabInteractable cube, UnityEngine.Events.UnityAction<SelectEnterEventArgs> grabAction, UnityEngine.Events.UnityAction<SelectExitEventArgs> releaseAction)
    {
        if (cube != null)
        {
            cube.selectEntered.RemoveListener(grabAction);
            cube.selectExited.RemoveListener(releaseAction);
        }
    }

    private void Update()
    {
        if (activeChannel == ColorChannel.None) return;

        Slider targetSlider = GetActiveSlider();
        if (targetSlider == null) return;

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
                // Deadzone check
                if (Mathf.Abs(thumbstickValue.y) > 0.1f)
                {
                    adjustment += thumbstickValue.y * adjustmentSpeed * Time.deltaTime;
                }
            }
        }

        // Apply adjustment to the active slider
        if (Mathf.Abs(adjustment) > 0.01f)
        {
            targetSlider.value = Mathf.Clamp(targetSlider.value + adjustment, 0f, 255f);
        }
    }

    private Slider GetActiveSlider()
    {
        switch (activeChannel)
        {
            case ColorChannel.Red: return redSlider;
            case ColorChannel.Green: return greenSlider;
            case ColorChannel.Blue: return blueSlider;
            default: return null;
        }
    }

    private void UpdateMonitorColor()
    {
        if (monitorMaterial != null)
        {
            float r = redSlider != null ? redSlider.value / 255f : 0f;
            float g = greenSlider != null ? greenSlider.value / 255f : 0f;
            float b = blueSlider != null ? blueSlider.value / 255f : 0f;

            Color finalColor = new Color(r, g, b, 1f);

            // Update standard base/diffuse color
            monitorMaterial.color = finalColor;

            // Update emission color if the material uses emission (for glowing screens/monitors)
            if (monitorMaterial.HasProperty("_EmissionColor"))
            {
                monitorMaterial.SetColor("_EmissionColor", finalColor);
                
                // Ensure emission keyword is enabled dynamically on the material
                monitorMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    // --- Grab and Release Handlers for each subpixel cube ---

    private void OnRedGrabbed(SelectEnterEventArgs args) => CubeGrabbed(redCube, ColorChannel.Red, args);
    private void OnRedReleased(SelectExitEventArgs args) => CubeReleased(redCube, args);

    private void OnGreenGrabbed(SelectEnterEventArgs args) => CubeGrabbed(greenCube, ColorChannel.Green, args);
    private void OnGreenReleased(SelectExitEventArgs args) => CubeReleased(greenCube, args);

    private void OnBlueGrabbed(SelectEnterEventArgs args) => CubeGrabbed(blueCube, ColorChannel.Blue, args);
    private void OnBlueReleased(SelectExitEventArgs args) => CubeReleased(blueCube, args);

    private void CubeGrabbed(XRGrabInteractable cube, ColorChannel channel, SelectEnterEventArgs args)
    {
        if (args.interactableObject == null) return;

        XRNode grabNode = DetermineGrabNode(args.interactorObject);
        grabbedCubes[cube] = grabNode;

        activeChannel = channel;
        activeGrabNode = grabNode;

        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }
    }

    private void CubeReleased(XRGrabInteractable cube, SelectExitEventArgs args)
    {
        if (grabbedCubes.ContainsKey(cube))
        {
            grabbedCubes.Remove(cube);
        }

        if (grabbedCubes.Count > 0)
        {
            // Switch active focus to one of the remaining held cubes
            var enumerator = grabbedCubes.GetEnumerator();
            enumerator.MoveNext();
            
            XRGrabInteractable remainingCube = enumerator.Current.Key;
            XRNode remainingNode = enumerator.Current.Value;

            activeChannel = GetChannelFromCube(remainingCube);
            activeGrabNode = remainingNode;
        }
        else
        {
            activeChannel = ColorChannel.None;
            if (uiPanel != null)
            {
                uiPanel.SetActive(false);
            }
        }
    }

    private ColorChannel GetChannelFromCube(XRGrabInteractable cube)
    {
        if (cube == redCube) return ColorChannel.Red;
        if (cube == greenCube) return ColorChannel.Green;
        if (cube == blueCube) return ColorChannel.Blue;
        return ColorChannel.None;
    }

    private XRNode DetermineGrabNode(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
    {
        if (interactor == null) return XRNode.RightHand;

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

        return XRNode.RightHand;
    }
}
