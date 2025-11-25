using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class InfiniteSocketItem : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] XRSocketInteractor socket;              // The socket that holds the carabiner
    [SerializeField] GameObject carabinerPrefab;           // The real grabbable carabiner prefab
    [SerializeField] float respawnDelay = 0.5f;           // Delay before spawning new carabiner (prevents spam)
    
    [Header("Spawn Limit")]
    [SerializeField] int maxCarabiners = 5;                // Maximum carabiners that can spawn per level
    [SerializeField] bool unlimitedSpawns = false;         // Set to true for infinite carabiners
    
    [Header("Return Settings")]
    [SerializeField] float returnRadius = 0.3f;            // Radius around socket where carabiners can be returned
    
    private GameObject currentCarabiner;                   // The carabiner currently in the socket
    private bool isRespawning = false;                     // Prevent multiple respawns at once
    private int carabinersSpawned = 0;                     // Counter for how many carabiners have been spawned
    private SphereCollider returnTrigger;                   // Trigger zone for returning carabiners

    void Awake()
    {
        // Auto-find socket if not assigned
        if (!socket) socket = GetComponent<XRSocketInteractor>();
    }

    void Start()
    {
        // Validate setup before proceeding
        if (socket == null)
        {
            Debug.LogError("XRSocketInteractor not assigned! Cannot spawn carabiner.");
            return;
        }
        
        if (carabinerPrefab == null)
        {
            Debug.LogError("Carabiner prefab not assigned! Cannot spawn carabiner.");
            return;
        }
        
        // Create return trigger zone
        CreateReturnTrigger();
        
        // Spawn initial carabiner in socket
        SpawnCarabinerInSocket();
        
        // Listen for when carabiner is grabbed/taken out
        socket.selectExited.AddListener(OnCarabinerTaken);
    }

    void CreateReturnTrigger()
    {
        // Create a trigger zone around the socket for returning carabiners
        GameObject triggerObj = new GameObject("ReturnTrigger");
        triggerObj.transform.SetParent(socket.transform);
        triggerObj.transform.localPosition = Vector3.zero;
        triggerObj.layer = socket.gameObject.layer;
        
        returnTrigger = triggerObj.AddComponent<SphereCollider>();
        returnTrigger.isTrigger = true;
        returnTrigger.radius = returnRadius;
        
        Rigidbody rb = triggerObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnDisable()
    {
        // Clean up event listener
        if (socket != null)
        {
            socket.selectExited.RemoveListener(OnCarabinerTaken);
        }
    }

    void OnCarabinerTaken(SelectExitEventArgs args)
    {
        // When carabiner is grabbed from socket, spawn a new one after delay (if limit not reached)
        if (!isRespawning)
        {
            StartCoroutine(RespawnCarabinerDelayed());
        }
    }

    IEnumerator RespawnCarabinerDelayed()
    {
        isRespawning = true;
        
        // Wait for the delay
        yield return new WaitForSeconds(respawnDelay);
        
        // Check if we can still spawn more carabiners
        if (CanSpawnMore())
        {
            SpawnCarabinerInSocket();
        }
        else
        {
            // Limit reached - no more carabiners will spawn
            Debug.Log($"Carabiner limit reached! ({carabinersSpawned}/{maxCarabiners} spawned)");
        }
        
        isRespawning = false;
    }

    bool CanSpawnMore()
    {
        // If unlimited spawns is enabled, always return true
        if (unlimitedSpawns) return true;
        
        // Otherwise check if we've reached the limit
        return carabinersSpawned < maxCarabiners;
    }

    void SpawnCarabinerInSocket()
    {
        if (socket == null || carabinerPrefab == null) return;
        
        // Check if socket has an attach transform
        if (socket.attachTransform == null)
        {
            Debug.LogError("Socket attachTransform is null! Cannot spawn carabiner.");
            return;
        }
        
        // Check limit before spawning
        if (!CanSpawnMore()) return;
        
        // Spawn the carabiner at the socket's attach point
        GameObject newCarabiner = Instantiate(carabinerPrefab, socket.attachTransform.position, socket.attachTransform.rotation);
        
        // Get the interactable component
        IXRSelectInteractable interactable = newCarabiner.GetComponent<IXRSelectInteractable>();
        
        if (interactable == null)
        {
            Debug.LogWarning($"Carabiner prefab '{carabinerPrefab.name}' doesn't have an IXRSelectInteractable component! Destroying spawned object.");
            Destroy(newCarabiner);
            return;
        }
        
        // Tag this carabiner so we can identify it
        newCarabiner.tag = "SpawnedCarabiner";
        
        // Add component to track this carabiner for return detection
        ReturnDetector detector = newCarabiner.AddComponent<ReturnDetector>();
        detector.Initialize(this, socket.transform);
        
        // Put it directly into the socket using manual interaction
        socket.StartManualInteraction(interactable);
        currentCarabiner = newCarabiner;
        
        // Increment the spawn counter only on successful spawn
        carabinersSpawned++;
        Debug.Log($"Carabiner spawned! ({carabinersSpawned}/{maxCarabiners})");
    }

    /// <summary>
    /// Reset the spawn counter (call this when starting a new level)
    /// </summary>
    public void ResetSpawnCounter()
    {
        carabinersSpawned = 0;
        Debug.Log("Carabiner spawn counter reset!");
    }

    /// <summary>
    /// Get how many carabiners have been spawned
    /// </summary>
    public int GetCarabinersSpawned()
    {
        return carabinersSpawned;
    }

    /// <summary>
    /// Get how many carabiners are remaining
    /// </summary>
    public int GetCarabinersRemaining()
    {
        if (unlimitedSpawns) return -1; // -1 means unlimited
        return Mathf.Max(0, maxCarabiners - carabinersSpawned);
    }

    /// <summary>
    /// Called by ReturnDetector when a carabiner is dropped near the socket
    /// </summary>
    public void ReturnCarabiner(GameObject carabiner)
    {
        if (carabiner == null) return;
        
        // Decrement counter
        if (carabinersSpawned > 0)
        {
            carabinersSpawned--;
            Debug.Log($"Carabiner returned! Counter decreased. ({carabinersSpawned}/{maxCarabiners})");
        }
        
        // Destroy the carabiner
        Destroy(carabiner);
    }
}

/// <summary>
/// Component attached to spawned carabiners to detect when they should be returned
/// </summary>
public class ReturnDetector : MonoBehaviour
{
    private InfiniteSocketItem parentScript;
    private Transform socketTransform;
    private XRGrabInteractable grabInteractable;
    private bool isHeldByHand = false;

    public void Initialize(InfiniteSocketItem parent, Transform socket)
    {
        parentScript = parent;
        socketTransform = socket;
        
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Check if grabbed by a hand/controller (not the socket)
        isHeldByHand = args.interactorObject is XRDirectInteractor || 
                       args.interactorObject is XRRayInteractor;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Only check for return if it was held by a hand
        if (!isHeldByHand) return;
        
        isHeldByHand = false;
        
        // Check if close enough to socket to return
        if (socketTransform != null && parentScript != null)
        {
            float distance = Vector3.Distance(transform.position, socketTransform.position);
            if (distance <= 0.3f) // Within return radius
            {
                parentScript.ReturnCarabiner(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}