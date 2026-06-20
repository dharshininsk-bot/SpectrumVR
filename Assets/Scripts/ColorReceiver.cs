using UnityEngine;
using UnityEngine.Events;

public class ColorReceiver : MonoBehaviour
{
    [Header("Target Color Settings")]
    [Tooltip("The combined color required to trigger the event (e.g. Yellow (1,1,0), Magenta (1,0,1), Cyan (0,1,1), White (1,1,1))")]
    public Color targetColor = Color.yellow;
    
    [Tooltip("How close the combined color must be to the target color (0.01 to 0.1 is standard)")]
    public float tolerance = 0.1f;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private Renderer displayRenderer;
    [SerializeField] private string colorPropertyName = "_EmissionColor";

    [Header("Events")]
    public UnityEvent OnColorMatched;
    public UnityEvent OnColorUnmatched;

    private Color currentAccumulatedColor = Color.black;
    private bool isMatched = false;

    // We will clear the color in LateUpdate so that all LightSource Update() calls have registered their color.
    private void LateUpdate()
    {
        UpdateVisuals();
        CheckColorMatch();
        
        // Reset color for the next frame
        currentAccumulatedColor = Color.black;
    }

    public void RegisterLightContribution(Color color)
    {
        // Additive color mixing
        currentAccumulatedColor.r = Mathf.Clamp01(currentAccumulatedColor.r + color.r);
        currentAccumulatedColor.g = Mathf.Clamp01(currentAccumulatedColor.g + color.g);
        currentAccumulatedColor.b = Mathf.Clamp01(currentAccumulatedColor.b + color.b);
    }

    private void CheckColorMatch()
    {
        // Check if current color is close to target color
        float diffR = Mathf.Abs(currentAccumulatedColor.r - targetColor.r);
        float diffG = Mathf.Abs(currentAccumulatedColor.g - targetColor.g);
        float diffB = Mathf.Abs(currentAccumulatedColor.b - targetColor.b);

        bool currentlyMatched = (diffR <= tolerance && diffG <= tolerance && diffB <= tolerance);

        if (currentlyMatched && !isMatched)
        {
            isMatched = true;
            OnColorMatched?.Invoke();
            Debug.Log("Color match achieved!");
        }
        else if (!currentlyMatched && isMatched)
        {
            isMatched = false;
            OnColorUnmatched?.Invoke();
            Debug.Log("Color unmatched.");
        }
    }

    private void UpdateVisuals()
    {
        if (displayRenderer != null)
        {
            // If using a material that shows the color (e.g. emission)
            Material mat = displayRenderer.material;
            mat.color = currentAccumulatedColor;
            if (mat.HasProperty(colorPropertyName))
            {
                mat.SetColor(colorPropertyName, currentAccumulatedColor * 2f); // boost emission intensity
            }
        }
    }
}
