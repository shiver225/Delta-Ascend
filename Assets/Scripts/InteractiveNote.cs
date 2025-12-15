using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractiveNote : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private GameObject noteTextCanvas;        // Parent canvas (NoteTextCanvas)
    [SerializeField] private GameObject textSelection;         // TextSelection GameObject with buttons
    [SerializeField] private GameObject textDisplay;           // TextDisplay GameObject

    [Header("Text Reference")]
    [SerializeField] private TextMeshProUGUI noteText;         // NoteText (child of TextDisplay)

    [Header("Buttons")]
    [SerializeField] private Button[] textButtons;             // All text option buttons

    [Header("Settings")]
    [SerializeField] private float showDistance = 0.4f;        // Distance to face to show options
    [SerializeField] private Transform playerCamera;           // VR camera reference
    [SerializeField] private Transform distanceTarget;         // Where to measure distance from (defaults to canvas/display)

    private XRGrabInteractable grabInteractable;
    private int grabCount = 0;            // number of hands currently grabbing
    private bool hasSelectedText = false;
    private float hideDistance;           // showDistance + small hysteresis

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        // Auto-find player camera if not assigned
        if (playerCamera == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                Transform mainCamera = xrOrigin.transform.Find("Camera Offset/Main Camera");
                if (mainCamera != null)
                    playerCamera = mainCamera;
            }
            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main.transform;
        }

        // Default distance target to the front canvas/display if not set
        if (distanceTarget == null)
        {
            if (noteTextCanvas != null) distanceTarget = noteTextCanvas.transform;
            else if (textDisplay != null) distanceTarget = textDisplay.transform;
            else distanceTarget = transform;
        }

        hideDistance = showDistance + 0.05f; // small hysteresis to avoid flicker

        // Setup buttons
        foreach (var button in textButtons)
        {
            if (button != null)
                button.onClick.AddListener(() => OnTextSelected(button));
        }

        // Initial state
        if (textDisplay != null) textDisplay.SetActive(false);
        if (textSelection != null) textSelection.SetActive(false);
        if (noteText != null) noteText.text = "";
    }

    void Update()
    {
        // Only check distance when held by at least one hand and no text chosen yet
        if (grabCount > 0 && !hasSelectedText && playerCamera != null)
        {
            float distance = Vector3.Distance(distanceTarget.position, playerCamera.position);
            bool shouldShow = distance < showDistance;
            bool shouldHide = distance > hideDistance;

            if (textSelection != null)
            {
                if (shouldShow && !textSelection.activeSelf)
                    textSelection.SetActive(true);
                else if (shouldHide && textSelection.activeSelf)
                    textSelection.SetActive(false);
            }
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        grabCount = Mathf.Max(0, grabCount + 1);

        // Force refresh when (first) grabbed so it shows immediately if close
        if (!hasSelectedText && textSelection != null && playerCamera != null)
        {
            float distance = Vector3.Distance(distanceTarget.position, playerCamera.position);
            textSelection.SetActive(distance < showDistance);
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        grabCount = Mathf.Max(0, grabCount - 1);

        // Hide picker only when no hands remain (unless text chosen)
        if (grabCount == 0 && !hasSelectedText && textSelection != null)
            textSelection.SetActive(false);
    }

    void OnTextSelected(Button clickedButton)
    {
        if (clickedButton == null || noteText == null) return;

        TextMeshProUGUI buttonText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            noteText.text = buttonText.text;
            hasSelectedText = true;

            if (textSelection != null) textSelection.SetActive(false);
            if (textDisplay != null) textDisplay.SetActive(true);

            Debug.Log($"Text selected: {buttonText.text}");
        }
        else
        {
            Debug.LogWarning("Button doesn't have a TextMeshProUGUI child!");
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        foreach (var button in textButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }

    // Optional: reset for reuse
    public void ResetNote()
    {
        hasSelectedText = false;
        grabCount = 0;

        if (noteText != null) noteText.text = "";
        if (textDisplay != null) textDisplay.SetActive(false);
        if (textSelection != null) textSelection.SetActive(false);
    }
}