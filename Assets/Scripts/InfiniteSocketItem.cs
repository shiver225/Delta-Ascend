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
    
    private GameObject currentCarabiner;                   // The carabiner currently in the socket
    private bool isRespawning = false;                     // Prevent multiple respawns at once

    void Awake()
    {
        // Auto-find socket if not assigned
        if (!socket) socket = GetComponent<XRSocketInteractor>();
    }

    void Start()
    {
        // Spawn initial carabiner in socket
        SpawnCarabinerInSocket();
        
        // Listen for when carabiner is grabbed/taken out
        if (socket != null)
        {
            socket.selectExited.AddListener(OnCarabinerTaken);
        }
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
        // When carabiner is grabbed from socket, spawn a new one after delay
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
        
        // Spawn new carabiner in socket
        SpawnCarabinerInSocket();
        
        isRespawning = false;
    }

    void SpawnCarabinerInSocket()
    {
        if (socket == null || carabinerPrefab == null) return;
        
        // Spawn the carabiner at the socket's attach point
        GameObject newCarabiner = Instantiate(carabinerPrefab, socket.attachTransform.position, socket.attachTransform.rotation);
        
        // Get the interactable component
        IXRSelectInteractable interactable = newCarabiner.GetComponent<IXRSelectInteractable>();
        
        if (interactable != null && socket != null)
        {
            // Put it directly into the socket using manual interaction
            socket.StartManualInteraction(interactable);
            currentCarabiner = newCarabiner;
        }
    }
}