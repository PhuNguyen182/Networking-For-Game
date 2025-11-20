using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Configuration
{
    [CreateAssetMenu(fileName = "RequestQueueManagerConfig",
        menuName = "Scriptable Objects/NetworkingForGame/RequestQueueManagerConfig")]
    public class RequestQueueManagerConfig : ScriptableObject
    {
        [Header("Rate Limiting")] [SerializeField]
        public int maxRequestsPerSecond = 10;

        [SerializeField] public int maxRequestsPerMinute = 300;
        [SerializeField] public float rateLimitCooldown = 60f;

        [Header("Queue Settings")] [SerializeField]
        public int maxQueueSize = 1000;

        [SerializeField] public float processInterval = 0.1f;
        [SerializeField] public int maxConcurrentRequests = 5;

        [Header("Offline Support")] [SerializeField]
        public bool enableOfflineQueue = true;

        [SerializeField] public int maxOfflineQueueSize = 500;
        [SerializeField] public string offlineQueueKey = "offline_queue";

        [Header("Network Detection")] [SerializeField]
        public float networkCheckInterval = 5f;

        [SerializeField] public string healthCheckUrl = "https://your-server.com/health";
    }
}
