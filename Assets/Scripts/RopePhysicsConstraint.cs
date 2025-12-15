// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit;

// public class RopePhysicsConstraint : MonoBehaviour
// {
//     public RopeManager ropeManager;
//     public Transform playerClippingPoint;
//     public CharacterController characterController;

//     [Header("Rope Limits")]
//     public float maxRopeLength = 10f;
//     public float ropeSegmentLength = 3f;

//     [Header("Pull Tuning")]
//     [Tooltip("Extra slack before pull starts (meters)")]
//     public float slackDistance = 0.3f;
//     [Tooltip("How quickly we interpolate toward the correction")]
//     public float pullLerpSpeed = 6f;
//     [Tooltip("Hard cap on pull distance per frame (meters)")]
//     public float maxPullPerFrame = 0.5f;

//     [Header("Misc")]
//     public bool enableConstraint = true;
//     public bool showDebugLogs = false;
//     public bool enableHapticFeedback = true;
//     public float hapticIntensity = 0.4f;
//     public float hapticDuration = 0.1f;

//     void Start()
//     {
//         if (ropeManager == null)
//         {
//             ropeManager = FindObjectOfType<RopeManager>();
//         } 
//         if (playerClippingPoint == null && ropeManager != null)
//         {
//             playerClippingPoint = ropeManager.playerClippingPoint;
//         } 
//         if (characterController == null)
//         {
//             characterController = GetComponent<CharacterController>();
//         }

//         if (showDebugLogs)
//         {
//             Debug.Log("[RopeConstraint] Initialized:");
//             Debug.Log($"  - RopeManager: {(ropeManager ? "FOUND" : "MISSING")}");
//             Debug.Log($"  - ClippingPoint: {(playerClippingPoint ? "FOUND" : "MISSING")}");
//             Debug.Log($"  - CharacterController: {(characterController ? "FOUND" : "MISSING")}");
//             Debug.Log($"  - Max Rope Length: {maxRopeLength}m");
//             Debug.Log($"  - Constraint Enabled: {enableConstraint}");
//         }
//     }

//     void LateUpdate()
//     {
//         if (!enableConstraint || ropeManager == null || playerClippingPoint == null) return;
//         if (ropeManager.ropeDisconnectedFromPlayer) return; // skip if unclipped

//         // Build points: player + active chain carabiners
//         List<Transform> points = ropeManager.GetActiveChainPoints();
//         if (points == null || points.Count == 0) return;

//         float totalNeeded = 0f;
//         for (int i = 0; i < points.Count - 1; i++)
//             totalNeeded += Vector3.Distance(points[i].position, points[i + 1].position);

//         float effectiveMax = maxRopeLength + slackDistance;
//         if (totalNeeded > effectiveMax)
//         {
//             if (showDebugLogs)
//                 Debug.LogWarning($"[RopeConstraint] PULLING PLAYER BACK! Distance: {totalNeeded:F2} > Max: {effectiveMax:F2}");
//             ConstrainPlayerPosition(points, totalNeeded, effectiveMax);
//             PlayHapticFeedback();
//         }
//     }

//     void ConstrainPlayerPosition(List<Transform> points, float totalRopeNeeded, float effectiveMax)
//     {
//         // Pull back toward the first segment after the player
//         Vector3 playerPos = transform.position;
//         Vector3 targetPos = points[1].position; // first point after player

//         Vector3 dir = (targetPos - playerPos).normalized;
//         float excess = totalRopeNeeded - effectiveMax;
//         Vector3 fullCorrection = dir * excess;

//         // Smooth + clamp per frame so you can still grab while tugged
//         Vector3 step = Vector3.Lerp(Vector3.zero, fullCorrection, Time.deltaTime * pullLerpSpeed);
//         if (step.magnitude > maxPullPerFrame)
//             step = step.normalized * maxPullPerFrame;

//         if (characterController != null)
//             characterController.Move(step);
//         else
//             transform.position += step;
//     }

