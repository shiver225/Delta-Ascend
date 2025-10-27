using UnityEngine;
using UnityEngine.InputSystem;

public class UI : MonoBehaviour
{
    public GameObject wristUI;
    public InputActionProperty toggleAction;

    void Update()
    {
        if (toggleAction.action.WasPressedThisFrame())
        {
            wristUI.SetActive(!wristUI.activeSelf);
        }
    }
}
