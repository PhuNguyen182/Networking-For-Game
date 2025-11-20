using System;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Configuration
{
    /// <summary>
    /// Request configuration for different types
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "RequestConfig", menuName = "Scriptable Objects/NetworkingForGame/RequestConfig")]
    public class RequestConfig : ScriptableObject
    {
        public RequestPriority priority;
        public bool canBatch;           // Can this request type be batched?
        public int maxBatchSize;        // Max items in a batch
        public float maxBatchDelay;     // Max seconds to wait before forcing batch send
        public bool bypassRateLimit;    // Critical requests bypass rate limit
        public int maxRetries;          // Max retry attempts
        public float retryDelay;        // Base retry delay (will use exponential backoff)
    }
}
