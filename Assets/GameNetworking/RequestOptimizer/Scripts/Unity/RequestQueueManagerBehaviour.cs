using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Core;
using GameNetworking.RequestOptimizer.Scripts.BatchingStrategies;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Core;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using GameNetworking.RequestOptimizer.Scripts.Storage;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Unity
{
    /// <summary>
    /// MonoBehaviour adapter để kết nối RequestQueueManager với Unity lifecycle
    /// Singleton pattern cho dễ dàng truy cập
    /// </summary>
    public class RequestQueueManagerBehaviour : MonoBehaviour
    {
        private OptimizedWebRequestService _optimizedWebRequestService;
        
        private static RequestQueueManagerBehaviour _instance;
        
        public static RequestQueueManagerBehaviour Instance
        {
            get
            {
                if (_instance) 
                    return _instance;
                
                var go = new GameObject("[RequestQueueManager]");
                _instance = go.AddComponent<RequestQueueManagerBehaviour>();
                DontDestroyOnLoad(go);

                return _instance;
            }
        }
        
        [Header("Configuration")]
        [SerializeField] private RequestQueueManagerConfig config;
        [SerializeField] private RequestConfigCollection configCollection;
        
        [Header("Batching Strategy Settings")]
        [SerializeField] private BatchingStrategyType defaultBatchingStrategy = BatchingStrategyType.Adaptive;
        
        private RequestQueueManager _requestQueueManager;
        private CancellationTokenSource _lifecycleCts;
        
        public event Action<QueueStatistics> OnStatisticsUpdated
        {
            add => this._requestQueueManager.OnStatisticsUpdated += value;
            remove => this._requestQueueManager.OnStatisticsUpdated -= value;
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            this.InitializeQueueManager();
        }
        
        private void Start()
        {
            this.StartQueueManagerAsync().Forget();
        }
        
        private void OnDestroy()
        {
            this._lifecycleCts?.Cancel();
            this._lifecycleCts?.Dispose();
            this._requestQueueManager?.Dispose();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                this.SaveOfflineQueueAsync().Forget();
            }
        }
        
        private void OnApplicationQuit()
        {
            this.SaveOfflineQueueAsync().Forget();
        }
        
        /// <summary>
        /// Enqueue request với automatic JSON serialization
        /// </summary>
        public void EnqueueRequest(string endpoint, object data, RequestConfig config,
            Action<bool, string> callback = null)
        {
            this._requestQueueManager?.EnqueueRequest(endpoint, data, config, callback);
        }
        
        /// <summary>
        /// Enqueue request với pre-serialized JSON
        /// </summary>
        public void EnqueueRequestRaw(string endpoint, string jsonBody, RequestConfig config,
            Action<bool, string> callback = null)
        {
            this._requestQueueManager?.EnqueueRequestRaw(endpoint, jsonBody, config, callback);
        }
        
        /// <summary>
        /// Đăng ký batching strategy cho endpoint cụ thể
        /// </summary>
        public void RegisterBatchingStrategy(string endpoint, IBatchingStrategy strategy)
        {
            this._requestQueueManager?.RegisterBatchingStrategy(endpoint, strategy);
        }
        
        /// <summary>
        /// Lấy thống kê hiện tại của queue
        /// </summary>
        public QueueStatistics GetStatistics()
        {
            return this._requestQueueManager?.GetStatistics() ?? default;
        }
        
        /// <summary>
        /// Xóa tất cả queues và batches
        /// </summary>
        public async UniTask ClearAllAsync()
        {
            if (this._requestQueueManager != null)
            {
                await this._requestQueueManager.ClearAllAsync();
            }
        }
        
        private void InitializeQueueManager()
        {
            if (this.config == null)
            {
                Debug.LogError("[RequestQueueManagerBehaviour] Config is null! Please assign a config in Inspector.");
                return;
            }
            
            if (this.configCollection == null)
            {
                Debug.LogError("[RequestQueueManagerBehaviour] ConfigCollection is null! Please assign a config collection in Inspector.");
                return;
            }
            
            var requestQueue = new PriorityRequestQueue(this.config.maxQueueSize);
            
            var rateLimiter = new RateLimiter(
                this.config.maxRequestsPerSecond,
                this.config.maxRequestsPerMinute,
                this.config.rateLimitCooldown
            );
            
            var networkMonitor = new OnlineCheckNetworkMonitor();
            
            var requestSender = new HttpRequestSender(this._optimizedWebRequestService.HttpClient);
            
            var offlineStorage = new JsonOfflineQueueStorage(
                this.config.offlineQueueKey,
                this.config.maxOfflineQueueSize,
                this.configCollection
            );
            
            var batchingStrategies = this.CreateBatchingStrategies();
            
            this._requestQueueManager = new RequestQueueManager(
                this.config,
                requestQueue,
                rateLimiter,
                networkMonitor,
                requestSender,
                offlineStorage,
                batchingStrategies
            );
            
            Debug.Log("[RequestQueueManagerBehaviour] Queue Manager initialized successfully");
        }
        
        private async UniTaskVoid StartQueueManagerAsync()
        {
            if (this._requestQueueManager == null)
            {
                Debug.LogError("[RequestQueueManagerBehaviour] Queue Manager is null! Cannot start.");
                return;
            }
            
            this._lifecycleCts = new System.Threading.CancellationTokenSource();
            
            try
            {
                await this._requestQueueManager.StartAsync(this._lifecycleCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[RequestQueueManagerBehaviour] Queue Manager stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RequestQueueManagerBehaviour] Error starting Queue Manager: {ex.Message}");
            }
        }
        
        private async UniTaskVoid SaveOfflineQueueAsync()
        {
            try
            {
                var stats = this.GetStatistics();
                Debug.Log($"[RequestQueueManagerBehaviour] Saving state: {stats}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RequestQueueManagerBehaviour] Error saving offline queue: {ex.Message}");
            }
            
            await UniTask.CompletedTask;
        }
        
        private Dictionary<string, IBatchingStrategy> CreateBatchingStrategies()
        {
            var strategies = new Dictionary<string, IBatchingStrategy>();
            
            foreach (var requestConfig in this.configCollection.requestConfigs)
            {
                if (!requestConfig.canBatch)
                {
                    continue;
                }
                
                IBatchingStrategy strategy = this.defaultBatchingStrategy switch
                {
                    BatchingStrategyType.TimeBased => new TimeBasedBatchingStrategy(
                        requestConfig.maxBatchSize,
                        requestConfig.maxBatchDelay
                    ),
                    BatchingStrategyType.SizeBased => new SizeBasedBatchingStrategy(
                        requestConfig.maxBatchSize,
                        requestConfig.maxBatchDelay
                    ),
                    BatchingStrategyType.Adaptive => new AdaptiveBatchingStrategy(
                        requestConfig.maxBatchSize,
                        requestConfig.maxBatchDelay
                    ),
                    BatchingStrategyType.PriorityAware => new PriorityAwareBatchingStrategy(
                        requestConfig.maxBatchSize,
                        requestConfig.maxBatchDelay
                    ),
                    _ => new AdaptiveBatchingStrategy(
                        requestConfig.maxBatchSize,
                        requestConfig.maxBatchDelay
                    )
                };
                
                var endpointKey = $"{requestConfig.priority}";
                strategies[endpointKey] = strategy;
            }
            
            return strategies;
        }
    }
    
    /// <summary>
    /// Enum định nghĩa các loại batching strategy có sẵn
    /// </summary>
    public enum BatchingStrategyType
    {
        TimeBased,
        SizeBased,
        Adaptive,
        PriorityAware
    }
}

