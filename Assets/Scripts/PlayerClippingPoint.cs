using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Add this to the player's clipping point to allow manual rope disconnection
/// </summary>
public class PlayerClippingPoint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RopeManager ropeManager;
    
    [Header("Settings")]
    [SerializeField] private bool allowReconnection = false;  // Can player reconnect the rope?
    
    private XRSimpleInteractable interactable;
    private bool isDisconnected = false;
    
    void Start()
    {
        // Get the interactable component
        interactable = GetComponent<XRSimpleInteractable>();
        
        if (interactable == null)
        {
            Debug.LogError("PlayerClippingPoint: XRSimpleInteractable component not found!");
            return;
        }
        
        // Listen for when player interacts with this point
        interactable.selectEntered.AddListener(OnInteracted);
        
        // Find RopeManager if not assigned
        if (ropeManager == null)
        {
            ropeManager = FindObjectOfType<RopeManager>();
            if (ropeManager == null)
            {
                Debug.LogError("PlayerClippingPoint: RopeManager not found!");
            }
        }
    }
    
    void OnInteracted(SelectEnterEventArgs args)
    {
        if (ropeManager == null) return;
        
        // Toggle connection/disconnection
        if (!isDisconnected)
        {
            // Disconnect the rope from player
            ropeManager.DisconnectRopeFromPlayer();
            isDisconnected = true;
            Debug.Log("Rope disconnected from player!");
            
            // Optional: Play sound or haptic feedback here
            //PlayHapticFeedback(args.interactorObject);
        }
        else if (allowReconnection)
        {
            // Reconnect the rope to player
            ropeManager.ReconnectRopeToPlayer();
            isDisconnected = false;
            Debug.Log("Rope reconnected to player!");
            
            //PlayHapticFeedback(args.interactorObject);
        }
    }
    
    void PlayHapticFeedback(IXRSelectInteractor interactor)
    {
        // Send haptic pulse to controller
        if (interactor is XRBaseInputInteractor controllerInteractor)
        {
            controllerInteractor.SendHapticImpulse(0.5f, 0.2f);
        }
    }
    
    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnInteracted);
        }
    }
}