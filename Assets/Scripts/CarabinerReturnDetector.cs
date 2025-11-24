using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Component added to spawned carabiners to detect when they're returned to the socket
/// </summary>
public class CarabinerReturnDetector : MonoBehaviour
{
    private XRSocketInteractor targetSocket;
    private float returnDistance = 0.2f;
    private InfiniteSocketItem parentScript;
    private bool wasSelected = false;
    private XRGrabInteractable grabInteractable;

    public void Initialize(XRSocketInteractor socket, InfiniteSocketItem script, float distance)
    {
        targetSocket = socket;
        parentScript = script;
        returnDistance = distance;
        
        // Get the grab interactable component
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelected);
            grabInteractable.selectExited.AddListener(OnDeselected);
        }
    }

    void OnSelected(SelectEnterEventArgs args)
    {
        // Track that this carabiner is being held (by hand or socket)
        wasSelected = true;
    }

    void OnDeselected(SelectExitEventArgs args)
    {
        if (!wasSelected) return;
        
        // Only destroy if it's being released from a HAND/CONTROLLER (not from socket)
        // Check if the interactor that's releasing it is a hand interactor
        bool isHandInteractor = args.interactorObject is XRDirectInteractor || 
                                args.interactorObject is XRRayInteractor;
        
        // If it's not a hand interactor (e.g., it's the socket), ignore it
        if (!isHandInteractor)
        {
            wasSelected = false;
            return;
        }
        
        wasSelected = false;
        
        // Check if we're close to the socket when released from hand
        if (targetSocket != null && parentScript != null)
        {
            float distance = Vector3.Distance(transform.position, targetSocket.transform.position);
            if (distance <= returnDistance)
            {
                // We're close enough and released from hand - return it!
                parentScript.HandleCarabinerReturned(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelected);
            grabInteractable.selectExited.RemoveListener(OnDeselected);
        }
    }
}

