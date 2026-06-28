using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EscapeRoom
{
    public class ExitDoorController : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("The Renderers for the door frames (assign both here).")]
        public Renderer[] frameRenderers;
        
        [Tooltip("The Renderer for the center sphere.")]
        public Renderer sphereRenderer;
        
        [Tooltip("The glowing cyan material for the initial state.")]
        public Material cyanGlowMaterial;

        [Tooltip("The glowing yellow material for when it opens.")]
        public Material yellowGlowMaterial;

        [Header("Animation")]
        [Tooltip("The Animator component on the door.")]
        public Animator doorAnimator;

        [Header("End Game Sequence")]
        [Tooltip("The name of the End Scene to load (e.g., '2_Outro'). Leave empty to quit the game instead.")]
        public string sceneToLoadOnEnd = "";

        [Header("Lock Settings")]
        [Tooltip("If true, interacting with the door will do nothing until UnlockDoor() is called.")]
        public bool isLocked = true;

        private bool hasOpened = false;

        private void Start()
        {
            // Set initial colors to cyan
            if (cyanGlowMaterial != null)
            {
                if (frameRenderers != null)
                {
                    foreach (var frame in frameRenderers)
                    {
                        if (frame != null) frame.material = cyanGlowMaterial;
                    }
                }
                
                if (sphereRenderer != null)
                {
                    sphereRenderer.material = cyanGlowMaterial;
                }
            }
        }

        /// <summary>
        /// Call this from StairManager's OnAllStepsSolved event to allow the door to open.
        /// </summary>
        public void UnlockDoor()
        {
            isLocked = false;
            Debug.Log("[ExitDoorController] Door has been unlocked!");
        }

        /// <summary>
        /// Call this method from your XR Interactable Event (e.g., Select Entered).
        /// </summary>
        public void OpenExitDoor()
        {
            if (isLocked)
            {
                Debug.Log("[ExitDoorController] Door is locked! Complete the stair puzzle first.");
                return;
            }

            if (hasOpened) return; // Prevent opening twice
            hasOpened = true;

            // Change materials to Yellow Glow
            if (yellowGlowMaterial != null)
            {
                if (frameRenderers != null)
                {
                    foreach (var frame in frameRenderers)
                    {
                        if (frame != null) frame.material = yellowGlowMaterial;
                    }
                }
                
                if (sphereRenderer != null)
                {
                    sphereRenderer.material = yellowGlowMaterial;
                }
            }

            // Trigger the animation
            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger("OpenDoor");
            }

            // Start the endgame sequence
            StartCoroutine(EndGameRoutine());
        }

        private System.Collections.IEnumerator EndGameRoutine()
        {
            // Wait 5 seconds after the door opens so the animation finishes
            yield return new WaitForSeconds(5f);

            // Either load the end scene, or exit the game
            if (!string.IsNullOrEmpty(sceneToLoadOnEnd))
            {
                Debug.Log($"Loading End Scene: {sceneToLoadOnEnd}");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnEnd);
            }
            else
            {
                Debug.Log("Exiting Game...");
                
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
    }
}
