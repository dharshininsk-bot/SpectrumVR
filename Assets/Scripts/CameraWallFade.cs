using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SpectrumVR
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CameraWallFade : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("The UI Image used to fade the screen. It should cover the whole screen and start transparent.")]
        public Image fadeImage;
        
        [Tooltip("Duration of the fade-in and fade-out effects.")]
        public float fadeDuration = 0.2f;
        
        [Tooltip("Color of the screen fade when inside a wall.")]
        public Color fadeColor = Color.black;
        
        [Header("Collision Settings")]
        [Tooltip("Layer mask containing the walls/obstacles that trigger the fade.")]
        public LayerMask obstacleLayers;

        private int m_TriggerCount = 0;
        private Coroutine m_FadeCoroutine;

        private void Start()
        {
            // Setup Collider dynamically
            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.15f; // Roughly human head size

            // Setup Rigidbody (required for trigger events to work)
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Ensure we have a fade image
            if (fadeImage == null)
            {
                Debug.LogWarning("CameraWallFade: No UI Image assigned for fading! Please assign a fullscreen Image.");
            }
            else
            {
                // Set initial color to transparent
                Color colImage = fadeColor;
                colImage.a = 0f;
                fadeImage.color = colImage;
                fadeImage.enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the collided object is in the obstacle layers
            if (((1 << other.gameObject.layer) & obstacleLayers) != 0)
            {
                m_TriggerCount++;
                if (m_TriggerCount == 1)
                {
                    StartFade(1.0f);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if the collided object is in the obstacle layers
            if (((1 << other.gameObject.layer) & obstacleLayers) != 0)
            {
                m_TriggerCount = Mathf.Max(0, m_TriggerCount - 1);
                if (m_TriggerCount == 0)
                {
                    StartFade(0.0f);
                }
            }
        }

        private void StartFade(float targetAlpha)
        {
            if (m_FadeCoroutine != null)
            {
                StopCoroutine(m_FadeCoroutine);
            }
            m_FadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            if (fadeImage == null) yield break;

            if (targetAlpha > 0f)
            {
                fadeImage.enabled = true;
            }

            Color startColor = fadeImage.color;
            Color endColor = fadeColor;
            endColor.a = targetAlpha;

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeImage.color = Color.Lerp(startColor, endColor, elapsedTime / fadeDuration);
                yield return null;
            }

            fadeImage.color = endColor;

            if (targetAlpha == 0f)
            {
                fadeImage.enabled = false;
            }
        }
    }
}
