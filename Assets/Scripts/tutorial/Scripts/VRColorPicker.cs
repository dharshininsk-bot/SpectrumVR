using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class VRColorPicker : MonoBehaviour, IPointerClickHandler
{
    private Image colorWheelImage;
    private Texture2D colorTexture;

    [Header("Target Configuration")]
    [Tooltip("Drag the 3D Object or VR Lens Renderer here.")]
    public Renderer targetRenderer;

    void Start()
    {
        // 1. Check for Image Component
        colorWheelImage = GetComponent<Image>();
        if (colorWheelImage == null)
        {
            Debug.LogError($"[VRColorPicker] ON {gameObject.name}: No UI Image component found! This script must be placed directly on the GameObject with the color wheel Image component.", this);
            return;
        }

        // 2. Check for RectTransform
        if (colorWheelImage.rectTransform == null)
        {
            Debug.LogError($"[VRColorPicker] ON {gameObject.name}: RectTransform is missing or null.", this);
            return;
        }

        // 3. Check for Assigned Sprite Texture
        if (colorWheelImage.sprite == null)
        {
            Debug.LogError($"[VRColorPicker] ON {gameObject.name}: The UI Image component has no Source Image assigned! Drag your color wheel sprite asset into the 'Source Image' slot.", this);
            return;
        }

        colorTexture = colorWheelImage.sprite.texture;

        // 4. Verify Target Renderer assignment
        if (targetRenderer == null)
        {
            Debug.LogWarning($"[VRColorPicker] ON {gameObject.name}: Target Renderer is currently empty. Make sure to assign a 3D object or lens in the inspector.", this);
        }
        else
        {
            Debug.Log($"[VRColorPicker] '{gameObject.name}' initialized successfully. Target assigned: {targetRenderer.gameObject.name}", this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Guard clause in case initialization failed
        if (colorWheelImage == null || colorTexture == null)
        {
            Debug.LogError($"[VRColorPicker] Click ignored on {gameObject.name}: Script components were not correctly initialized at Start.", this);
            return;
        }

        if (targetRenderer == null)
        {
            Debug.LogError($"[VRColorPicker] Click ignored on {gameObject.name}: No Target Renderer assigned to apply the color to!", this);
            return;
        }

        // 1. Calculate local hit coordinates on the UI component element
        Vector2 localPoint;
        bool didHit = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            colorWheelImage.rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        );

        if (!didHit)
        {
            Debug.LogWarning($"[VRColorPicker] Could not map screen point to local point coordinates on {gameObject.name}.", this);
            return;
        }

        // 2. Convert local position to normalized texture coordinates (range 0 to 1)
        float rectWidth = colorWheelImage.rectTransform.rect.width;
        float rectHeight = colorWheelImage.rectTransform.rect.height;
        
        float normalizedX = (localPoint.x + rectWidth * 0.5f) / rectWidth;
        float normalizedY = (localPoint.y + rectHeight * 0.5f) / rectHeight;

        // Clamp coordinates to stay within bounds if the user clicks slightly outside the margins
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // 3. Map normalized coordinates directly onto pixels 
        int pixelX = (int)(normalizedX * colorTexture.width);
        int pixelY = (int)(normalizedY * colorTexture.height);

        // Try-Catch protection in case the texture asset has Read/Write settings disabled
        try
        {
            Color pickedColor = colorTexture.GetPixel(pixelX, pixelY);
            
            // Console confirmation path tracking
            Debug.Log($"[VRColorPicker] Click registered on {gameObject.name}. Coordinates: (X:{pixelX}, Y:{pixelY}) -> RGB Color Output: {pickedColor}", this);
            
            ApplyColorToTarget(pickedColor);
        }
        catch (UnityException e)
        {
            Debug.LogError($"[VRColorPicker] CRITICAL ERROR on {gameObject.name}: Cannot read pixel colors from texture. Go to your Color Wheel asset import settings in the Inspector and make sure 'Read/Write' is CHECKED. System Error: {e.Message}", this);
        }
    }

    private void ApplyColorToTarget(Color newColor)
    {
        // Check if the assigned material has our custom Shader Graph variable properties
        if (targetRenderer.material.HasProperty("_LensColor"))
        {
            targetRenderer.material.SetColor("_LensColor", newColor);
            Debug.Log($"[VRColorPicker] Color updated successfully using Subtractive Shader property '_LensColor' on {targetRenderer.gameObject.name}.", this);
        }
        else
        {
            targetRenderer.material.color = newColor;
            Debug.Log($"[VRColorPicker] Color updated successfully using standard material surface property on {targetRenderer.gameObject.name}.", this);
        }
    }
}