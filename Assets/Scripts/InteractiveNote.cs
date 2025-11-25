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
    
    private XRGrabInteractable grabInteractable;
    private bool isBeingHeld = false;
    private bool hasSelectedText = false;                      // Track if text has been selected

    void Start()
    {
        // Get grab interactable component
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        
        // Auto-find player camera if not assigned
        if (playerCamera == null)
        {
            // Look for XR Origin camera
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                Transform mainCamera = xrOrigin.transform.Find("Camera Offset/Main Camera");
                if (mainCamera != null)
                {
                    playerCamera = mainCamera;
                }
            }
            
            // Fallback to main camera
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }
        
        // Setup button click listeners - each button passes itself
        foreach (var button in textButtons)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => OnTextSelected(button));
            }
        }
        
        // Initial state: TextDisplay OFF (no text yet)
        if (textDisplay != null)
        {
            textDisplay.SetActive(false);
        }
        
        // TextSelection OFF initially
        if (textSelection != null)
        {
            textSelection.SetActive(false);
        }
        
        // Clear text initially
        if (noteText != null)
        {
            noteText.text = "";
        }
    }

    void Update()
    {
        // Only check distance when note is being held and text hasn't been selected yet
        if (isBeingHeld && !hasSelectedText && playerCamera != null)
        {
            float distance = Vector3.Distance(transform.position, playerCamera.position);
            bool isCloseToFace = distance < showDistance;
            
            // Toggle TextSelection based on distance
            if (textSelection != null && textSelection.activeSelf != isCloseToFace)
            {
                textSelection.SetActive(isCloseToFace);
            }
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isBeingHeld = true;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isBeingHeld = false;
        
        // Hide TextSelection when released (only if text hasn't been selected)
        if (!hasSelectedText && textSelection != null)
        {
            textSelection.SetActive(false);
        }
    }

    void OnTextSelected(Button clickedButton)
    {
        if (clickedButton == null || noteText == null) return;
        
        // Get the TextMeshProUGUI component from the button's child
        TextMeshProUGUI buttonText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
        
        if (buttonText != null)
        {
            // Copy the button's text to NoteText
            noteText.text = buttonText.text;
            
            // Mark that text has been selected
            hasSelectedText = true;
            
            // Toggle OFF TextSelection
            if (textSelection != null)
            {
                textSelection.SetActive(false);
            }
            
            // Toggle ON TextDisplay
            if (textDisplay != null)
            {
                textDisplay.SetActive(true);
            }
            
            Debug.Log($"Text selected: {buttonText.text}");
        }
        else
        {
            Debug.LogWarning("Button doesn't have a TextMeshProUGUI child!");
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        
        // Clean up button listeners
        foreach (var button in textButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
    
    // Optional: Public method to reset the note (for reuse)
    public void ResetNote()
    {
        hasSelectedText = false;
        
        if (noteText != null)
        {
            noteText.text = "";
        }
        
        if (textDisplay != null)
        {
            textDisplay.SetActive(false);
        }
        
        if (textSelection != null)
        {
            textSelection.SetActive(false);
        }
    }
}