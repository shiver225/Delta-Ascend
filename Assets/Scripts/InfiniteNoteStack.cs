using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Infinite storage socket for notes. Stores multiple notes and lets you retrieve them one at a time.
/// </summary>
public class InfiniteNoteStack : MonoBehaviour
{
    [Header("Socket Setup")]
    [SerializeField] private XRSocketInteractor socket;           // The socket component
    [SerializeField] private GameObject notePrefab;               // Reference note prefab for spawning display copies
    [SerializeField] private float depositDistance = 0.3f;        // How close to socket to deposit a note
    
    [Header("Visual Feedback")]
    [SerializeField] private TextMeshPro counterText;             // Text showing note count (optional)
    
    private List<GameObject> storedNotes = new List<GameObject>(); // Stack of stored notes
    private GameObject currentDisplayNote;                         // The note currently visible in socket
    private bool isHandlingNote = false;                           // Flag to prevent multiple operations at once
    private SphereCollider depositTrigger;                         // Trigger for detecting notes to deposit

    void Awake()
    {
        if (!socket) socket = GetComponent<XRSocketInteractor>();
    }

    void Start()
    {
        if (socket == null)
        {
            Debug.LogError("XRSocketInteractor not assigned on InfiniteNoteStack!");
            return;
        }
        
        // Create deposit trigger zone
        CreateDepositTrigger();
        
        // Listen for when player grabs from socket
        socket.selectExited.AddListener(OnNoteGrabbed);
        
        // Start with empty socket
        UpdateDisplay();
        UpdateCounter();
    }

    void CreateDepositTrigger()
    {
        // Create a trigger zone around the socket for depositing notes
        GameObject triggerObj = new GameObject("DepositTrigger");
        triggerObj.transform.SetParent(socket.transform);
        triggerObj.transform.localPosition = Vector3.zero;
        triggerObj.layer = gameObject.layer;
        
        depositTrigger = triggerObj.AddComponent<SphereCollider>();
        depositTrigger.isTrigger = true;
        depositTrigger.radius = depositDistance;
        
        Rigidbody rb = triggerObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Add detection component
        NoteDepositZone depositZone = triggerObj.AddComponent<NoteDepositZone>();
        depositZone.Initialize(this);
    }

    void OnDisable()
    {
        if (socket != null)
        {
            socket.selectExited.RemoveListener(OnNoteGrabbed);
        }
    }

    void UpdateDisplay()
    {
        // If we have stored notes and no display note, create one
        if (storedNotes.Count > 0 && currentDisplayNote == null)
        {
            SpawnDisplayNote();
        }
        // If we have no stored notes and there's a display note, remove it
        else if (storedNotes.Count == 0 && currentDisplayNote != null)
        {
            Destroy(currentDisplayNote);
            currentDisplayNote = null;
        }
    }

    void SpawnDisplayNote()
    {
        if (socket == null || notePrefab == null) return;
        if (socket.attachTransform == null)
        {
            Debug.LogError("Socket attachTransform is null!");
            return;
        }
        
        // Create a visual display note
        currentDisplayNote = Instantiate(notePrefab, socket.attachTransform.position, socket.attachTransform.rotation);
        
        // Disable the InteractiveNote script on the display note so it doesn't trigger text selection
        InteractiveNote noteScript = currentDisplayNote.GetComponent<InteractiveNote>();
        if (noteScript != null)
        {
            noteScript.enabled = false;
        }
        
        // Put it in the socket
        IXRSelectInteractable interactable = currentDisplayNote.GetComponent<IXRSelectInteractable>();
        if (interactable != null)
        {
            socket.StartManualInteraction(interactable);
        }
        
        Debug.Log($"Display note spawned (representing {storedNotes.Count} stored notes)");
    }

