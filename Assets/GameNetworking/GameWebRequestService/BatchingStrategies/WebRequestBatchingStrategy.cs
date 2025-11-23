using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.BatchingStrategies;
using GameNetworking.RequestOptimizer.Scripts.Models;
using GameNetworking.RequestOptimizer.Scripts.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameNetworking.GameWebRequestService.BatchingStrategies
{
    /// <summary>
    /// Custom batching strategy cho WebRequestService
    /// Parse batch response và gọi đúng callback cho từng request
    /// </summary>
    public class WebRequestBatchingStrategy : BaseBatchingStrategy
    {
        public WebRequestBatchingStrategy(int maxBatchingSize, float maxBatchDelay) 
            : base(maxBatchingSize, maxBatchDelay)
        {
        }
        
        public override async UniTask ProcessBatchResponseAsync(
            IReadOnlyList<QueuedRequest> requests, 
            bool success, 
            string response)
        {
            await UniTask.SwitchToMainThread();
            
            if (!success || requests.Count == 0)
            {
                // Nếu batch thất bại hoàn toàn, gọi callback failed cho tất cả
                foreach (var request in requests)
                {
                    request.Callback?.Invoke(false, response);
                }
                return;
            }
            
            // Parse batch response
            var batchResult = BatchResponseParser.ParseBatchResponse(response, requests.Count);
            
            if (batchResult.IsFullSuccess)
            {
                // Tất cả thành công - gọi callback success với từng response tương ứng
                await this.ProcessSuccessfulBatchAsync(requests, batchResult);
            }
            else if (batchResult.IsPartialSuccess)
            {
                // Một số thành công, một số thất bại
                await this.ProcessPartialBatchAsync(requests, batchResult);
            }
            else
            {
                // Tất cả thất bại
                await this.ProcessFailedBatchAsync(requests, batchResult);
            }
        }
        
        /// <summary>
        /// Process batch khi tất cả thành công
        /// </summary>
        private async UniTask ProcessSuccessfulBatchAsync(
            IReadOnlyList<QueuedRequest> requests,
            BatchResponseResult batchResult)
        {
            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                
                // Lấy individual result tương ứng
                var individualResult = i < batchResult.results.Count 
                    ? batchResult.results[i] 
                    : null;
                
                if (individualResult != null && individualResult.isSuccess)
                {
                    // Gọi callback success với response data
                    request.Callback?.Invoke(true, individualResult.response);
                }
                else
                {
                    // Fallback: gọi callback success với empty response
                    request.Callback?.Invoke(true, "{}");
                }
            }
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// Process batch khi partial success
        /// </summary>
        private async UniTask ProcessPartialBatchAsync(
            IReadOnlyList<QueuedRequest> requests,
            BatchResponseResult batchResult)
        {
            Debug.LogWarning($"[WebRequestBatchingStrategy] Partial success: {batchResult.successfulRequests}/{batchResult.totalRequests} succeeded");
            
            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                
                // Lấy individual result
                var individualResult = i < batchResult.results.Count 
                    ? batchResult.results[i] 
                    : null;
                
                if (individualResult != null)
                {
                    if (individualResult.isSuccess)
                    {
                        // Success callback
                        request.Callback?.Invoke(true, individualResult.response);
                    }
                    else
                    {
                        // Failed callback với error message
                        var errorJson = JsonConvert.SerializeObject(new
                        {
                            errorCode = individualResult.statusCode,
                            errorMessage = individualResult.errorMessage,
                            error = individualResult.errorMessage
                        });
                        request.Callback?.Invoke(false, errorJson);
                    }
                }
                else
                {
                    // Không có result - treat as failed
                    var errorJson = JsonConvert.SerializeObject(new
                    {
                        errorCode = 500,
                        errorMessage = "No response from server for this request",
                        error = "Missing response"
                    });
                    request.Callback?.Invoke(false, errorJson);
                }
            }
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// Process batch khi tất cả thất bại
        /// </summary>
        private async UniTask ProcessFailedBatchAsync(
            IReadOnlyList<QueuedRequest> requests,
            BatchResponseResult batchResult)
        {
            Debug.LogError($"[WebRequestBatchingStrategy] Batch completely failed");
            
            for (var i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                
                // Lấy individual result nếu có
                var individualResult = i < batchResult.results.Count 
                    ? batchResult.results[i] 
                    : null;
                
                var errorMessage = individualResult?.errorMessage ?? "Batch request failed";
                var statusCode = individualResult?.statusCode ?? 500;
                
                var errorJson = JsonConvert.SerializeObject(new
                {
                    errorCode = statusCode,
                    errorMessage = errorMessage,
                    error = errorMessage
                });
                
                request.Callback?.Invoke(false, errorJson);
            }
            
            await UniTask.Yield();
        }
        
        /// <summary>
        /// Override SerializeBatchBodyAsync để format theo server expectation
        /// </summary>
        protected override async UniTask<string> SerializeBatchBodyAsync(IReadOnlyList<QueuedRequest> requests)
        {
            await UniTask.SwitchToThreadPool();
            
            // Format batch requests theo chuẩn server
            // Server expects: { "requests": [ {...}, {...}, ... ] }
            var requestBodies = new List<JObject>();
            
            for (var i = 0; i < requests.Count; i++)
            {
                try
                {
                    var jsonObj = JObject.Parse(requests[i].jsonBody);
                    requestBodies.Add(jsonObj);
                }
                catch
                {
                    // Nếu parse thất bại, wrap in object
                    requestBodies.Add(new JObject
                    {
                        ["data"] = requests[i].jsonBody
                    });
                }
            }
            
            var batchData = new JObject
            {
                ["requests"] = new JArray(requestBodies)
            };
            
            var batchJson = batchData.ToString(Formatting.None);
            
            await UniTask.SwitchToMainThread();
            return batchJson;
        }
    }
}

