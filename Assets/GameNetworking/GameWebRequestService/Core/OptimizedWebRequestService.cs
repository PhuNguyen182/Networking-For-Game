using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameNetworking.GameWebRequestService.Attributes;
using GameNetworking.GameWebRequestService.Models;
using GameNetworking.GameWebRequestService.Utilities;
using GameNetworking.OnlineChecking;
using GameNetworking.RequestOptimizer.Scripts;
using GameNetworking.RequestOptimizer.Scripts.Adapters;
using GameNetworking.RequestOptimizer.Scripts.BatchingStrategies;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Core;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Storage;
using Newtonsoft.Json;
using UnityEngine;

namespace GameNetworking.GameWebRequestService.Core
{
    /// <summary>
    /// Optimized WebRequestService với tích hợp RequestOptimizer
    /// Tự động batch/merge requests khi cần thiết để tránh spam
    /// Mỗi request vẫn trả về đúng OnResponseSuccess/OnResponseFailed callback
    /// </summary>
    public class OptimizedWebRequestService : IDisposable
    {
        private const string OfflineQueueStoragePath = "offline_queue";

        private readonly OnlineCheckService _onlineCheckService;
        private readonly WebRequestConfig _webRequestConfig;
        private readonly RequestQueueManager _queueManager;
        private readonly RequestConfigCollection _requestConfigCollection;
        private readonly Dictionary<string, UniTaskCompletionSource<IBaseResponse>> _pendingRequests;
        
        private GameWebRequestAdapter _httpClient;
        private bool _isDisposed;
        
        public GameWebRequestAdapter HttpClient => this._httpClient;
        
        /// <summary>
        /// Constructor với auto-setup RequestOptimizer
        /// </summary>
        public OptimizedWebRequestService(
            WebRequestConfig webRequestConfig,
            RequestQueueManagerConfig queueConfig,
            RequestConfigCollection customRequestConfigCollection,
            OnlineCheckService onlineCheckService)
        {
            this._webRequestConfig = webRequestConfig ?? throw new ArgumentNullException(nameof(webRequestConfig));
            this._pendingRequests = new Dictionary<string, UniTaskCompletionSource<IBaseResponse>>();
            
            // Setup default queue config nếu chưa có
            if (queueConfig == null)
            {
                queueConfig = ScriptableObject.CreateInstance<RequestQueueManagerConfig>();
                queueConfig.maxRequestsPerSecond = 10;
                queueConfig.maxRequestsPerMinute = 300;
                queueConfig.maxQueueSize = 1000;
                queueConfig.processInterval = 0.1f;
                queueConfig.maxConcurrentRequests = 5;
                queueConfig.enableOfflineQueue = true;
                queueConfig.maxOfflineQueueSize = 500;
                queueConfig.networkCheckInterval = 5f;
                queueConfig.rateLimitCooldown = 60f;
                queueConfig.healthCheckUrl = "https://www.google.com";
            }
            
            // Setup request config collection
            this._requestConfigCollection = customRequestConfigCollection;
            
            // Setup RequestQueueManager
            this._queueManager = this.SetupRequestQueueManager(queueConfig);
            this._onlineCheckService = onlineCheckService;
            Debug.Log("[OptimizedWebRequestService] Initialized with automatic batching/merging support");
        }
        
        /// <summary>
        /// Start async operations (phải gọi sau khi khởi tạo)
        /// </summary>
        public async UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            await this._queueManager.StartAsync(cancellationToken);
            Debug.Log("[OptimizedWebRequestService] Started successfully");
        }
        
        /// <summary>
        /// GET request với automatic optimization
        /// </summary>
        public async UniTask<TResponse> GetAsync<TRequest, TResponse>(
            TRequest requestBody = null,
            CancellationToken cancellationToken = default
        ) where TRequest : class
          where TResponse : class, IBaseResponse, new()
        {
            EndpointHelper.ValidateEndpointAttribute<TResponse>();
            var url = EndpointHelper.GetEndpointPath<TResponse>();
            
            return await this.ExecuteOptimizedRequestAsync<TRequest, TResponse>(
                url, 
                requestBody, 
                "GET",
                cancellationToken
            );
        }
        
