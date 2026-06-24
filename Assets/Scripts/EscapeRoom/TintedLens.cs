using UnityEngine;

namespace EscapeRoom
{
    public enum LensColor
    {
        Cyan,
        Orange,
        Magenta,
        Blue,
        Yellow
    }

    [DisallowMultipleComponent]
    public class TintedLens : MonoBehaviour
    {
        [Header("Lens Setup")]
        [Tooltip("The color identifier for this lens.")]
        public LensColor lensColor;

        [Tooltip("The physical filter color of this lens (use 0 or 1 for primary/secondary filters to blend cleanly).")]
        public Color filterColor = Color.white;

        [Tooltip("Optional visual mesh representation of the lens inside the socket.")]
        public GameObject lensMeshVisual;

        private void OnEnable()
        {
            if (LensPuzzleController.Instance != null)
            {
                LensPuzzleController.Instance.UpdatePuzzleState();
            }
        }

        private void OnDisable()
        {
            if (LensPuzzleController.Instance != null)
            {
                LensPuzzleController.Instance.UpdatePuzzleState();
            }
        }

        private void Reset()
        {
            // Set up smart defaults based on the selected color enum
            switch (lensColor)
            {
                case LensColor.Cyan:
                    filterColor = new Color(0f, 1f, 1f, 1f);
                    break;
                case LensColor.Yellow:
                    filterColor = new Color(1f, 1f, 0f, 1f);
                    break;
                case LensColor.Magenta:
                    filterColor = new Color(1f, 0f, 1f, 1f);
                    break;
                case LensColor.Blue:
                    filterColor = new Color(0f, 0f, 1f, 1f);
                    break;
                case LensColor.Orange:
                    filterColor = new Color(1f, 0.5f, 0f, 1f);
                    break;
            }
        }
    }
}
