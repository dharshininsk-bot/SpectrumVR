using UnityEngine;

public class LightSource : MonoBehaviour
{
    [Header("Settings")]
    public Color lightColor = Color.red;
    public bool isOn = true;
    public float maxDistance = 15f;
    public LayerMask targetLayer;

    [Header("References")]
    [SerializeField] private Light unityLight;
    [SerializeField] private GameObject beamVisual; // Optional cone visual mesh

    private void Start()
    {
        if (unityLight == null) unityLight = GetComponentInChildren<Light>();
        UpdateLightState();
    }

    private void Update()
    {
        if (!isOn) return;

        // Cast ray to detect if pointing at the receiver
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxDistance, targetLayer))
        {
            ColorReceiver receiver = hit.collider.GetComponent<ColorReceiver>();
            if (receiver != null)
            {
                receiver.RegisterLightContribution(lightColor);
            }
        }
    }

    public void ToggleLight()
    {
        isOn = !isOn;
        UpdateLightState();
    }

    private void UpdateLightState()
    {
        if (unityLight != null) unityLight.enabled = isOn;
        if (beamVisual != null) beamVisual.SetActive(isOn);
    }
}
