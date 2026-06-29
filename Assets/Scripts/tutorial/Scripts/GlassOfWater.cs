using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeRoom.Tutorial
{
    /// <summary>
    /// Attach this to a "Glass of Water" GameObject in the scene.
    /// When the player clicks it, their ColorWand picks up "water" —
    /// a special state that lets them click a mix slot to clean it.
    ///
    /// Setup: Add XRSimpleInteractable + a Collider + this script to the glass object.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(Collider))]
    public class GlassOfWater : MonoBehaviour
    {
        [Header("Optional Visual Feedback")]
        [Tooltip("If assigned, this object will briefly animate/scale when clicked to give feedback.")]
        public bool playClickFeedback = true;

        private XRSimpleInteractable _interactable;
        private Vector3 _originalScale;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_interactable != null)
                _interactable.selectEntered.AddListener(OnGlassClicked);
        }

        private void OnDisable()
        {
            if (_interactable != null)
                _interactable.selectEntered.RemoveListener(OnGlassClicked);
        }

        private void OnGlassClicked(SelectEnterEventArgs args)
        {
            ColorWand wand = ColorWand.FromInteractor(args.interactorObject as XRBaseInteractor);
            if (wand == null)
            {
                Debug.LogWarning("[GlassOfWater] Clicked but no ColorWand found on the interactor.", this);
                return;
            }

            wand.PickUpWater();
            Debug.Log($"[GlassOfWater] {wand.gameObject.name} now holds water — click a mix slot to clean it.");

            if (playClickFeedback)
            {
                StopAllCoroutines();
                StartCoroutine(ClickFeedbackRoutine());
            }
        }

        private System.Collections.IEnumerator ClickFeedbackRoutine()
        {
            // Quick squish-and-restore animation for tactile feedback
            float elapsed = 0f;
            float duration = 0.18f;
            Vector3 squishScale = _originalScale * 0.85f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Squish in, then pop back
                transform.localScale = Vector3.Lerp(_originalScale, squishScale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            transform.localScale = _originalScale;
        }
    }
}
