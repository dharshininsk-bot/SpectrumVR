using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeRoom.Tutorial
{
    public enum PaletteSlotType
    {
        BaseColor,  // Always holds a fixed CMY color. Clicking gives that color to the wand.
        MixSlot     // Empty at start. Accepts a color from the wand, or mixes two colors together.
    }

    /// <summary>
    /// Attach to each of the 9 palette slot GameObjects.
    /// Requires an XRSimpleInteractable so the XR Ray/Direct Interactor can click it.
    /// The slot itself changes color - no blobs involved.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class PaletteSlot : MonoBehaviour
    {
        [Header("Slot Type")]
        public PaletteSlotType slotType = PaletteSlotType.MixSlot;

        [Header("Base Color Settings")]
        [Tooltip("For BaseColor slots only: the fixed color this slot always provides.")]
        public Color baseColor = Color.cyan;

        [Header("Visuals")]
        [Tooltip("The Renderer of this slot's visual (e.g. the paint pool mesh). Its material color will be updated.")]
        public Renderer slotRenderer;

        // Runtime state
        [Header("Runtime State (Read Only)")]
        [SerializeField] private bool _hasColor = false;
        [SerializeField] private Color _currentSlotColor = Color.clear;

        private XRSimpleInteractable _interactable;

        // The neutral/empty color for mix slots
        private static readonly Color EmptyColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();

            if (slotRenderer == null)
                slotRenderer = GetComponentInChildren<Renderer>();
        }

        private void Start()
        {
            if (slotType == PaletteSlotType.BaseColor)
            {
                // Base slots are always "full" with their color
                _hasColor = true;
                _currentSlotColor = baseColor;
            }
            else
            {
                // Mix slots start empty
                _hasColor = false;
                _currentSlotColor = EmptyColor;
            }

            ApplyColorToSlotVisual(_currentSlotColor);
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.selectEntered.AddListener(OnSlotClicked);
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.selectEntered.RemoveListener(OnSlotClicked);
        }

        private void OnSlotClicked(SelectEnterEventArgs args)
        {
            // Find the ColorWand on the interactor that clicked us
            ColorWand wand = ColorWand.FromInteractor(args.interactorObject as XRBaseInteractor);
            if (wand == null)
            {
                Debug.LogWarning($"[PaletteSlot] {gameObject.name} was clicked but no ColorWand found on the interactor.", this);
                return;
            }

            // Water cleaning — only clears mix slots, not base CMY slots
            if (wand.HasWater)
            {
                if (slotType == PaletteSlotType.MixSlot)
                {
                    wand.ConsumeWater();
                    ClearSlot();
                }
                else
                {
                    Debug.Log($"[PaletteSlot] Can't clean base color slot '{gameObject.name}' with water.");
                }
                return;
            }

            if (slotType == PaletteSlotType.BaseColor)
            {
                HandleBaseSlotClick(wand);
            }
            else
            {
                HandleMixSlotClick(wand);
            }
        }

        private void ClearSlot()
        {
            _hasColor = false;
            _currentSlotColor = EmptyColor;
            ApplyColorToSlotVisual(_currentSlotColor);

            // Fallback: also set directly on the material instance in case the shader
            // doesn't respond to MaterialPropertyBlock (e.g. non-URP shaders)
            if (slotRenderer != null)
            {
                if (slotRenderer.material.HasProperty("_BaseColor"))
                    slotRenderer.material.SetColor("_BaseColor", EmptyColor);
                if (slotRenderer.material.HasProperty("_Color"))
                    slotRenderer.material.color = EmptyColor;

                // Disable emission so the slot truly looks "empty"
                slotRenderer.material.DisableKeyword("_EMISSION");
            }

            Debug.Log($"[PaletteSlot] Mix slot '{gameObject.name}' was cleaned with water.");
        }

        private void HandleBaseSlotClick(ColorWand wand)
        {
            // Base slots always give their color to the wand (regardless of wand state)
            wand.PickUpColor(baseColor);
            Debug.Log($"[PaletteSlot] Base slot '{gameObject.name}' gave {baseColor} to {wand.gameObject.name}");
        }

        private void HandleMixSlotClick(ColorWand wand)
        {
            if (wand.HasColor)
            {
                // Wand is bringing a color to this slot
                Color incomingColor = wand.ConsumeColor();

                if (!_hasColor)
                {
                    // Slot is empty — fill it with the incoming color
                    _hasColor = true;
                    _currentSlotColor = incomingColor;
                    ApplyColorToSlotVisual(_currentSlotColor);
                    Debug.Log($"[PaletteSlot] Mix slot '{gameObject.name}' filled with {_currentSlotColor}");
                }
                else
                {
                    // Slot already has a color — mix!
                    Color mixed = ColorMixer.MixSubtractive(_currentSlotColor, incomingColor);
                    _currentSlotColor = mixed;
                    ApplyColorToSlotVisual(_currentSlotColor);
                    Debug.Log($"[PaletteSlot] Mix slot '{gameObject.name}' mixed into {_currentSlotColor}");
                }
            }
            else
            {
                // Wand is empty — pick up this slot's color if it has one
                if (_hasColor)
                {
                    wand.PickUpColor(_currentSlotColor);

                    // Do NOT clear the slot — the player can keep picking the same mixed color
                    Debug.Log($"[PaletteSlot] Mix slot '{gameObject.name}' gave {_currentSlotColor} to {wand.gameObject.name}");
                }
                else
                {
                    Debug.Log($"[PaletteSlot] Mix slot '{gameObject.name}' is empty and wand has no color. Nothing happened.");
                }
            }
        }

        private void ApplyColorToSlotVisual(Color color)
        {
            if (slotRenderer == null) return;

            // MaterialPropertyBlock path (preferred, no new material instance)
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            slotRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            slotRenderer.SetPropertyBlock(block);

            // Direct material fallback — ensures the change is visible on any shader type
            if (slotRenderer.material.HasProperty("_BaseColor"))
                slotRenderer.material.SetColor("_BaseColor", color);
            if (slotRenderer.material.HasProperty("_Color"))
                slotRenderer.material.color = color;
        }
    }
}
