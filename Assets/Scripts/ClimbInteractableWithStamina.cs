using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Climbing;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbInteractableWithStamina : ClimbInteractable
{
    public PlayerClimbingManager climbingManager;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (climbingManager != null)
            climbingManager.RegisterClimb(args.interactorObject as IXRSelectInteractor);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (climbingManager != null)
            climbingManager.UnregisterClimb(args.interactorObject as IXRSelectInteractor);
    }

}