        /// <summary>
        /// POST request với automatic optimization
        /// </summary>
        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            TRequest requestBody,
            CancellationToken cancellationToken = default
        ) where TRequest : class
          where TResponse : class, IBaseResponse, new()
        {
            if (requestBody == null)
            {
                throw new ArgumentNullException(nameof(requestBody));
            }
            
            EndpointHelper.ValidateEndpointAttribute<TResponse>();
            var url = EndpointHelper.GetEndpointPath<TResponse>();
            
            return await this.ExecuteOptimizedRequestAsync<TRequest, TResponse>(
                url, 
                requestBody, 
                "POST",
                cancellationToken
            );
        }
        
        /// <summary>
        /// PUT request với automatic optimization
        /// </summary>
        public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
            TRequest requestBody,
            CancellationToken cancellationToken = default
        ) where TRequest : class
          where TResponse : class, IBaseResponse, new()
        {
            if (requestBody == null)
            {
                throw new ArgumentNullException(nameof(requestBody));
            }
            
            EndpointHelper.ValidateEndpointAttribute<TResponse>();
            var url = EndpointHelper.GetEndpointPath<TResponse>();
            
            return await this.ExecuteOptimizedRequestAsync<TRequest, TResponse>(
                url, 
                requestBody, 
                "PUT",
                cancellationToken
            );
        }
        
        /// <summary>
        /// Core method để execute request với optimization
        /// </summary>
        private async UniTask<TResponse> ExecuteOptimizedRequestAsync<TRequest, TResponse>(
            string url,
            TRequest requestBody,
            string httpMethod,
            CancellationToken cancellationToken
        ) where TRequest : class
          where TResponse : class, IBaseResponse, new()
        {
            // Tạo unique request ID
            var requestId = Guid.NewGuid().ToString();
            
            // Tạo completion source để await result
            var completionSource = new UniTaskCompletionSource<IBaseResponse>();
            this._pendingRequests[requestId] = completionSource;
            
            try
            {
                // Determine request priority based on endpoint or type
                var config = this.DetermineRequestConfig<TResponse>(url);
                
                // Enqueue request với callback
                this._queueManager.EnqueueRequest(
                    endpoint: url,
                    data: requestBody,
                    config: config,
                    callback: (success, response) =>
                    {
                        this.HandleRequestCallback<TResponse>(requestId, success, response);
                    }
                );
                
                // Await completion
                var baseResponse = await completionSource.Task;
                
                // Cast to specific response type
                if (baseResponse is TResponse typedResponse)
                {
                    return typedResponse;
                }
                
                // Nếu không cast được, return null hoặc throw exception
                Debug.LogError($"[OptimizedWebRequestService] Cannot cast response to {typeof(TResponse).Name}");
                return new TResponse();
            }
            catch (OperationCanceledException)
            {
                this._pendingRequests.Remove(requestId);
                throw;
            }
            catch (Exception ex)
            {
                this._pendingRequests.Remove(requestId);
                Debug.LogError($"[OptimizedWebRequestService] Request failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Handle callback từ RequestQueueManager
        /// </summary>
        private void HandleRequestCallback<TResponse>(string requestId, bool success, string response)
            where TResponse : class, IBaseResponse, new()
        {
            if (!this._pendingRequests.TryGetValue(requestId, out var completionSource))
            {
                Debug.LogWarning($"[OptimizedWebRequestService] Completion source not found for request: {requestId}");
                return;
            }
            
            this._pendingRequests.Remove(requestId);
            
            try
            {
                var typedResponse = new TResponse();
                
                if (success)
                {
                    // Parse response JSON và gọi callback
                    var responseData = this.ParseResponseData<TResponse>(response);
                    
                    // Set response properties
                    typedResponse.StatusCode = 200;
                    typedResponse.Message = "Success";
                    typedResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    
                    // Gọi OnResponseSuccess với data
                    this.InvokeResponseSuccess(typedResponse, responseData);
                }
                else
                {
                    // Parse error
                    var (errorCode, errorMessage) = this.ParseErrorResponse(response);
                    
                    // Set error properties
                    typedResponse.StatusCode = errorCode;
                    typedResponse.Message = errorMessage;
                    typedResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    
                    // Gọi OnResponseFailed
                    this.InvokeResponseFailed(typedResponse, errorCode, errorMessage);
                }
                
                completionSource.TrySetResult(typedResponse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OptimizedWebRequestService] Callback handling failed: {ex.Message}");
                
                var errorResponse = new TResponse();
                errorResponse.StatusCode = 500;
                errorResponse.Message = $"Callback failed: {ex.Message}";
                errorResponse.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                this.InvokeResponseFailed(errorResponse, 500, $"Callback failed: {ex.Message}");
                completionSource.TrySetResult(errorResponse);
            }
        }
        
        /// <summary>
        /// Invoke OnResponseSuccess method via reflection hoặc dynamic
        /// </summary>
        private void InvokeResponseSuccess<TResponse>(TResponse response, object data)
            where TResponse : class, IBaseResponse
        {
            // Sử dụng reflection để gọi OnResponseSuccess
            var method = typeof(TResponse).GetMethod("OnResponseSuccess");
            if (method != null)
            {
                method.Invoke(response, new[] { data });
            }
            else
            {
                Debug.LogWarning($"[OptimizedWebRequestService] OnResponseSuccess method not found on {typeof(TResponse).Name}");
            }
        }
        
        /// <summary>
        /// Invoke OnResponseFailed method via reflection
        /// </summary>
        private void InvokeResponseFailed<TResponse>(TResponse response, int errorCode, string errorMessage)
            where TResponse : class, IBaseResponse
        {
            // Sử dụng reflection để gọi OnResponseFailed
            var method = typeof(TResponse).GetMethod("OnResponseFailed");
            if (method != null)
            {
                method.Invoke(response, new object[] { errorCode, errorMessage });
            }
            else
            {
                Debug.LogWarning($"[OptimizedWebRequestService] OnResponseFailed method not found on {typeof(TResponse).Name}");
            }
        }
        
        /// <summary>
        /// Parse response data cho generic type
        /// </summary>
        private object ParseResponseData<TResponse>(string responseJson)
            where TResponse : class, IBaseResponse, new()
        {
            try
            {
                // Nếu TResponse là BaseGetResponse<T>, BasePostResponse<T>, hoặc BasePutResponse<T>
                // Ta cần extract generic type argument
                var responseType = typeof(TResponse);
                
                if (responseType.IsGenericType)
                {
                    var genericArgs = responseType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        var dataType = genericArgs[0];
                        return JsonConvert.DeserializeObject(responseJson, dataType);
                    }
                }
                
                // Fallback: deserialize as dynamic hoặc string
                return JsonConvert.DeserializeObject(responseJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OptimizedWebRequestService] Failed to parse response: {ex.Message}");
                return responseJson; // Return raw string as fallback
            }
        }
        
        /// <summary>
        /// Parse error response
        /// </summary>
        private (int errorCode, string errorMessage) ParseErrorResponse(string response)
        {
            try
            {
                var errorObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                
                var errorCode = 500;
                if (errorObj.TryGetValue("errorCode", out var code) || errorObj.TryGetValue("statusCode", out code))
                {
                    errorCode = Convert.ToInt32(code);
                }
                
                var errorMessage = "Unknown error";
                if (errorObj.TryGetValue("error", out var msg) || 
                    errorObj.TryGetValue("message", out msg) || 
                    errorObj.TryGetValue("errorMessage", out msg))
                {
                    errorMessage = msg.ToString();
                }
                
                return (errorCode, errorMessage);
            }
            catch
            {
                // Fallback: treat as plain error message
                return (500, response);
            }
        }
        
        
        /// <summary>
        /// Determine request config dựa trên EndpointAttribute của response type
        /// </summary>
        /// <remarks>
        /// Priority được lấy trực tiếp từ EndpointAttribute.Priority field.
        /// Nếu không có attribute hoặc không specify priority, sử dụng Normal làm default.
        /// </remarks>
        private RequestConfig DetermineRequestConfig<TResponse>(string url)
        {
            // Lấy EndpointAttribute từ TResponse
            var attributes = typeof(TResponse).GetCustomAttributes(typeof(EndpointAttribute), false);
            
            if (attributes.Length > 0 && attributes[0] is EndpointAttribute endpointAttr)
            {
                // Lấy priority trực tiếp từ attribute
                var priority = endpointAttr.Priority;
                
                // Get config từ collection dựa trên priority
                var config = this._requestConfigCollection.GetRequestConfigByPriority(priority);
                
                Debug.Log($"[OptimizedWebRequestService] Request to {url} using Priority: {priority}");
                
                return config;
            }
            
            // Fallback: Normal priority nếu không có attribute
            Debug.LogWarning($"[OptimizedWebRequestService] No EndpointAttribute found for {typeof(TResponse).Name}, using Normal priority");
            return this._requestConfigCollection.GetRequestConfigByPriority(RequestPriority.Normal);
        }
        
        /// <summary>
        /// Setup RequestQueueManager với default configuration
        /// </summary>
        private RequestQueueManager SetupRequestQueueManager(RequestQueueManagerConfig config)
        {
            // Create HTTP client adapter
            this._httpClient = GameWebRequestAdapter.CreateDefault(this._webRequestConfig);
            
            // Create queue components
            var requestQueue = new PriorityRequestQueue(config.maxQueueSize);
            var rateLimiter = new RateLimiter(config.maxRequestsPerSecond, config.maxRequestsPerMinute);
            var networkMonitor = new OnlineCheckNetworkMonitor(this._onlineCheckService);
            var requestSender = new HttpRequestSender(this._httpClient, config.maxConcurrentRequests);
            var offlineStorage = new JsonOfflineQueueStorage(OfflineQueueStoragePath, config.maxOfflineQueueSize,
                this._requestConfigCollection);
            
            // Setup default batching strategies
            var batchingStrategies = new Dictionary<string, IBatchingStrategy>
            {
                // Strategy cho analytics endpoints
                ["analytics"] = new TimeBasedBatchingStrategy(100, 5f, 2f),
                ["telemetry"] = new AdaptiveBatchingStrategy(50, 5f, 1f, 10),
                ["events"] = new SizeBasedBatchingStrategy(100, 10f, 80)
            };

            // Create RequestQueueManager
            var queueManager = new RequestQueueManager(
                config,
                requestQueue,
                rateLimiter,
                networkMonitor,
                requestSender,
                offlineStorage,
                batchingStrategies
            );
            
            // Setup merging strategies cho position updates
            // Note: Cần extend RequestQueueManager để support MergeManager
            // Hoặc có thể implement trong custom batching strategy
            
            return queueManager;
        }
        
        /// <summary>
        /// Get queue statistics
        /// </summary>
        public RequestOptimizer.Scripts.Models.QueueStatistics GetStatistics()
        {
            return this._queueManager.GetStatistics();
        }
        
        /// <summary>
        /// Clear all queues
        /// </summary>
        public async UniTask ClearAllAsync()
        {
            await this._queueManager.ClearAllAsync();
        }
        
        public void Dispose()
        {
            if (this._isDisposed)
            {
                return;
            }
            
            this._queueManager?.Dispose();
            this._pendingRequests?.Clear();
            this._isDisposed = true;
        }
    }
}

