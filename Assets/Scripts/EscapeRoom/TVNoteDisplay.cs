using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EscapeRoom
{
    /// <summary>
    /// Manages the display of visual notes or slides on a TV UI screen.
    /// Exposes methods to cycle forward and backward through notes, suitable for calling from VR Canvas buttons or 3D physical triggers.
    /// </summary>
    public class TVNoteDisplay : MonoBehaviour
    {
        [Header("UI Components")]
        [Tooltip("The UI Image component on the TV displaying the note.")]
        [SerializeField] private Image tvImageTarget;

        [Header("Slideshow Content")]
        [Tooltip("Array of sprite images representing the notes in sequence.")]
        [SerializeField] private Sprite[] noteSprites;

        [Header("Slideshow Settings")]
        [Tooltip("If true, pressing Next on the last slide wraps around to the first slide, and vice-versa.")]
        [SerializeField] private bool loopSlideshow = false;

        [Tooltip("If true, the display will automatically update to the first slide on start.")]
        [SerializeField] private bool showFirstOnStart = true;

        [Header("Events")]
        [Tooltip("Triggered whenever the active note index changes.")]
        public UnityEvent<int> OnNoteIndexChanged;

        [Tooltip("Triggered when the player reaches the final note/slide.")]
        public UnityEvent OnReachedEnd;

        private int currentIndex = 0;

        /// <summary>
        /// Gets the current active note index.
        /// </summary>
        public int CurrentIndex => currentIndex;

        /// <summary>
        /// Gets the total number of notes configured.
        /// </summary>
        public int TotalNotes => noteSprites != null ? noteSprites.Length : 0;

        private void Start()
        {
            if (tvImageTarget == null)
            {
                tvImageTarget = GetComponent<Image>();
            }

            if (showFirstOnStart)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Shows the next note in the sequence.
        /// </summary>
        public void ShowNextNote()
        {
            if (noteSprites == null || noteSprites.Length == 0)
            {
                Debug.LogWarning("TVNoteDisplay: No note sprites assigned.", this);
                return;
            }

            int nextIndex = currentIndex + 1;

            if (nextIndex >= noteSprites.Length)
            {
                if (loopSlideshow)
                {
                    currentIndex = 0;
                }
                else
                {
                    OnReachedEnd?.Invoke();
                    return; // Stop at the end
                }
            }
            else
            {
                currentIndex = nextIndex;
            }

            UpdateDisplay();
        }

        /// <summary>
        /// Shows the previous note in the sequence.
        /// </summary>
        public void ShowPreviousNote()
        {
            if (noteSprites == null || noteSprites.Length == 0)
            {
                Debug.LogWarning("TVNoteDisplay: No note sprites assigned.", this);
                return;
            }

            int prevIndex = currentIndex - 1;

            if (prevIndex < 0)
            {
                if (loopSlideshow)
                {
                    currentIndex = noteSprites.Length - 1;
                }
                else
                {
                    return; // Stop at the beginning
                }
            }
            else
            {
                currentIndex = prevIndex;
            }

            UpdateDisplay();
        }

        /// <summary>
        /// Displays the note at a specific index.
        /// </summary>
        /// <param name="index">Index of the note sprite to show.</param>
        public void ShowNoteAtIndex(int index)
        {
            if (noteSprites == null || noteSprites.Length == 0)
            {
                Debug.LogWarning("TVNoteDisplay: No note sprites assigned.", this);
                return;
            }

            if (index < 0 || index >= noteSprites.Length)
            {
                Debug.LogWarning($"TVNoteDisplay: Index {index} is out of bounds (0 to {noteSprites.Length - 1}).", this);
                return;
            }

            currentIndex = index;
            UpdateDisplay();
        }

        /// <summary>
        /// Updates the image target and fires events.
        /// </summary>
        private void UpdateDisplay()
        {
            if (tvImageTarget == null)
            {
                Debug.LogError("TVNoteDisplay: Image target is not assigned.", this);
                return;
            }

            if (noteSprites == null || noteSprites.Length == 0)
            {
                tvImageTarget.enabled = false;
                return;
            }

            tvImageTarget.enabled = true;
            tvImageTarget.sprite = noteSprites[currentIndex];

            OnNoteIndexChanged?.Invoke(currentIndex);

            if (currentIndex == noteSprites.Length - 1 && !loopSlideshow)
            {
                OnReachedEnd?.Invoke();
            }
        }
    }
}
