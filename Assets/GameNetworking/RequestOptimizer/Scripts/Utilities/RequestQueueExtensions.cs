using System;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Unity;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Utilities
{
    /// <summary>
    /// Extension methods để đơn giản hóa việc sử dụng Request Queue
    /// </summary>
    public static class RequestQueueExtensions
    {
        /// <summary>
        /// Enqueue request với UniTask callback thay vì Action callback
        /// </summary>
        public static async UniTask<(bool success, string response)> EnqueueRequestAsync(
            this RequestQueueManagerBehaviour manager,
            string endpoint,
            object data,
            RequestConfig config)
        {
            var completionSource = new UniTaskCompletionSource<(bool, string)>();
            
            manager.EnqueueRequest(endpoint, data, config, (success, response) =>
            {
                completionSource.TrySetResult((success, response));
            });
            
            return await completionSource.Task;
        }
        
        /// <summary>
        /// Enqueue request với raw JSON và UniTask callback
        /// </summary>
        public static async UniTask<(bool success, string response)> EnqueueRequestRawAsync(
            this RequestQueueManagerBehaviour manager,
            string endpoint,
            string jsonBody,
            RequestConfig config)
        {
            var completionSource = new UniTaskCompletionSource<(bool, string)>();
            
            manager.EnqueueRequestRaw(endpoint, jsonBody, config, (success, response) =>
            {
                completionSource.TrySetResult((success, response));
            });
            
            return await completionSource.Task;
        }
        
        /// <summary>
        /// Enqueue critical request với bypass rate limit
        /// </summary>
        public static void EnqueueCriticalRequest(
            this RequestQueueManagerBehaviour manager,
            string endpoint,
            object data,
            RequestConfig config,
            Action<bool, string> callback = null)
        {
            if (config.priority != RequestPriority.Critical)
            {
                Debug.LogWarning($"[RequestQueueExtensions] Config priority is not Critical, forcing Critical priority");
            }
            
            var criticalConfig = ScriptableObject.CreateInstance<RequestConfig>();
            criticalConfig.priority = RequestPriority.Critical;
            criticalConfig.canBatch = false;
            criticalConfig.bypassRateLimit = true;
            criticalConfig.maxRetries = config.maxRetries;
            criticalConfig.retryDelay = config.retryDelay;
            
            manager.EnqueueRequest(endpoint, data, criticalConfig, callback);
        }
        
        /// <summary>
        /// Enqueue batch request với low priority
        /// </summary>
        public static void EnqueueBatchRequest(
            this RequestQueueManagerBehaviour manager,
            string endpoint,
            object data,
            RequestConfig config,
            Action<bool, string> callback = null)
        {
            if (!config.canBatch)
            {
                Debug.LogWarning($"[RequestQueueExtensions] Config does not allow batching");
            }
            
            manager.EnqueueRequest(endpoint, data, config, callback);
        }
    }
}

