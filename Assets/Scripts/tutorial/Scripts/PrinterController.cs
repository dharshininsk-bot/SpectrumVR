using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Controls a printer object in VR/3D.
/// On interaction, plays two animations in sequence with a delay in between:
/// 1. Paper insert animation
/// 2. Printed output animation
/// </summary>
public class PrinterController : MonoBehaviour
{
    [Header("Animator Settings")]
    [Tooltip("Animator for the paper insertion animation. Falls back to the printer's Animator if left blank.")]
    [SerializeField] private Animator paperAnimator;

    [Tooltip("Animator for the print output animation. Falls back to the printer's Animator if left blank.")]
    [SerializeField] private Animator printAnimator;

    [Tooltip("If checked, the script will set Animator Triggers. If unchecked, it will play the state names directly.")]
    [SerializeField] private bool useTriggers = false;

    [Tooltip("The state or trigger name for inserting the paper.")]
    [SerializeField] private string paperInsertAnimation = "InsertPaper";

    [Tooltip("The state or trigger name for getting the print.")]
    [SerializeField] private string printOutputAnimation = "GetPrint";

    [Tooltip("The state name of the Idle state to return to after the sequence finishes (optional).")]
    [SerializeField] private string idleStateName = "Idle";

    [Tooltip("The delay in seconds between the two animations.")]
    [SerializeField] private float delayBetweenAnimations = 5f;

    [Header("Audio Settings (Optional)")]
    [Tooltip("Audio source to play sounds. If left blank, will try to find one on this GameObject.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sound clip to play when inserting paper.")]
    [SerializeField] private AudioClip paperInsertSound;

    [Tooltip("Sound clip to play when printing.")]
    [SerializeField] private AudioClip printOutputSound;

    private XRBaseInteractable interactable;
    private bool isPrinting = false;

    private void Awake()
    {
        // Try to get components if not assigned in inspector
        Animator defaultAnimator = GetComponent<Animator>();
        if (defaultAnimator == null)
        {
            defaultAnimator = GetComponentInChildren<Animator>();
        }

        if (paperAnimator == null)
        {
            paperAnimator = defaultAnimator;
        }

        if (printAnimator == null)
        {
            printAnimator = defaultAnimator;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        // Dynamically subscribe to XR Interaction Toolkit events if XR interactable is present
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.activated.AddListener(OnActivated);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.activated.RemoveListener(OnActivated);
        }
    }

    /// <summary>
    /// Event handler for XR selectEntered (e.g. clicking/grabbing the simple interactable).
    /// </summary>
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[PrinterController] CLICK DETECTED: XR Select Entered on '{gameObject.name}' by '{args.interactorObject?.transform.name}'", this);
        PlayPrintSequence();
    }

    /// <summary>
    /// Event handler for XR activated (e.g. trigger clicked while pointing).
    /// </summary>
    private void OnActivated(ActivateEventArgs args)
    {
        Debug.Log($"[PrinterController] CLICK DETECTED: XR Activated on '{gameObject.name}' by '{args.interactorObject?.transform.name}'", this);
        PlayPrintSequence();
    }

    /// <summary>
    /// Fallback for standard mouse clicking in the Unity Editor or non-VR gameplay.
    /// Requires a collider on the GameObject.
    /// </summary>
    private void OnMouseDown()
    {
        Debug.Log($"[PrinterController] CLICK DETECTED: Mouse Click (OnMouseDown) on '{gameObject.name}'", this);
        PlayPrintSequence();
    }

    /// <summary>
    /// Public method to trigger the print sequence.
    /// Can be called manually from other scripts, UI events, or UnityEvents in the Inspector.
    /// </summary>
    public void PlayPrintSequence()
    {
        Debug.Log($"[PrinterController] PlayPrintSequence requested. current isPrinting = {isPrinting}", this);
        if (isPrinting)
        {
            Debug.Log("[PrinterController] Print sequence is already in progress. Click ignored.", this);
            return;
        }

        StartCoroutine(PrintSequenceCoroutine());
    }

    /// <summary>
    /// Coroutine handling the two animations and delay.
    /// </summary>
    private IEnumerator PrintSequenceCoroutine()
    {
        isPrinting = true;
        Debug.Log("[PrinterController] Starting print sequence...", this);

        // 1. Play Insert Paper Animation
        PlayAnimation(paperAnimator, paperInsertAnimation);
        PlaySound(paperInsertSound);

        // 2. Wait for the specified delay gap
        Debug.Log($"[PrinterController] Waiting for {delayBetweenAnimations} seconds...", this);
        yield return new WaitForSeconds(delayBetweenAnimations);

        // 3. Play Get Print Animation
        PlayAnimation(printAnimator, printOutputAnimation);
        PlaySound(printOutputSound);

        // 4. Optionally return animators to Idle after the print duration or sequence completes
        if (!string.IsNullOrEmpty(idleStateName))
        {
            // Give the second animation a brief moment to play before resetting, 
            // or let it transition naturally in the animator.
            yield return new WaitForSeconds(1.5f); 
            PlayAnimation(paperAnimator, idleStateName);
            PlayAnimation(printAnimator, idleStateName);
        }

        isPrinting = false;
        Debug.Log("[PrinterController] Print sequence completed.", this);
    }

    /// <summary>
    /// Helper to play animator state/trigger.
    /// </summary>
    private void PlayAnimation(Animator targetAnimator, string animationName)
    {
        if (targetAnimator == null)
        {
            Debug.LogError($"[PrinterController] ON {gameObject.name}: Animator component is missing/unassigned!", this);
            return;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"[PrinterController] ON {gameObject.name}: Animation name is empty, skipping animation playback.", this);
            return;
        }

        if (useTriggers)
        {
            targetAnimator.SetTrigger(animationName);
            Debug.Log($"[PrinterController] Set Animator Trigger: '{animationName}' on '{targetAnimator.gameObject.name}'", this);
        }
        else
        {
            // Verify if the state exists on the Base Layer to help debug typos/casing issues
            if (!targetAnimator.HasState(0, Animator.StringToHash(animationName)))
            {
                Debug.LogWarning($"[PrinterController] WARNING: Animator on '{targetAnimator.gameObject.name}' does not contain state '{animationName}' on the Base Layer. Please check your spelling/casing (e.g. 'paperIn' vs 'InsertPaper', 'idle' vs 'Idle').", targetAnimator);
            }
            targetAnimator.Play(animationName);
            Debug.Log($"[PrinterController] Played Animator State: '{animationName}' on '{targetAnimator.gameObject.name}'", this);
        }
    }

    /// <summary>
    /// Helper to play audio clip.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
