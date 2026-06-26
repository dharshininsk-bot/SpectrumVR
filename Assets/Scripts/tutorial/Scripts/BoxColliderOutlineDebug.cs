using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[ExecuteAlways] // Allows it to render and update in the editor scene view
public class BoxColliderOutlineDebug : MonoBehaviour
{
    [Header("Outline Appearance")]
    [Tooltip("Color of the box collider outline.")]
    [SerializeField] private Color lineColor = new Color(0f, 1f, 0.5f, 0.8f); // Bright neon green/cyan

    [Tooltip("Width of the outline lines in meters.")]
    [SerializeField] private float lineWidth = 0.02f;

    [Tooltip("Optional custom material. If null, a simple unlit material will be generated.")]
    [SerializeField] private Material customMaterial;

    private BoxCollider boxCollider;
    private LineRenderer lineRenderer;
    private Material generatedMaterial;

    private Vector3 lastCenter;
    private Vector3 lastSize;

    // The 16-point path that traces all 12 edges of a cube using a single continuous line
    private static readonly int[] PathIndices = new int[]
    {
        0, 1, 2, 3, 0, // Bottom face loop
        4, 5, 6, 7, 4, // Up to top face and top face loop
        7, 3,          // Down front-left
        2, 6,          // Across bottom-front to top-front-right
        5, 1           // Across top-right-back to bottom-right-back
    };

    // Normalized unit corners of a cube from -0.5 to 0.5
    private static readonly Vector3[] UnitCorners = new Vector3[]
    {
        new Vector3(-0.5f, -0.5f, -0.5f), // 0: Bottom-Left-Back
        new Vector3( 0.5f, -0.5f, -0.5f), // 1: Bottom-Right-Back
        new Vector3( 0.5f, -0.5f,  0.5f), // 2: Bottom-Right-Front
        new Vector3(-0.5f, -0.5f,  0.5f), // 3: Bottom-Left-Front
        new Vector3(-0.5f,  0.5f, -0.5f), // 4: Top-Left-Back
        new Vector3( 0.5f,  0.5f, -0.5f), // 5: Top-Right-Back
        new Vector3( 0.5f,  0.5f,  0.5f), // 6: Top-Right-Front
        new Vector3(-0.5f,  0.5f,  0.5f)  // 7: Top-Left-Front
    };

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        SetupLineRenderer();
    }

    private void Start()
    {
        UpdateOutline();
    }

    private void Update()
    {
        // Only run checks if we have a collider
        if (boxCollider == null) return;

        // Automatically regenerate if collider parameters change at runtime or in editor
        if (boxCollider.center != lastCenter || boxCollider.size != lastSize)
        {
            UpdateOutline();
        }
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Configure LineRenderer properties
        lineRenderer.useWorldSpace = false; // Ensures the outline rotates/scales with the GameObject
        lineRenderer.positionCount = PathIndices.Length;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.loop = false; // Our path manually loops and connects everything

        // Set width
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Setup material
        ApplyMaterial();
    }

    private void ApplyMaterial()
    {
        if (lineRenderer == null) return;

        if (customMaterial != null)
        {
            lineRenderer.sharedMaterial = customMaterial;
            return;
        }

        if (generatedMaterial == null)
        {
            // Try to find a suitable shader for URP, Sprites, or standard built-in
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            if (shader != null)
            {
                generatedMaterial = new Material(shader);
                generatedMaterial.name = "BoxColliderOutline_DebugMaterial";
            }
            else
            {
                // Fallback to basic standard material if absolutely no unlit shader is found
                generatedMaterial = new Material(Shader.Find("Standard"));
            }
        }

        // Apply colors
        generatedMaterial.color = lineColor;
        if (generatedMaterial.HasProperty("_Color"))
        {
            generatedMaterial.SetColor("_Color", lineColor);
        }

        lineRenderer.sharedMaterial = generatedMaterial;
    }

    public void UpdateOutline()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null) return;
        }

        if (lineRenderer == null)
        {
            SetupLineRenderer();
        }

        // Store current parameters to detect changes
        lastCenter = boxCollider.center;
        lastSize = boxCollider.size;

        // Apply current style configurations
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        ApplyMaterial();

        // Calculate and set local positions for the LineRenderer
        Vector3[] localPositions = new Vector3[PathIndices.Length];
        for (int i = 0; i < PathIndices.Length; i++)
        {
            int cornerIndex = PathIndices[i];
            Vector3 unitCorner = UnitCorners[cornerIndex];
            
            // Calculate corner offset based on collider's size and center
            localPositions[i] = lastCenter + Vector3.Scale(lastSize, unitCorner);
        }

        lineRenderer.SetPositions(localPositions);
    }

    private void OnDestroy()
    {
        // Clean up generated material to prevent memory leaks in editor/runtime
        if (generatedMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedMaterial);
            }
            else
            {
                DestroyImmediate(generatedMaterial);
            }
        }
    }

    private void OnValidate()
    {
        // Update live in editor when inspector values change
        if (boxCollider != null && lineRenderer != null)
        {
            UpdateOutline();
        }
    }
}
