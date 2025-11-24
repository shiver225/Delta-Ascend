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
    
    private GameObject currentCarabiner;                   // The carabiner currently in the socket
    private bool isRespawning = false;                     // Prevent multiple respawns at once
    private int carabinersSpawned = 0;                     // Counter for how many carabiners have been spawned
    private Coroutine respawnCoroutine;                    // Reference to respawn coroutine so we can cancel it
    private bool isSpawning = false;                       // Flag to ignore selectEntered during scripted spawns
    private GameObject lastSpawnedCarabiner;                // Track the last carabiner we spawned
    [SerializeField] float returnDistance = 0.2f;          // How close carabiner needs to be to socket to "return" it

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
        
        // Spawn initial carabiner in socket
        SpawnCarabinerInSocket();
        
        // Listen for when carabiner is grabbed/taken out
        socket.selectExited.AddListener(OnCarabinerTaken);
        
        // Listen for when carabiner is placed back in socket
        socket.selectEntered.AddListener(OnCarabinerReturned);
        
    }

    void OnDisable()
    {
        // Clean up event listeners
        if (socket != null)
        {
            socket.selectExited.RemoveListener(OnCarabinerTaken);
            socket.selectEntered.RemoveListener(OnCarabinerReturned);
        }
        
        // Cancel any pending respawn
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
            isRespawning = false;
        }
    }

    void OnCarabinerTaken(SelectExitEventArgs args)
    {
        // Ignore if we're spawning (this is just the old carabiner being replaced)
        if (isSpawning) return;
        
        // When carabiner is grabbed from socket, spawn a new one after delay (if limit not reached)
        if (!isRespawning)
        {
            respawnCoroutine = StartCoroutine(RespawnCarabinerDelayed());
        }
    }

    void OnCarabinerReturned(SelectEnterEventArgs args)
    {
        // Ignore if we're in the middle of spawning (this is our script placing it, not the player)
        if (isSpawning) return;
        
        // Check if the carabiner being placed in socket is one we spawned
        GameObject returnedCarabiner = (args.interactableObject as MonoBehaviour)?.gameObject;
        
        if (returnedCarabiner != null && returnedCarabiner.GetComponent<SpawnedCarabinerMarker>() != null)
        {
            HandleCarabinerReturned(returnedCarabiner);
        }
    }

    public void HandleCarabinerReturned(GameObject returnedCarabiner)
    {
        if (returnedCarabiner == null) return;
        
        // Cancel any pending respawn since we're putting one back
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
            isRespawning = false;
        }
        
        // Remove it from socket if it's in there
        if (socket != null && socket.hasSelection)
        {
            socket.EndManualInteraction();
        }
        
        // Destroy the carabiner (it disappears)
        Destroy(returnedCarabiner);
        
        // Decrement the counter
        if (carabinersSpawned > 0)
        {
            carabinersSpawned--;
            Debug.Log($"Carabiner returned and destroyed! Counter decreased. ({carabinersSpawned}/{maxCarabiners} spawned)");
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
        respawnCoroutine = null;
    }

    IEnumerator ResetSpawningFlag()
    {
        // Wait one frame to ensure the selectEntered event has been processed
        yield return null;
        isSpawning = false;
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
        
        // Mark this carabiner as one we spawned (so we can identify it when returned)
        if (newCarabiner.GetComponent<SpawnedCarabinerMarker>() == null)
        {
            newCarabiner.AddComponent<SpawnedCarabinerMarker>();
        }
        
        // Add return detector component
        CarabinerReturnDetector detector = newCarabiner.GetComponent<CarabinerReturnDetector>();
        if (detector == null)
        {
            detector = newCarabiner.AddComponent<CarabinerReturnDetector>();
        }
        detector.Initialize(socket, this, returnDistance);
        
        // Get the interactable component
        IXRSelectInteractable interactable = newCarabiner.GetComponent<IXRSelectInteractable>();
        
        if (interactable == null)
        {
            Debug.LogWarning($"Carabiner prefab '{carabinerPrefab.name}' doesn't have an IXRSelectInteractable component! Destroying spawned object.");
            Destroy(newCarabiner);
            return;
        }
        
        // Set flag to ignore selectEntered event during spawn
        isSpawning = true;
        lastSpawnedCarabiner = newCarabiner;
        
        // Put it directly into the socket using manual interaction
        socket.StartManualInteraction(interactable);
        currentCarabiner = newCarabiner;
        
        // Increment the spawn counter only on successful spawn
        carabinersSpawned++;
        Debug.Log($"Carabiner spawned! ({carabinersSpawned}/{maxCarabiners} spawned)");
        
        // Reset the flag after a frame (to allow the selectEntered event to be ignored)
        StartCoroutine(ResetSpawningFlag());
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
}