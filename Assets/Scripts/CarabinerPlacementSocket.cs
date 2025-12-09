using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Add this to the CarabinWhole prefab to detect when a Carabin is placed into its socket
/// </summary>
public class CarabinerPlacementSocket : MonoBehaviour
{
    private RopeManager ropeManager;
    private XRSocketInteractor socket;
    private GameObject placedCarabin = null;
    
    void Start()
    {
        // Find the RopeManager in the scene
        ropeManager = FindObjectOfType<RopeManager>();
        if (ropeManager == null)
        {
            Debug.LogWarning("CarabinerPlacementSocket: RopeManager not found in scene!");
        }
        
        // Get the socket component on this CarabinWhole
        socket = GetComponent<XRSocketInteractor>();
        if (socket == null)
        {
            Debug.LogError("CarabinerPlacementSocket: XRSocketInteractor not found on CarabinWhole!");
            return;
        }
        
        // Listen for when a Carabin is placed in this socket
        socket.selectEntered.AddListener(OnCarabinPlaced);
        socket.selectExited.AddListener(OnCarabinRemoved);
    }
    
    void OnCarabinPlaced(SelectEnterEventArgs args)
    {
        if (ropeManager == null) return;
        
        // Get the Carabin GameObject that was placed into this CarabinWhole
        GameObject carabin = args.interactableObject.transform.gameObject;
        
        placedCarabin = carabin;
        
        // Notify rope manager that a carabin was placed into THIS CarabinWhole
        // Pass the CarabinWhole's transform (this gameObject) not the Carabin
        ropeManager.OnCarabinerPlaced(carabin.transform);
        
        Debug.Log($"Carabin placed into CarabinWhole: {gameObject.name}");
    }
    
    void OnCarabinRemoved(SelectExitEventArgs args)
    {
        if (ropeManager == null) return;
        
        GameObject carabin = args.interactableObject.transform.gameObject;
        
        if (carabin == placedCarabin)
        {
            // Notify rope manager that carabin was removed from THIS CarabinWhole
            ropeManager.OnCarabinerRemoved(carabin.transform);
            
            placedCarabin = null;
            
            Debug.Log($"Carabin removed from CarabinWhole: {gameObject.name}");
        }
    }
    
    void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnCarabinPlaced);
            socket.selectExited.RemoveListener(OnCarabinRemoved);
        }
    }
}