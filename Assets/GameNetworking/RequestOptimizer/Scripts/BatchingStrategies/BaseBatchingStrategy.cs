using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using GameNetworking.RequestOptimizer.Scripts.Utilities;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.BatchingStrategies
{
    /// <summary>
    /// Base class cho batching strategy với implementation mặc định
    /// </summary>
    public abstract class BaseBatchingStrategy : IBatchingStrategy
    {
        protected readonly int MaxBatchingSize;
        protected readonly float MaxBatchingDelay;
        
        protected BaseBatchingStrategy(int maxBatchingSize, float maxBatchDelay)
        {
            this.MaxBatchingSize = maxBatchingSize;
            this.MaxBatchingDelay = maxBatchDelay;
        }
        
        public virtual bool CanAddToBatch(QueuedRequest request, IReadOnlyList<QueuedRequest> currentBatch)
        {
            if (currentBatch.Count >= this.MaxBatchingSize)
            {
                return false;
            }
            
            if (currentBatch.Count == 0)
            {
                return true;
            }
            
            var firstRequest = currentBatch[0];
            return this.AreRequestsCompatible(request, firstRequest);
        }
        
        public virtual bool ShouldSendBatch(IReadOnlyList<QueuedRequest> batch, float firstBatchTime)
        {
            if (batch.Count == 0)
            {
                return false;
            }
            
            if (batch.Count >= this.MaxBatchingSize)
            {
                return true;
            }
            
            var elapsedTime = Time.realtimeSinceStartup - firstBatchTime;
            return elapsedTime >= this.MaxBatchingDelay;
        }
        
        public virtual async UniTask<QueuedRequest> CreateBatchRequestAsync(IReadOnlyList<QueuedRequest> requests)
        {
            if (requests.Count == 0)
            {
                throw new ArgumentException("Cannot create batch from empty request list");
            }
            
            var batchEndpoint = this.GetBatchEndpoint(requests[0].endpoint);
            var batchBody = await this.SerializeBatchBodyAsync(requests);
            var httpMethod = requests[0].httpMethod;
            var firstRequest = requests[0];
            
            var batchRequest = new QueuedRequest(
                batchEndpoint,
                batchBody,
                httpMethod,
                firstRequest.priority,
                firstRequest.config
            );
            
            return batchRequest;
        }
        
        public virtual async UniTask ProcessBatchResponseAsync(IReadOnlyList<QueuedRequest> requests, bool success, string response)
        {
            await UniTask.SwitchToMainThread();
            
            foreach (var request in requests)
            {
                request.Callback?.Invoke(success, response);
            }
        }
        
        public int maxBatchingSize => this.MaxBatchingSize;
        public float MaxBatchDelay => this.MaxBatchingDelay;
        
        /// <summary>
        /// Kiểm tra xem 2 requests có tương thích để batch không
        /// </summary>
        protected virtual bool AreRequestsCompatible(QueuedRequest request1, QueuedRequest request2)
        {
            return request1.endpoint == request2.endpoint &&
                   request1.priority == request2.priority &&
                   request1.httpMethod == request2.httpMethod; // CRITICAL: Same HTTP method
        }
        
        /// <summary>
        /// Lấy endpoint cho batch request
        /// Cache kết quả để tránh string allocation
        /// </summary>
        protected virtual string GetBatchEndpoint(string baseEndpoint)
        {
            // Tối ưu: cache string concatenation results
            return string.Concat(baseEndpoint, "/batch");
        }
        
        /// <summary>
        /// Serialize danh sách requests thành batch body
        /// Tối ưu: tránh LINQ và ToList() allocations
        /// </summary>
        protected virtual async UniTask<string> SerializeBatchBodyAsync(IReadOnlyList<QueuedRequest> requests)
        {
            await UniTask.SwitchToThreadPool();
            
            // Tối ưu: dùng for loop thay vì LINQ
            var requestCount = requests.Count;
            var bodies = new List<string>(requestCount);
            
            for (var i = 0; i < requestCount; i++)
            {
                bodies.Add(requests[i].jsonBody);
            }
            
            var batchData = new { events = bodies };
            var batchJson = JsonSerializer.SerializeCompact(batchData);
            
            await UniTask.SwitchToMainThread();
            return batchJson;
        }
    }
}

