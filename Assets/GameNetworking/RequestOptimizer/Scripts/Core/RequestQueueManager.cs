using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using GameNetworking.RequestOptimizer.Scripts.Utilities;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Main Request Queue Manager - Plain class sử dụng UniTask
    /// Tuân thủ SOLID principles với dependency injection
    /// </summary>
    public class RequestQueueManager : IDisposable
    {
        private readonly IRequestQueue _requestQueue;
        private readonly IRateLimiter _rateLimiter;
        private readonly INetworkMonitor _networkMonitor;
        private readonly IRequestSender _requestSender;
        private readonly IOfflineQueueStorage _offlineQueueStorage;
        private readonly RequestQueueManagerConfig _requestQueueManagerConfig;
        private readonly Dictionary<string, IBatchingStrategy> _batchingStrategies;
        
        private readonly Dictionary<string, List<QueuedRequest>> _batchBuffers;
        private readonly Dictionary<string, float> _batchTimers;
        private readonly HashSet<string> _processedRequestIds;
        
        private CancellationTokenSource _lifecycleCts;
        private bool _isDisposed;
        
        public event Action<QueueStatistics> OnStatisticsUpdated;
        
        public RequestQueueManager(
            RequestQueueManagerConfig config,
            IRequestQueue requestQueue,
            IRateLimiter rateLimiter,
            INetworkMonitor networkMonitor,
            IRequestSender requestSender,
            IOfflineQueueStorage offlineStorage,
            Dictionary<string, IBatchingStrategy> batchingStrategies = null)
        {
            this._requestQueueManagerConfig = config ?? throw new ArgumentNullException(nameof(config));
            this._requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));
            this._rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            this._networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
            this._requestSender = requestSender ?? throw new ArgumentNullException(nameof(requestSender));
            this._offlineQueueStorage = offlineStorage ?? throw new ArgumentNullException(nameof(offlineStorage));
            this._batchingStrategies = batchingStrategies ?? new Dictionary<string, IBatchingStrategy>();
            
            this._batchBuffers = new Dictionary<string, List<QueuedRequest>>();
            this._batchTimers = new Dictionary<string, float>();
            this._processedRequestIds = new HashSet<string>();
            
            this._networkMonitor.OnNetworkStatusChanged += this.OnNetworkStatusChanged;
        }
        
        /// <summary>
        /// Khởi động Request Queue Manager
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            this._lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            await this.LoadOfflineQueueAsync();
            
            var monitoringTask = this._networkMonitor.StartMonitoringAsync(this._lifecycleCts.Token);
            var queueProcessingTask = this.ProcessQueueLoopAsync(this._lifecycleCts.Token);
            var batchProcessingTask = this.ProcessBatchLoopAsync(this._lifecycleCts.Token);
            var rateLimiterUpdateTask = this.RateLimiterUpdateLoopAsync(this._lifecycleCts.Token);
            
            await UniTask.WhenAll(monitoringTask, queueProcessingTask, batchProcessingTask, rateLimiterUpdateTask);
        }
        
        /// <summary>
        /// Enqueue request với automatic JSON serialization
        /// </summary>
        public void EnqueueRequest(string endpoint, object data, RequestConfig config,
            Action<bool, string> callback = null)
        {
            var jsonBody = JsonSerializer.SerializeCompact(data);
            this.EnqueueRequestRaw(endpoint, jsonBody, config, callback);
        }
        
        /// <summary>
        /// Enqueue request với pre-serialized JSON
        /// </summary>
        public void EnqueueRequestRaw(string endpoint, string jsonBody, RequestConfig config,
            Action<bool, string> callback = null)
        {
            var request = new QueuedRequest(endpoint, jsonBody, config.priority, config, callback);
            
            if (this._processedRequestIds.Contains(request.requestId))
            {
                Debug.LogWarning($"[RequestQueueManager] Request {request.requestId} already processed, skipping");
                return;
            }
            
            if (config.priority == RequestPriority.Critical && config.bypassRateLimit)
            {
                this.SendRequestImmediateAsync(request).Forget();
                return;
            }
            
            if (config.canBatch && this.TryGetBatchingStrategy(endpoint, out var strategy))
            {
                this.AddToBatch(request, strategy);
            }
            else
            {
                this._requestQueue.Enqueue(request);
            }
            
            if (this._requestQueueManagerConfig.enableOfflineQueue && !this._networkMonitor.IsOnline)
            {
                this._offlineQueueStorage.SaveRequestAsync(request).Forget();
            }
        }
        
        /// <summary>
        /// Đăng ký batching strategy cho endpoint cụ thể
        /// </summary>
        public void RegisterBatchingStrategy(string endpoint, IBatchingStrategy strategy)
        {
            this._batchingStrategies[endpoint] = strategy;
        }
        
        /// <summary>
        /// Lấy thống kê hiện tại của queue
        /// </summary>
        public QueueStatistics GetStatistics()
        {
            var totalBatched = 0;
            foreach (var batch in this._batchBuffers.Values)
            {
                totalBatched += batch.Count;
            }
            
            return new QueueStatistics(
                this._requestQueue.TotalQueuedCount,
                totalBatched,
                this._requestSender.ActiveRequestsCount,
                this._rateLimiter.IsRateLimited,
                this._networkMonitor.IsOnline,
                0
            );
        }
        
        /// <summary>
        /// Xóa tất cả queues và batches
        /// </summary>
        public async UniTask ClearAllAsync()
        {
            this._requestQueue.Clear();
            this._batchBuffers.Clear();
            this._batchTimers.Clear();
            this._processedRequestIds.Clear();
            
            await this._offlineQueueStorage.ClearAsync();
        }
        
        public void Dispose()
        {
            if (this._isDisposed)
            {
                return;
            }
            
            this._lifecycleCts?.Cancel();
            this._lifecycleCts?.Dispose();
            this._networkMonitor.StopMonitoring();
            this._networkMonitor.OnNetworkStatusChanged -= this.OnNetworkStatusChanged;
            
            this._isDisposed = true;
        }
        
        private async UniTask ProcessQueueLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(this._requestQueueManagerConfig.processInterval),
                        cancellationToken: cancellationToken
                    );
                    
                    if (!this.CanProcessQueue())
                    {
                        continue;
                    }
                    
                    if (this._requestQueue.HasRequests && this._rateLimiter.CanSendRequest())
                    {
                        var request = this._requestQueue.Dequeue();
                        if (request != null)
                        {
                            this.SendRequestAsync(request).Forget();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RequestQueueManager] Error in queue processing: {ex.Message}");
                }
            }
        }
        
        private async UniTask ProcessBatchLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
                    
                    if (!this.CanProcessQueue())
                    {
                        continue;
                    }
                    
                    var batchesToSend = new List<string>();
                    
                    foreach (var kvp in this._batchBuffers)
                    {
                        var endpoint = kvp.Key;
                        var batch = kvp.Value;
                        
                        if (batch.Count == 0)
                        {
                            continue;
                        }
                        
                        if (!this.TryGetBatchingStrategy(endpoint, out var strategy))
                        {
                            continue;
                        }
                        
                        var firstBatchTime = this._batchTimers[endpoint];
                        
                        if (strategy.ShouldSendBatch(batch, firstBatchTime))
                        {
                            batchesToSend.Add(endpoint);
                        }
                    }
                    
                    foreach (var endpoint in batchesToSend)
                    {
                        if (this._rateLimiter.CanSendRequest())
                        {
                            await this.SendBatchAsync(endpoint);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RequestQueueManager] Error in batch processing: {ex.Message}");
                }
            }
        }
        
        private async UniTask RateLimiterUpdateLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.1f), cancellationToken: cancellationToken);
                    
                    this._rateLimiter.Update(Time.realtimeSinceStartup);
                    
                    var stats = this.GetStatistics();
                    this.OnStatisticsUpdated?.Invoke(stats);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RequestQueueManager] Error in rate limiter update: {ex.Message}");
                }
            }
        }
        
        private async UniTaskVoid SendRequestAsync(QueuedRequest request)
        {
            this._rateLimiter.RecordRequest();
            
            Debug.Log($"[RequestQueueManager][{request.priority}] Sending request to {request.endpoint}");
            
            var result = await this._requestSender.SendRequestAsync(request);
            
            if (result.ErrorType == RequestErrorType.RateLimitExceeded)
            {
                Debug.LogWarning("[RequestQueueManager] Hit rate limit (429)! Entering cooldown...");
                this._rateLimiter.ActivateRateLimitCooldown(this._requestQueueManagerConfig.rateLimitCooldown);
                this._requestQueue.Enqueue(request);
            }
            else if (!result.IsSuccess && request.retryCount < request.config.maxRetries)
            {
                var retryRequest = request.WithIncrementedRetry();
                this._requestQueue.Enqueue(retryRequest);
            }
            else
            {
                if (result.IsSuccess)
                {
                    this._processedRequestIds.Add(request.requestId);
                }
                
                request.Callback?.Invoke(result.IsSuccess, result.IsSuccess ? result.Response : result.ErrorMessage);
            }
        }
        
        private async UniTaskVoid SendRequestImmediateAsync(QueuedRequest request)
        {
            Debug.Log($"[RequestQueueManager][IMMEDIATE] Sending critical request to {request.endpoint}");
            
            var result = await this._requestSender.SendRequestImmediateAsync(request);
            
            if (result.IsSuccess)
            {
                this._processedRequestIds.Add(request.requestId);
            }
            
            request.Callback?.Invoke(result.IsSuccess, result.IsSuccess ? result.Response : result.ErrorMessage);
        }
        
        private void AddToBatch(QueuedRequest request, IBatchingStrategy strategy)
        {
            if (!this._batchBuffers.ContainsKey(request.endpoint))
            {
                this._batchBuffers[request.endpoint] = new List<QueuedRequest>();
                this._batchTimers[request.endpoint] = Time.realtimeSinceStartup;
            }
            
            var batch = this._batchBuffers[request.endpoint];
            
            if (strategy.CanAddToBatch(request, batch))
            {
                batch.Add(request);
                Debug.Log($"[RequestQueueManager] Added to batch for {request.endpoint} (count: {batch.Count})");
            }
            else
            {
                this.SendBatchAsync(request.endpoint).Forget();
                
                this._batchBuffers[request.endpoint] = new List<QueuedRequest> { request };
                this._batchTimers[request.endpoint] = Time.realtimeSinceStartup;
            }
        }
        
        private async UniTask SendBatchAsync(string endpoint)
        {
            if (!this._batchBuffers.ContainsKey(endpoint) || this._batchBuffers[endpoint].Count == 0)
            {
                return;
            }
            
            if (!this.TryGetBatchingStrategy(endpoint, out var strategy))
            {
                return;
            }
            
            var batch = this._batchBuffers[endpoint];
            this._batchBuffers[endpoint] = new List<QueuedRequest>();
            this._batchTimers[endpoint] = Time.realtimeSinceStartup;
            
            Debug.Log($"[RequestQueueManager] Sending batch of {batch.Count} requests to {endpoint}");
            
            var batchRequest = await strategy.CreateBatchRequestAsync(batch);
            
            this._rateLimiter.RecordRequest();
            var result = await this._requestSender.SendRequestAsync(batchRequest);
            
            await strategy.ProcessBatchResponseAsync(batch, result.IsSuccess, 
                result.IsSuccess ? result.Response : result.ErrorMessage);
            
            if (result.IsSuccess)
            {
                foreach (var req in batch)
                {
                    this._processedRequestIds.Add(req.requestId);
                }
            }
        }
        
        private bool CanProcessQueue()
        {
            return !this._rateLimiter.IsRateLimited && this._networkMonitor.IsOnline;
        }
        
        private bool TryGetBatchingStrategy(string endpoint, out IBatchingStrategy strategy)
        {
            return this._batchingStrategies.TryGetValue(endpoint, out strategy);
        }
        
        private async UniTask LoadOfflineQueueAsync()
        {
            if (!this._requestQueueManagerConfig.enableOfflineQueue)
            {
                return;
            }
            
            try
            {
                var offlineRequests = await this._offlineQueueStorage.LoadRequestsAsync();
                
                if (offlineRequests.Count > 0)
                {
                    Debug.Log($"[RequestQueueManager] Loading {offlineRequests.Count} offline requests");
                    
                    foreach (var request in offlineRequests)
                    {
                        this._requestQueue.Enqueue(request);
                    }
                    
                    await this._offlineQueueStorage.ClearAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RequestQueueManager] Failed to load offline queue: {ex.Message}");
            }
        }
        
        private void OnNetworkStatusChanged(bool isOnline)
        {
            Debug.Log($"[RequestQueueManager] Network status changed: {(isOnline ? "Online" : "Offline")}");
            
            if (isOnline && this._requestQueueManagerConfig.enableOfflineQueue)
            {
                this.LoadOfflineQueueAsync().Forget();
            }
        }
    }
}

