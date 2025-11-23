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
        private readonly Dictionary<RequestPriority, IBatchingStrategy> _priorityBatchingStrategies;
        
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
            this._priorityBatchingStrategies = new Dictionary<RequestPriority, IBatchingStrategy>();
            
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
        public void EnqueueRequest(string endpoint, object data, string httpMethod, RequestConfig config,
            Action<bool, string> callback = null)
        {
            var jsonBody = JsonSerializer.SerializeCompact(data);
            this.EnqueueRequestRaw(endpoint, jsonBody, httpMethod, config, callback);
        }
        
        /// <summary>
        /// Enqueue request với pre-serialized JSON
        /// </summary>
        public void EnqueueRequestRaw(string endpoint, string jsonBody, string httpMethod, RequestConfig config,
            Action<bool, string> callback = null)
        {
            var request = new QueuedRequest(endpoint, jsonBody, httpMethod, config.priority, config, callback);
            
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
            
            if (config.canBatch)
            {
                // Try get strategy by priority first, then by endpoint
                IBatchingStrategy strategy = null;
                
                if (this._priorityBatchingStrategies.TryGetValue(config.priority, out strategy) ||
                    this.TryGetBatchingStrategy(endpoint, out strategy))
                {
                    this.AddToBatch(request, strategy);
                }
                else
                {
                    this._requestQueue.Enqueue(request);
                }
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
        /// Đăng ký batching strategy cho priority level
        /// </summary>
        public void RegisterPriorityBatchingStrategy(RequestPriority priority, IBatchingStrategy strategy)
        {
            this._priorityBatchingStrategies[priority] = strategy;
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
                        var batchKey = kvp.Key;
                        var batch = kvp.Value;
                        
                        if (batch.Count == 0)
                        {
                            continue;
                        }
                        
                        // Get strategy từ batch's first request
                        if (batch.Count > 0)
                        {
                            var firstRequest = batch[0];
                            IBatchingStrategy strategy = null;
                            
                            // Try get by priority first, then by endpoint
                            if (!this._priorityBatchingStrategies.TryGetValue(firstRequest.config.priority, out strategy) &&
                                !this.TryGetBatchingStrategy(firstRequest.endpoint, out strategy))
                            {
                                continue;
                            }
                            
                            var firstBatchTime = this._batchTimers[batchKey];
                            
                            if (strategy.ShouldSendBatch(batch, firstBatchTime))
                            {
                                batchesToSend.Add(batchKey);
                            }
                        }
                    }
                    
                    foreach (var batchKey in batchesToSend)
                    {
                        if (this._rateLimiter.CanSendRequest())
                        {
                            await this.SendBatchAsync(batchKey);
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
            // Tạo batch key dựa trên endpoint và priority để group requests có cùng đặc điểm
            var batchKey = $"{request.endpoint}_{request.priority}";
            
            if (!this._batchBuffers.ContainsKey(batchKey))
            {
                this._batchBuffers[batchKey] = new List<QueuedRequest>();
                this._batchTimers[batchKey] = Time.realtimeSinceStartup;
            }
            
            var batch = this._batchBuffers[batchKey];
            
            if (strategy.CanAddToBatch(request, batch))
            {
                batch.Add(request);
                Debug.Log($"[RequestQueueManager] Added to batch for {batchKey} (count: {batch.Count})");
            }
            else
            {
                this.SendBatchAsync(batchKey).Forget();
                
                this._batchBuffers[batchKey] = new List<QueuedRequest> { request };
                this._batchTimers[batchKey] = Time.realtimeSinceStartup;
            }
        }
        
        private async UniTask SendBatchAsync(string batchKey)
        {
            if (!this._batchBuffers.ContainsKey(batchKey) || this._batchBuffers[batchKey].Count == 0)
            {
                return;
            }
            
            var batch = this._batchBuffers[batchKey];
            
            // Get strategy từ batch's first request
            IBatchingStrategy strategy = null;
            var firstRequest = batch[0];
            
            if (!this._priorityBatchingStrategies.TryGetValue(firstRequest.config.priority, out strategy) &&
                !this.TryGetBatchingStrategy(firstRequest.endpoint, out strategy))
            {
                Debug.LogWarning($"[RequestQueueManager] No batching strategy found for batch key: {batchKey}");
                return;
            }
            
            this._batchBuffers[batchKey] = new List<QueuedRequest>();
            this._batchTimers[batchKey] = Time.realtimeSinceStartup;
            
            Debug.Log($"[RequestQueueManager] Sending batch of {batch.Count} requests (key: {batchKey}, endpoint: {firstRequest.endpoint})");
            
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

