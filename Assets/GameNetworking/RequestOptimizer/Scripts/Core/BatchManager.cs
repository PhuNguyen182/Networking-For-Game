using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Batch Manager - quản lý batch buffers và batching strategies
    /// Tách responsibility từ RequestQueueManager (Single Responsibility Principle)
    /// </summary>
    public class BatchManager
    {
        private readonly Dictionary<string, IBatchingStrategy> _batchingStrategies;
        private readonly Dictionary<string, List<QueuedRequest>> _batchBuffers;
        private readonly Dictionary<string, float> _batchTimers;
        
        public BatchManager(Dictionary<string, IBatchingStrategy> batchingStrategies = null)
        {
            this._batchingStrategies = batchingStrategies ?? new Dictionary<string, IBatchingStrategy>();
            this._batchBuffers = new Dictionary<string, List<QueuedRequest>>();
            this._batchTimers = new Dictionary<string, float>();
        }
        
        /// <summary>
        /// Đăng ký batching strategy cho endpoint
        /// </summary>
        public void RegisterStrategy(string endpoint, IBatchingStrategy strategy)
        {
            this._batchingStrategies[endpoint] = strategy;
        }
        
        /// <summary>
        /// Thêm request vào batch buffer
        /// </summary>
        /// <returns>True nếu thêm thành công, False nếu cần gửi batch ngay</returns>
        public bool AddToBatch(QueuedRequest request)
        {
            if (!this.TryGetStrategy(request.endpoint, out var strategy))
            {
                return false;
            }
            
            if (!this._batchBuffers.ContainsKey(request.endpoint))
            {
                this._batchBuffers[request.endpoint] = new List<QueuedRequest>();
                this._batchTimers[request.endpoint] = Time.realtimeSinceStartup;
            }
            
            var batch = this._batchBuffers[request.endpoint];
            
            if (strategy.CanAddToBatch(request, batch))
            {
                batch.Add(request);
                return true;
            }
            
            // Batch đầy, cần gửi ngay
            return false;
        }
        
        /// <summary>
        /// Lấy danh sách các batches sẵn sàng gửi
        /// </summary>
        public List<string> GetReadyBatches()
        {
            var readyBatches = new List<string>();
            
            foreach (var kvp in this._batchBuffers)
            {
                var endpoint = kvp.Key;
                var batch = kvp.Value;
                
                if (batch.Count == 0)
                {
                    continue;
                }
                
                if (!this.TryGetStrategy(endpoint, out var strategy))
                {
                    continue;
                }
                
                var firstBatchTime = this._batchTimers[endpoint];
                
                if (strategy.ShouldSendBatch(batch, firstBatchTime))
                {
                    readyBatches.Add(endpoint);
                }
            }
            
            return readyBatches;
        }
        
        /// <summary>
        /// Lấy batch requests cho endpoint và clear buffer
        /// </summary>
        public List<QueuedRequest> ExtractBatch(string endpoint)
        {
            if (!this._batchBuffers.TryGetValue(endpoint, out var batch))
            {
                return new List<QueuedRequest>();
            }
            
            var result = new List<QueuedRequest>(batch);
            this._batchBuffers[endpoint] = new List<QueuedRequest>();
            this._batchTimers[endpoint] = Time.realtimeSinceStartup;
            
            return result;
        }
        
        /// <summary>
        /// Tạo batch request từ danh sách requests
        /// </summary>
        public async UniTask<QueuedRequest> CreateBatchRequestAsync(string endpoint, List<QueuedRequest> requests)
        {
            if (!this.TryGetStrategy(endpoint, out var strategy))
            {
                throw new InvalidOperationException($"No batching strategy found for endpoint: {endpoint}");
            }
            
            return await strategy.CreateBatchRequestAsync(requests);
        }
        
        /// <summary>
        /// Xử lý batch response
        /// </summary>
        public async UniTask ProcessBatchResponseAsync(string endpoint, List<QueuedRequest> requests, 
            bool success, string response)
        {
            if (!this.TryGetStrategy(endpoint, out var strategy))
            {
                // Fallback: gọi tất cả callbacks
                foreach (var request in requests)
                {
                    request.Callback?.Invoke(success, response);
                }
                return;
            }
            
            await strategy.ProcessBatchResponseAsync(requests, success, response);
        }
        
        /// <summary>
        /// Lấy tổng số requests đang trong batch buffers
        /// </summary>
        public int GetTotalBatchedCount()
        {
            var total = 0;
            foreach (var batch in this._batchBuffers.Values)
            {
                total += batch.Count;
            }
            return total;
        }
        
        /// <summary>
        /// Clear tất cả batch buffers
        /// </summary>
        public void Clear()
        {
            this._batchBuffers.Clear();
            this._batchTimers.Clear();
        }
        
        private bool TryGetStrategy(string endpoint, out IBatchingStrategy strategy)
        {
            return this._batchingStrategies.TryGetValue(endpoint, out strategy);
        }
    }
}

