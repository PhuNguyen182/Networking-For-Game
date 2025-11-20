using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Unity;
using GameNetworking.RequestOptimizer.Scripts.Utilities;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Examples
{
    /// <summary>
    /// Example sử dụng Request Queue Manager
    /// </summary>
    public class RequestQueueExample : MonoBehaviour
    {
        [SerializeField] private RequestConfigCollection configCollection;
        
        private void Start()
        {
            this.DemonstrateBasicUsage();
            this.DemonstrateAsyncUsage().Forget();
            this.DemonstrateBatchingUsage();
            this.DemonstrateCriticalRequest();
            this.MonitorStatistics();
        }
        
        /// <summary>
        /// Cách sử dụng cơ bản với callback
        /// </summary>
        private void DemonstrateBasicUsage()
        {
            var config = this.configCollection.GetRequestConfigByPriority(RequestPriority.Normal);
            
            var eventData = new
            {
                eventName = "PlayerLevelUp",
                level = 10,
                experienceGained = 1000,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            RequestQueueManagerBehaviour.Instance.EnqueueRequest(
                endpoint: "https://api.example.com/events",
                data: eventData,
                config: config,
                callback: (success, response) =>
                {
                    if (success)
                    {
                        Debug.Log($"✅ Event sent successfully: {response}");
                    }
                    else
                    {
                        Debug.LogError($"❌ Event failed: {response}");
                    }
                }
            );
        }
        
        /// <summary>
        /// Cách sử dụng async/await với UniTask
        /// </summary>
        private async UniTaskVoid DemonstrateAsyncUsage()
        {
            var config = this.configCollection.GetRequestConfigByPriority(RequestPriority.High);
            
            var purchaseData = new
            {
                itemId = "sword_legendary_001",
                price = 999,
                currency = "gems",
                playerId = "player_12345"
            };
            
            var (success, response) = await RequestQueueManagerBehaviour.Instance
                .EnqueueRequestAsync(
                    endpoint: "https://api.example.com/purchase",
                    data: purchaseData,
                    config: config
                );
            
            if (success)
            {
                Debug.Log($"✅ Purchase successful: {response}");
                this.ProcessPurchaseSuccess(response);
            }
            else
            {
                Debug.LogError($"❌ Purchase failed: {response}");
                this.ShowPurchaseError(response);
            }
        }
        
        /// <summary>
        /// Sử dụng batching cho analytics events
        /// </summary>
        private void DemonstrateBatchingUsage()
        {
            var config = this.configCollection.GetRequestConfigByPriority(RequestPriority.Batch);
            
            for (var i = 0; i < 10; i++)
            {
                var analyticsData = new
                {
                    eventType = "player_action",
                    action = $"action_{i}",
                    timestamp = System.DateTime.UtcNow.ToString("o")
                };
                
                RequestQueueManagerBehaviour.Instance.EnqueueBatchRequest(
                    endpoint: "https://api.example.com/analytics",
                    data: analyticsData,
                    config: config,
                    callback: (success, response) =>
                    {
                        if (success)
                        {
                            Debug.Log($"✅ Analytics batch sent");
                        }
                    }
                );
            }
        }
        
        /// <summary>
        /// Critical request với bypass rate limit
        /// </summary>
        private void DemonstrateCriticalRequest()
        {
            var config = this.configCollection.GetRequestConfigByPriority(RequestPriority.Critical);
            
            var criticalData = new
            {
                eventType = "player_death",
                deathReason = "boss_fight",
                itemsLost = new[] { "sword", "shield", "potion" },
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            RequestQueueManagerBehaviour.Instance.EnqueueCriticalRequest(
                endpoint: "https://api.example.com/critical-events",
                data: criticalData,
                config: config,
                callback: (success, response) =>
                {
                    if (success)
                    {
                        Debug.Log($"✅ Critical event sent immediately");
                    }
                    else
                    {
                        Debug.LogError($"❌ Critical event failed - this is serious!");
                    }
                }
            );
        }
        
        /// <summary>
        /// Monitor queue statistics
        /// </summary>
        private void MonitorStatistics()
        {
            RequestQueueManagerBehaviour.Instance.OnStatisticsUpdated += stats =>
            {
                Debug.Log($"📊 Queue Statistics: {stats}");
                
                if (stats.TotalQueued > 500)
                {
                    Debug.LogWarning("⚠️ Queue is getting large, consider reducing request rate");
                }
                
                if (stats.IsRateLimited)
                {
                    Debug.LogWarning("⚠️ Rate limited! Requests are being queued");
                }
                
                if (!stats.IsOnline)
                {
                    Debug.LogWarning("⚠️ Offline! Requests will be saved to offline queue");
                }
            };
        }
        
        private void ProcessPurchaseSuccess(string response)
        {
            Debug.Log($"Processing purchase: {response}");
        }
        
        private void ShowPurchaseError(string errorMessage)
        {
            Debug.LogError($"Showing purchase error UI: {errorMessage}");
        }
    }
}

