using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.BatchingStrategies
{
    /// <summary>
    /// Chiến lược batching có nhận biết priority - chỉ batch requests cùng priority
    /// Phù hợp cho mixed priority workloads
    /// </summary>
    public class PriorityAwareBatchingStrategy : BaseBatchingStrategy
    {
        private readonly Dictionary<RequestPriority, float> _priorityDelays;
        
        public PriorityAwareBatchingStrategy(int maxBatchingSize, float maxBatchDelay) 
            : base(maxBatchingSize, maxBatchDelay)
        {
            this._priorityDelays = new Dictionary<RequestPriority, float>
            {
                { RequestPriority.Critical, 0.1f },
                { RequestPriority.High, 0.5f },
                { RequestPriority.Normal, 2f },
                { RequestPriority.Low, 5f },
                { RequestPriority.Batch, maxBatchDelay }
            };
        }
        
        protected override bool AreRequestsCompatible(QueuedRequest request1, QueuedRequest request2)
        {
            return base.AreRequestsCompatible(request1, request2) &&
                   request1.priority == request2.priority;
        }
        
        public override bool ShouldSendBatch(IReadOnlyList<QueuedRequest> batch, float firstBatchTime)
        {
            if (batch.Count == 0)
            {
                return false;
            }
            
            var firstRequest = batch[0];
            var priorityDelay = this._priorityDelays[firstRequest.priority];
            
            if (batch.Count >= this.MaxBatchingSize)
            {
                return true;
            }
            
            var elapsedTime = Time.realtimeSinceStartup - firstBatchTime;
            return elapsedTime >= priorityDelay;
        }
        
        public override async UniTask ProcessBatchResponseAsync(IReadOnlyList<QueuedRequest> requests, bool success, string response)
        {
            await UniTask.SwitchToMainThread();
            
            var groupedByPriority = requests
                .GroupBy(r => r.priority)
                .OrderBy(g => (int)g.Key);
            
            foreach (var group in groupedByPriority)
            {
                foreach (var request in group)
                {
                    request.Callback?.Invoke(success, response);
                }
            }
        }
    }
}

