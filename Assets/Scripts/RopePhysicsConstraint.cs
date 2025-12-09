using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RopePhysicsConstraint : MonoBehaviour
{
    public RopeManager ropeManager;
    public Transform playerClippingPoint;
    public CharacterController characterController;
    public float maxRopeLength = 10f;
    public float ropeSegmentLength = 3f;
    public bool enableConstraint = true;
    public bool showDebugLogs = false;
    public bool enableHapticFeedback = true;
    public float hapticIntensity = 0.4f;
    public float hapticDuration = 0.1f;

    void Start()
    {
        if (ropeManager == null) ropeManager = FindObjectOfType<RopeManager>();
        if (playerClippingPoint == null && ropeManager != null)
            playerClippingPoint = ropeManager.playerClippingPoint;
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (showDebugLogs)
        {
            Debug.Log("[RopeConstraint] Initialized:");
            Debug.Log($"  - RopeManager: {(ropeManager ? "FOUND" : "MISSING")}");
            Debug.Log($"  - ClippingPoint: {(playerClippingPoint ? "FOUND" : "MISSING")}");
            Debug.Log($"  - CharacterController: {(characterController ? "FOUND" : "MISSING")}");
            Debug.Log($"  - Max Rope Length: {maxRopeLength}m");
            Debug.Log($"  - Constraint Enabled: {enableConstraint}");
        }
    }

    void LateUpdate()
    {
        if (!enableConstraint || ropeManager == null || playerClippingPoint == null) return;

        // If rope is disconnected from player, skip
        if (ropeManager.ropeDisconnectedFromPlayer) return;

        // Build points: player + active chain carabiners
        List<Transform> points = ropeManager.GetActiveChainPoints();
        if (points == null || points.Count == 0) return;

        float totalNeeded = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            totalNeeded += Vector3.Distance(points[i].position, points[i + 1].position);
        }

        if (totalNeeded > maxRopeLength)
        {
            if (showDebugLogs) Debug.LogWarning($"[RopeConstraint] PULLING PLAYER BACK! Distance: {totalNeeded:F2} > Max: {maxRopeLength:F2}");
            ConstrainPlayerPosition(points, totalNeeded);
            PlayHapticFeedback();
        }
    }

    void ConstrainPlayerPosition(List<Transform> points, float totalRopeNeeded)
    {
        // Pull back along the vector from player toward the first carabiner (or keep rope length)
        Vector3 playerPos = transform.position;
        Vector3 targetPos = points[1].position; // first point after player

        Vector3 dir = (targetPos - playerPos).normalized;
        float excess = totalRopeNeeded - maxRopeLength;
        Vector3 correction = dir * excess;

        Vector3 newPos = playerPos + correction;

        if (characterController != null)
            characterController.Move(newPos - playerPos);
        else
            transform.position = newPos;
    }

    void PlayHapticFeedback()
    {
        if (!enableHapticFeedback) return;
        var controllers = FindObjectsOfType<XRBaseController>();
        int count = 0;
        foreach (var c in controllers)
        {
            if (c != null)
            {
                c.SendHapticImpulse(hapticIntensity, hapticDuration);
                count++;
            }
        }
        if (showDebugLogs) Debug.Log($"[RopeConstraint] Haptics sent to {count} controllers");
    }
}