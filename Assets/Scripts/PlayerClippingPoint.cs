using UnityEngine;
using UnityEngine.XR;

public class PlayerClippingPoint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RopeManager ropeManager;

    [Header("Settings")]
    [SerializeField] private bool allowReconnection = true;
    [SerializeField] private bool startConnected = true;
    [SerializeField] private float buttonCooldown = 0.35f;   // debounce for X button
    [SerializeField] private float maxReconnectDistance = 10f; // match rope length

    [Header("Haptics")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private float hapticIntensity = 0.6f;
    [SerializeField] private float hapticDuration = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isConnected = true;
    private float lastPressTime = 0f;

    void Start()
    {
        if (ropeManager == null) ropeManager = FindObjectOfType<RopeManager>();
        if (ropeManager == null)
        {
            Debug.LogError("[ClippingPoint] RopeManager not found!");
            enabled = false;
            return;
        }

        isConnected = startConnected;
        ropeManager.SetPlayerConnectedState(isConnected);

        if (showDebugLogs)
            Debug.Log($"[ClippingPoint] Ready. Rope {(isConnected ? "CONNECTED" : "DISCONNECTED")}");
    }

    void Update()
    {
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!leftHand.isValid) return;

        // X button on left controller is CommonUsages.primaryButton
        if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) && pressed)
        {
            if (Time.time - lastPressTime > buttonCooldown)
            {
                HandleToggle(leftHand);
                lastPressTime = Time.time;
            }
        }
    }

    void HandleToggle(InputDevice device)
    {
        if (ropeManager == null) return;

        // If no rope exists at all, ignore button
        if (!ropeManager.HasActiveCarabiners() && isConnected)
        {
            if (showDebugLogs) Debug.Log("[ClippingPoint] No rope present; ignoring toggle.");
            return;
        }

        if (isConnected)
        {
            // Disconnect (but keep current chain intact)
            ropeManager.DisconnectRopeFromPlayer();
            isConnected = false;
            if (showDebugLogs) Debug.LogWarning("[ClippingPoint] ROPE DISCONNECTED");
            SendHaptics(device, hapticIntensity, hapticDuration);
        }
        else if (allowReconnection)
        {
            // If there is an active chain and we’re too far, block reconnect
            if (ropeManager.HasActiveCarabiners() && !ropeManager.WithinActiveChainRange(maxReconnectDistance))
            {
                if (showDebugLogs) Debug.LogWarning("[ClippingPoint] Too far to reconnect. Move closer (< rope length).");
                return;
            }

            // Reconnect to the current active chain
            ropeManager.ReconnectRopeToPlayer();
            isConnected = true;
            if (showDebugLogs) Debug.LogWarning("[ClippingPoint] ROPE RECONNECTED");
            SendHaptics(device, hapticIntensity * 0.7f, hapticDuration * 1.2f);
        }
    }

    void SendHaptics(InputDevice device, float intensity, float duration)
    {
        if (!enableHaptics || !device.isValid) return;
        if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0, intensity, duration);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isConnected ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}