//     void PlayHapticFeedback()
//     {
//         if (!enableHapticFeedback) return;
//         var controllers = FindObjectsOfType<XRBaseController>();
//         int count = 0;
//         foreach (var c in controllers)
//         {
//             if (c != null)
//             {
//                 c.SendHapticImpulse(hapticIntensity, hapticDuration);
//                 count++;
//             }
//         }
//         if (showDebugLogs) Debug.Log($"[RopeConstraint] Haptics sent to {count} controllers");
//     }
// }

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RopePhysicsConstraint : MonoBehaviour
{
    public RopeManager ropeManager;
    public Transform playerClippingPoint;
    public CharacterController characterController;

    [Header("Rope Limits")]
    [Tooltip("Max distance from player to newest carabiner (rope segment length in meters)")]
    public float ropeSegmentLength = 3f;
    [Tooltip("Extra slack before clamping (meters)")]
    public float slackDistance = 0.15f;

    [Header("Constraint Settings")]
    [Tooltip("Max position correction per frame (meters) - increase if falling through")]
    public float maxAdjustPerFrame = 0.5f;
    [Tooltip("When true, clamps player to sphere around carabiner (hard limit)")]
    public bool useHardClamp = true;

    [Header("Behavior")]
    public bool enableConstraint = true;
    public bool showDebugLogs = false;

    [Header("Haptics")]
    public bool enableHapticFeedback = true;
    public float hapticIntensity = 0.2f;
    public float hapticDuration = 0.05f;

    private float lastHapticTime = 0f;

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
            Debug.Log($"  - Rope Segment Length: {ropeSegmentLength}m");
            Debug.Log($"  - Constraint Enabled: {enableConstraint}");
        }
    }

    void LateUpdate()
    {
        if (!enableConstraint || ropeManager == null || playerClippingPoint == null) return;
        if (ropeManager.ropeDisconnectedFromPlayer) return;

        // Get newest carabiner only
        Transform newestCarabiner = ropeManager.GetNewestCarabiner();
        if (newestCarabiner == null) return;

        // Check distance from player to newest carabiner ONLY
        Vector3 playerPos = transform.position;
        Vector3 carabinerPos = newestCarabiner.position;
        float distance = Vector3.Distance(playerPos, carabinerPos);

        float maxDistance = ropeSegmentLength + slackDistance;
        if (distance > maxDistance)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[RopeConstraint] Distance {distance:F2}m > Max {maxDistance:F2}m - CLAMPING!");

            ConstrainPlayerPosition(playerPos, carabinerPos, distance, maxDistance);
            PlayHapticFeedback();
        }
    }

    void ConstrainPlayerPosition(Vector3 playerPos, Vector3 carabinerPos, float currentDist, float maxDist)
    {
        // How much over the limit
        float excess = currentDist - maxDist;
        if (excess <= 0.001f) return;

        if (useHardClamp)
        {
            // HARD CLAMP: Directly set player position to sphere boundary
            // This prevents falling through and works instantly
            Vector3 dirFromCarabiner = (playerPos - carabinerPos).normalized;
            Vector3 clampedPos = carabinerPos + dirFromCarabiner * maxDist;
            
            // Calculate the movement needed
            Vector3 correction = clampedPos - playerPos;
            
            // Apply correction
            if (characterController != null && characterController.enabled)
                characterController.Move(correction);
            else
                transform.position = clampedPos;
                
            if (showDebugLogs)
                Debug.Log($"[RopeConstraint] Hard clamped by {correction.magnitude:F2}m");
        }
        else
        {
            // SOFT CLAMP: Gradual pull back (may not stop fast falls)
            Vector3 dir = (carabinerPos - playerPos).normalized;
            Vector3 correction = dir * Mathf.Min(excess, maxAdjustPerFrame);

            if (characterController != null && characterController.enabled)
                characterController.Move(correction);
            else
                transform.position += correction;
        }
    }

    void PlayHapticFeedback()
    {
        if (!enableHapticFeedback) return;
        if (Time.time - lastHapticTime < 0.15f) return;

        var controllers = FindObjectsOfType<XRBaseController>();
        foreach (var c in controllers)
        {
            if (c != null)
                c.SendHapticImpulse(hapticIntensity, hapticDuration);
        }
        lastHapticTime = Time.time;
    }
}