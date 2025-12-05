using System.Collections.Generic;
using UnityEngine;
using GogoGaga.OptimizedRopesAndCables;

public class RopeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerClippingPoint;  // Transform on player rig where rope attaches
    [SerializeField] private Rope ropeComponent;              // The Rope script component
    [SerializeField] private RopeMesh ropeMeshComponent;      // Optional: RopeMesh for 3D mesh
    
    [Header("Settings")]
    [SerializeField] private bool ropeDisconnectedFromPlayer = false;  // When true, rope doesn't connect to player
    
    // List to track placed carabiners in order (newest first)
    private List<Transform> placedCarabiners = new List<Transform>();
    
    // Reference to the rope GameObject
    private GameObject ropeObject;
    
    void Start()
    {
        // Find or create rope object
        if (ropeComponent == null)
        {
            CreateRopeObject();
        }
        
        Debug.Log("RopeManager initialized. Waiting for carabiners...");
    }
    
    void CreateRopeObject()
    {
        // Create a new GameObject for the rope
        ropeObject = new GameObject("DynamicRope");
        ropeObject.transform.SetParent(transform);
        
        // Add LineRenderer component (required by Rope script)
        LineRenderer lr = ropeObject.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.white;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        
        // Add Rope component
        ropeComponent = ropeObject.AddComponent<Rope>();
        
        // Optionally add RopeMesh for 3D mesh rendering
        ropeMeshComponent = ropeObject.AddComponent<RopeMesh>();
    }
    
    /// <summary>
    /// Call this when a carabiner is placed/attached to a surface
    /// </summary>
    public void OnCarabinerPlaced(Transform carabinerTransform)
    {
        // Find the clipping point on the carabiner
        Transform clippingPoint = carabinerTransform.Find("RopePoint");
        if (clippingPoint == null)
        {
            Debug.LogError($"Carabiner '{carabinerTransform.name}' doesn't have a 'RopePoint' child!");
            return;
        }
        
        // Add to the front of the list (newest first)
        placedCarabiners.Insert(0, clippingPoint);
        
        // Update rope connections
        UpdateRopeConnections();
    }
    
    /// <summary>
    /// Call this when a carabiner is removed/unclipped
    /// </summary>
    public void OnCarabinerRemoved(Transform carabinerTransform)
    {
        // Find the clipping point
        Transform clippingPoint = carabinerTransform.Find("RopePoint");
        if (clippingPoint == null) return;
        
        // Remove from list
        placedCarabiners.Remove(clippingPoint);
        
        // Update rope connections
        UpdateRopeConnections();
    }
    
    /// <summary>
    /// Updates the rope to connect from player to carabiners in correct order
    /// </summary>
    void UpdateRopeConnections()
    {
        if (ropeComponent == null)
        {
            Debug.LogError("RopeComponent is null!");
            return;
        }
        
        // If no carabiners placed, disable the rope GameObject
        if (placedCarabiners.Count == 0)
        {
            if (ropeObject != null)
                ropeObject.SetActive(false);
            return;
        }
        
        // Make sure rope is active when we have carabiners
        if (ropeObject != null && !ropeObject.activeSelf)
            ropeObject.SetActive(true);
        
        // If rope is disconnected from player, start from first carabiner
        if (ropeDisconnectedFromPlayer)
        {
            if (placedCarabiners.Count == 1)
            {
                // Only one carabiner - hide rope
                ropeObject.SetActive(false);
            }
            else
            {
                // Connect from newest to oldest carabiner
                ropeComponent.SetStartPoint(placedCarabiners[0], true);
                ropeComponent.SetEndPoint(placedCarabiners[placedCarabiners.Count - 1], true);
                Debug.Log($"Rope disconnected mode: {placedCarabiners[0].name} -> {placedCarabiners[placedCarabiners.Count - 1].name}");
            }
        }
        else
        {
            // Normal mode: player -> newest carabiner -> ... -> oldest carabiner
            if (playerClippingPoint == null)
            {
                Debug.LogError("Player clipping point not assigned!");
                return;
            }
            
            // Start point is always the player
            ropeComponent.SetStartPoint(playerClippingPoint, true);
            
            // End point is the oldest (last placed) carabiner
            ropeComponent.SetEndPoint(placedCarabiners[placedCarabiners.Count - 1], true);
            
            Debug.Log($"Rope connected: Player -> {placedCarabiners.Count} carabiner(s)");
        }
    }
    
    /// <summary>
    /// Call this when player reaches the top to disconnect rope from player
    /// </summary>
    public void DisconnectRopeFromPlayer()
    {
        ropeDisconnectedFromPlayer = true;
        UpdateRopeConnections();
    }
    
    /// <summary>
    /// Reconnect rope to player (if needed)
    /// </summary>
    public void ReconnectRopeToPlayer()
    {
        ropeDisconnectedFromPlayer = false;
        UpdateRopeConnections();
    }
    
    /// <summary>
    /// Get the list of placed carabiners (for debugging)
    /// </summary>
    public List<Transform> GetPlacedCarabiners()
    {
        return new List<Transform>(placedCarabiners);
    }
}