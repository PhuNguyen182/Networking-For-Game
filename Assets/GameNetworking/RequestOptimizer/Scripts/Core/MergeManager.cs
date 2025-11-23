using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.Core
{
    /// <summary>
    /// Merge Manager - quản lý request merging với các strategies khác nhau
    /// Tách responsibility từ RequestQueueManager (Single Responsibility Principle)
    /// </summary>
    public class MergeManager
    {
        private readonly Dictionary<string, IRequestMergingStrategy> _mergingStrategies;
        private readonly Dictionary<string, List<QueuedRequest>> _mergeBuffers;
        private readonly Dictionary<string, float> _mergeTimers;
        
        public MergeManager()
        {
            this._mergingStrategies = new Dictionary<string, IRequestMergingStrategy>();
            this._mergeBuffers = new Dictionary<string, List<QueuedRequest>>();
            this._mergeTimers = new Dictionary<string, float>();
        }
        
        /// <summary>
        /// Đăng ký merging strategy cho endpoint
        /// </summary>
        public void RegisterStrategy(string endpoint, IRequestMergingStrategy strategy)
        {
            this._mergingStrategies[endpoint] = strategy;
        }
        
        /// <summary>
        /// Kiểm tra xem request có thể merge không
        /// </summary>
        public bool CanMerge(QueuedRequest request)
        {
            return this.TryGetStrategy(request.endpoint, out _);
        }
        
        /// <summary>
        /// Thêm request vào merge buffer
        /// </summary>
        /// <returns>True nếu thêm thành công, False nếu cần gửi merge ngay</returns>
        public bool AddToMerge(QueuedRequest request)
        {
            if (!this.TryGetStrategy(request.endpoint, out var strategy))
            {
                return false;
            }
            
            var mergeKey = strategy.GetMergeKey(request);
            
            if (!this._mergeBuffers.ContainsKey(mergeKey))
            {
                this._mergeBuffers[mergeKey] = new List<QueuedRequest>();
                this._mergeTimers[mergeKey] = Time.realtimeSinceStartup;
            }
            
            var mergeBuffer = this._mergeBuffers[mergeKey];
            
            if (strategy.CanMerge(request, mergeBuffer))
            {
                mergeBuffer.Add(request);
                return true;
            }
            
            // Không thể merge, cần gửi ngay
            return false;
        }
        
        /// <summary>
        /// Lấy danh sách các merges sẵn sàng gửi
        /// </summary>
        public List<string> GetReadyMerges()
        {
            var readyMerges = new List<string>();
            
            foreach (var kvp in this._mergeBuffers)
            {
                var mergeKey = kvp.Key;
                var mergeBuffer = kvp.Value;
                
                if (mergeBuffer.Count == 0)
                {
                    continue;
                }
                
                var firstRequest = mergeBuffer[0];
                if (!this.TryGetStrategy(firstRequest.endpoint, out var strategy))
                {
                    continue;
                }
                
                var firstMergeTime = this._mergeTimers[mergeKey];
                var elapsedTime = Time.realtimeSinceStartup - firstMergeTime;
                
                if (elapsedTime >= strategy.MaxMergeDelay)
                {
                    readyMerges.Add(mergeKey);
                }
            }
            
            return readyMerges;
        }
        
        /// <summary>
        /// Lấy merge buffer và clear
        /// </summary>
        public List<QueuedRequest> ExtractMerge(string mergeKey)
        {
            if (!this._mergeBuffers.TryGetValue(mergeKey, out var mergeBuffer))
            {
                return new List<QueuedRequest>();
            }
            
            var result = new List<QueuedRequest>(mergeBuffer);
            this._mergeBuffers[mergeKey] = new List<QueuedRequest>();
            this._mergeTimers[mergeKey] = Time.realtimeSinceStartup;
            
            return result;
        }
        
        /// <summary>
        /// Merge requests thành 1 request duy nhất
        /// </summary>
        public async UniTask<QueuedRequest> MergeRequestsAsync(List<QueuedRequest> requests)
        {
            if (requests.Count == 0)
            {
                return null;
            }
            
            if (requests.Count == 1)
            {
                return requests[0];
            }
            
            var firstRequest = requests[0];
            if (!this.TryGetStrategy(firstRequest.endpoint, out var strategy))
            {
                return requests[requests.Count - 1]; // Fallback: lấy request cuối cùng
            }
            
            return await strategy.MergeRequestsAsync(requests);
        }
        
        /// <summary>
        /// Xử lý merged response
        /// </summary>
        public async UniTask ProcessMergedResponseAsync(List<QueuedRequest> originalRequests, 
            string mergedResponse, bool success)
        {
            if (originalRequests.Count == 0)
            {
                return;
            }
            
            var firstRequest = originalRequests[0];
            if (!this.TryGetStrategy(firstRequest.endpoint, out var strategy))
            {
                // Fallback: gọi tất cả callbacks
                foreach (var request in originalRequests)
                {
                    request.Callback?.Invoke(success, mergedResponse);
                }
                return;
            }
            
            await strategy.ProcessMergedResponseAsync(originalRequests, mergedResponse, success);
        }
        
        /// <summary>
        /// Lấy tổng số requests đang trong merge buffers
        /// </summary>
        public int GetTotalMergedCount()
        {
            var total = 0;
            foreach (var buffer in this._mergeBuffers.Values)
            {
                total += buffer.Count;
            }
            return total;
        }
        
        /// <summary>
        /// Clear tất cả merge buffers
        /// </summary>
        public void Clear()
        {
            this._mergeBuffers.Clear();
            this._mergeTimers.Clear();
        }
        
        private bool TryGetStrategy(string endpoint, out IRequestMergingStrategy strategy)
        {
            return this._mergingStrategies.TryGetValue(endpoint, out strategy);
        }
    }
}

