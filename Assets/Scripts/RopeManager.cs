using System.Collections.Generic;
using UnityEngine;
using GogoGaga.OptimizedRopesAndCables;
using System.Reflection;

public class RopeManager : MonoBehaviour
{
    [Header("References")]
    public Transform playerClippingPoint;

    [Header("Rope Settings")]
    public Material ropeMaterial;
    public Color ropeColor = Color.white;
    public float ropeWidth = 0.02f;
    public float ropeSegmentLength = 3f;
    public float ropeStiffness = 500f;
    public float ropeDamping = 20f;
    public int lineQuality = 15;

    [Header("State")]
    public bool ropeDisconnectedFromPlayer = false;
    public bool showDebugLogs = false;

    private class RopeChain
    {
        public List<Transform> carabiners = new List<Transform>();
        public List<GameObject> ropeSegments = new List<GameObject>();
        public bool finalized = false;
    }

    private readonly List<RopeChain> chains = new List<RopeChain>();
    private int activeChainIndex = 0;

    void Start()
    {
        if (chains.Count == 0)
            chains.Add(new RopeChain());

        if (playerClippingPoint == null)
        {
            Debug.LogError("[RopeManager] playerClippingPoint not assigned!");
        }

        Debug.Log("RopeManager initialized. Waiting for carabiners...");
    }

    RopeChain ActiveChain => chains[activeChainIndex];

    public void SetPlayerConnectedState(bool connected)
    {
        ropeDisconnectedFromPlayer = !connected;
    }

    public bool HasActiveCarabiners()
    {
        return ActiveChain.carabiners.Count > 0 && !ActiveChain.finalized;
    }

    public bool WithinActiveChainRange(float maxDistance)
    {
        if (!HasActiveCarabiners()) return true;
        Transform newest = ActiveChain.carabiners[ActiveChain.carabiners.Count - 1];
        float dist = Vector3.Distance(playerClippingPoint.position, newest.position);
        return dist <= maxDistance;
    }

    public void FinalizeActiveChain()
    {
        ActiveChain.finalized = true;
    }

    public void StartNewChain()
    {
        var newChain = new RopeChain();
        chains.Add(newChain);
        activeChainIndex = chains.Count - 1;
        UpdateRopeConnections();
    }

    public void OnCarabinerPlaced(Transform carabinerTransform)
    {
        bool wasDisconnected = ropeDisconnectedFromPlayer;
        
        // If disconnected and already have a chain, finalize old chain and start new
        if (ropeDisconnectedFromPlayer && ActiveChain.carabiners.Count > 0 && !ActiveChain.finalized)
        {
            FinalizeActiveChain();
            StartNewChain();
        }

        Transform ropePoint = carabinerTransform.Find("RopePoint");
        if (ropePoint == null)
        {
            Debug.LogError("[RopeManager] RopePoint not found on carabiner!");
            return;
        }

        ActiveChain.carabiners.Add(ropePoint);
        Debug.Log($"[RopeManager] Found RopePoint on {carabinerTransform.name}");
        
        // AUTO-RECONNECT: If this is the first carabiner of a new chain and we were disconnected, reconnect
        if (wasDisconnected && ActiveChain.carabiners.Count == 1)
        {
            ropeDisconnectedFromPlayer = false;
            Debug.Log("[RopeManager] Auto-reconnected to new chain!");
            
            // Notify PlayerClippingPoint to update its state
            var clippingPoint = playerClippingPoint.GetComponent<PlayerClippingPoint>();
            if (clippingPoint != null)
            {
                clippingPoint.ForceReconnect();
            }
        }
        
        UpdateRopeConnections();
    }

    public void OnCarabinerRemoved(Transform carabinerTransform)
    {
        Transform ropePoint = carabinerTransform.Find("RopePoint");
        if (ropePoint == null) return;

        ActiveChain.carabiners.Remove(ropePoint);
        UpdateRopeConnections();
    }

    public void DisconnectRopeFromPlayer()
    {
        ropeDisconnectedFromPlayer = true;
        UpdateRopeConnections();
    }

    public void ReconnectRopeToPlayer()
    {
        ropeDisconnectedFromPlayer = false;
        UpdateRopeConnections();
    }

    void ClearRopeSegments(RopeChain chain)
    {
        foreach (var seg in chain.ropeSegments)
        {
            if (seg != null) Destroy(seg);
        }
        chain.ropeSegments.Clear();
    }

    void UpdateRopeConnections()
    {
        var chain = ActiveChain;

        ClearRopeSegments(chain);

        if (chain.finalized || chain.carabiners.Count == 0)
        {
            return;
        }

        // If disconnected: draw only between carabiners (keep chain visible)
        if (ropeDisconnectedFromPlayer)
        {
            for (int i = chain.carabiners.Count - 1; i > 0; i--)
            {
                CreateRopeSegment(chain, chain.carabiners[i], chain.carabiners[i - 1]);
            }
            if (showDebugLogs)
                Debug.Log($"[RopeManager] Disconnected: kept {chain.ropeSegments.Count} segments between carabiners.");
            return;
        }

        // Connected: Build segments player -> newest -> ... -> oldest
        Transform newest = chain.carabiners[chain.carabiners.Count - 1];

        // First segment: player to newest
        CreateRopeSegment(chain, playerClippingPoint, newest);

        // Subsequent segments: newest -> previous -> ...
        for (int i = chain.carabiners.Count - 1; i > 0; i--)
        {
            CreateRopeSegment(chain, chain.carabiners[i], chain.carabiners[i - 1]);
        }

        Debug.Log($"[RopeManager] Created {chain.ropeSegments.Count} rope segments (active chain).");
    }

    void CreateRopeSegment(RopeChain chain, Transform startPoint, Transform endPoint)
    {
        GameObject ropeObj = new GameObject("RopeSegment");
        LineRenderer lr = ropeObj.AddComponent<LineRenderer>();
        lr.material = ropeMaterial;
        lr.startColor = ropeColor;
        lr.endColor = ropeColor;
        lr.startWidth = ropeWidth;
        lr.endWidth = ropeWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        Rope rope = ropeObj.AddComponent<Rope>();

        var ropeType = typeof(Rope);
        var startField = ropeType.GetField("startPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        var endField = ropeType.GetField("endPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        if (startField != null && endField != null)
        {
            startField.SetValue(rope, startPoint);
            endField.SetValue(rope, endPoint);
        }
        else
        {
            Debug.LogError("[RopeManager] Failed to set private Rope fields.");
        }

        rope.linePoints = lineQuality;
        rope.stiffness = ropeStiffness;
        rope.damping = ropeDamping;
        rope.ropeLength = ropeSegmentLength;
        rope.ropeWidth = ropeWidth;

        chain.ropeSegments.Add(ropeObj);
    }

    public List<Transform> GetActiveChainPoints()
    {
        var chain = ActiveChain;
        if (chain.finalized || chain.carabiners.Count == 0) return null;
        var pts = new List<Transform>();
        pts.Add(playerClippingPoint);
        for (int i = chain.carabiners.Count - 1; i >= 0; i--)
            pts.Add(chain.carabiners[i]);
        return pts;
    }
    
    public Transform GetNewestCarabiner()
    {
        var chain = ActiveChain;
        if (chain.carabiners.Count == 0) return null;
        return chain.carabiners[chain.carabiners.Count - 1];
    }
}