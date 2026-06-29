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
        [Tooltip("The UI GameObject containing the 'LEVEL CLEARED' message.")]
        public GameObject levelClearedUI;

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
            
            // Ensure the UI is hidden at the start
            if (levelClearedUI != null)
            {
                levelClearedUI.SetActive(false);
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
            // Wait 5 seconds after the door opens
            yield return new WaitForSeconds(8f);

            // Show the Level Cleared UI
            if (levelClearedUI != null)
            {
                levelClearedUI.SetActive(true);
            }

            // Wait a little bit so the player can actually read the message before it suddenly closes
            yield return new WaitForSeconds(4f);

            // Exit the game
            Debug.Log("Exiting Game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