    void OnNoteGrabbed(SelectExitEventArgs args)
    {
        if (isHandlingNote) return;
        
        // Check if the grabbed object is our display note
        if (currentDisplayNote != null && args.interactableObject.transform.gameObject == currentDisplayNote)
        {
            // If we have stored notes, give the player one
            if (storedNotes.Count > 0)
            {
                isHandlingNote = true;
                
                // Destroy the display note
                Destroy(currentDisplayNote);
                currentDisplayNote = null;
                
                // Get the last stored note (LIFO - Last In, First Out)
                GameObject retrievedNote = storedNotes[storedNotes.Count - 1];
                storedNotes.RemoveAt(storedNotes.Count - 1);
                
                // Reactivate the stored note and position it
                retrievedNote.SetActive(true);
                retrievedNote.transform.position = socket.attachTransform.position;
                retrievedNote.transform.rotation = socket.attachTransform.rotation;
                
                // Re-enable the InteractiveNote script
                InteractiveNote noteScript = retrievedNote.GetComponent<InteractiveNote>();
                if (noteScript != null)
                {
                    noteScript.enabled = true;
                }
                
                // Give it to the player's hand
                IXRSelectInteractable noteInteractable = retrievedNote.GetComponent<IXRSelectInteractable>();
                if (noteInteractable != null && args.interactorObject is XRBaseInteractor handInteractor)
                {
                    handInteractor.StartManualInteraction(noteInteractable);
                }
                
                Debug.Log($"Note retrieved from stack! ({storedNotes.Count} remaining)");
                
                // Update display for remaining notes
                UpdateDisplay();
                UpdateCounter();
                
                isHandlingNote = false;
            }
        }
    }

    /// <summary>
    /// Called by NoteDepositZone when a note enters the deposit area
    /// </summary>
    public void TryDepositNote(GameObject note)
    {
        if (note == null || isHandlingNote) return;
        
        // Don't deposit the display note
        if (note == currentDisplayNote)
        {
            return;
        }
        
        // Check if this is actually a note
        InteractiveNote noteScript = note.GetComponent<InteractiveNote>();
        if (noteScript == null)
        {
            return;
        }
        
        // Check if the note is currently being held
        XRGrabInteractable grabInteractable = note.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            // Note is being held, don't deposit yet
            return;
        }
        
        // Check if it's already in our storage
        if (storedNotes.Contains(note))
        {
            return;
        }
        
        // Deposit the note!
        isHandlingNote = true;
        
        // Disable the InteractiveNote script while stored
        noteScript.enabled = false;
        
        // Add to storage and hide
        storedNotes.Add(note);
        note.SetActive(false);
        
        Debug.Log($"Note deposited! ({storedNotes.Count} in storage)");
        
        // Update display
        UpdateDisplay();
        UpdateCounter();
        
        isHandlingNote = false;
    }

    void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = storedNotes.Count > 0 ? $"Notes: {storedNotes.Count}" : "";
        }
    }

    /// <summary>
    /// Reset all stored notes (e.g., when starting new level)
    /// </summary>
    public void ClearStorage()
    {
        foreach (GameObject note in storedNotes)
        {
            if (note != null) Destroy(note);
        }
        storedNotes.Clear();
        
        if (currentDisplayNote != null)
        {
            Destroy(currentDisplayNote);
            currentDisplayNote = null;
        }
        
        UpdateCounter();
        Debug.Log("Note storage cleared!");
    }

    /// <summary>
    /// Get how many notes are currently stored
    /// </summary>
    public int GetStoredNoteCount()
    {
        return storedNotes.Count;
    }
}

/// <summary>
/// Trigger zone component that detects notes entering the deposit area
/// </summary>
public class NoteDepositZone : MonoBehaviour
{
    private InfiniteNoteStack parentStack;
    private HashSet<GameObject> notesInZone = new HashSet<GameObject>();

    public void Initialize(InfiniteNoteStack stack)
    {
        parentStack = stack;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if this is a note
        InteractiveNote noteScript = other.GetComponent<InteractiveNote>();
        if (noteScript != null)
        {
            notesInZone.Add(other.gameObject);
            Debug.Log($"Note entered deposit zone: {other.gameObject.name}");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Continuously try to deposit notes that are in the zone and not being held
        if (notesInZone.Contains(other.gameObject))
        {
            parentStack.TryDepositNote(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (notesInZone.Contains(other.gameObject))
        {
            notesInZone.Remove(other.gameObject);
            Debug.Log($"Note left deposit zone: {other.gameObject.name}");
        }
    }
}