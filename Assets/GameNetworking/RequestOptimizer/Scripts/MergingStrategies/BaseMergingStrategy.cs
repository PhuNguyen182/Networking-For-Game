using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Configuration;
using GameNetworking.RequestOptimizer.Scripts.Interfaces;
using GameNetworking.RequestOptimizer.Scripts.Models;

namespace GameNetworking.RequestOptimizer.Scripts.MergingStrategies
{
    /// <summary>
    /// Base abstract class cho merging strategy với default implementation
    /// </summary>
    public abstract class BaseMergingStrategy : IRequestMergingStrategy
    {
        protected readonly float maxMergeDelay;
        
        protected BaseMergingStrategy(float maxMergeDelay)
        {
            this.maxMergeDelay = maxMergeDelay;
        }
        
        public virtual bool CanMerge(QueuedRequest newRequest, IReadOnlyList<QueuedRequest> existingRequests)
        {
            if (existingRequests.Count == 0)
            {
                return true;
            }
            
            // Kiểm tra xem merge key có giống nhau không
            var newKey = this.GetMergeKey(newRequest);
            var firstKey = this.GetMergeKey(existingRequests[0]);
            
            return newKey == firstKey;
        }
        
        public abstract UniTask<QueuedRequest> MergeRequestsAsync(IReadOnlyList<QueuedRequest> requests);
        
        public virtual async UniTask ProcessMergedResponseAsync(
            IReadOnlyList<QueuedRequest> originalRequests,
            string mergedResponse,
            bool success)
        {
            await UniTask.SwitchToMainThread();
            
            // Default: gọi tất cả callbacks với cùng response
            foreach (var request in originalRequests)
            {
                request.Callback?.Invoke(success, mergedResponse);
            }
        }
        
        public abstract string GetMergeKey(QueuedRequest request);
        
        public float MaxMergeDelay => this.maxMergeDelay;
    }
}

