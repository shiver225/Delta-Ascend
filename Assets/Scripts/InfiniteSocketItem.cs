using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public sealed class InfiniteSocketItem : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    [SerializeField] GameObject displayPrefab;   // mesh-only version for the socket
    [SerializeField] GameObject livePrefab;      // grab-enabled prefab handed to the player
    [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable displayProxy;
    [SerializeField] XRInteractionManager interactionManager;

    UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable displayInteractable;

    void Awake()
    {
        if (!socket) socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (!interactionManager) interactionManager = FindObjectOfType<XRInteractionManager>();
    }

    void OnEnable()
    {
        SpawnOrResetDisplay();
        displayInteractable.selectEntered.AddListener(HandlePlayerPullsItem);
    }

    void OnDisable()
    {
        socket.selectEntered.RemoveListener(HandlePlayerPullsItem);
    }

    void SpawnOrResetDisplay()
    {
        // if (displayInteractable == null)
        // {
        //     var displayObj = Instantiate(displayPrefab, socket.attachTransform);
        //     if (displayObj.TryGetComponent(out UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)) grab.enabled = false;
        //     displayInteractable = displayObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>();
        // }

        // // Make sure the socket thinks it is holding the display object
        // socket.StartManualInteraction(displayInteractable);

        if (displayInteractable != null) return;

        var displayObj = Instantiate(displayPrefab, socket.attachTransform);
        displayInteractable = displayObj.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>();

        if (!displayProxy)
            displayProxy = displayObj.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    void HandlePlayerPullsItem(SelectEnterEventArgs args)
    {
        // var liveObj = Instantiate(livePrefab, socket.attachTransform.position, socket.attachTransform.rotation);
        // var liveInteractable = liveObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>();

        // // Hand the live object to the player’s interactor (controller)
        // interactionManager.SelectEnter(args.interactorObject, liveInteractable);

        // // Immediately reassign the display back to the socket so it looks like it never left
        // interactionManager.SelectEnter(socket, displayInteractable);

        var liveObj = Instantiate(livePrefab,
            socket.attachTransform.position,
            socket.attachTransform.rotation);

        var liveInteractable = liveObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>();

        // Give the spawned copy to the player’s interactor (their hand/controller)
        interactionManager.SelectEnter(args.interactorObject, liveInteractable);

        // Force the proxy to release so it can be triggered again immediately
        interactionManager.SelectExit(args.interactorObject, displayProxy);
    }
}