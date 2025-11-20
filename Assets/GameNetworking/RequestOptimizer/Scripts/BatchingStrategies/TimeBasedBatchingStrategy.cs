using System.Collections.Generic;
using GameNetworking.RequestOptimizer.Scripts.Models;
using UnityEngine;

namespace GameNetworking.RequestOptimizer.Scripts.BatchingStrategies
{
    /// <summary>
    /// Chiến lược batching dựa trên thời gian - ưu tiên gửi sau một khoảng thời gian nhất định
    /// Phù hợp cho analytics, telemetry events
    /// </summary>
    public class TimeBasedBatchingStrategy : BaseBatchingStrategy
    {
        private readonly float _minBatchDelay;
        
        public TimeBasedBatchingStrategy(int maxBatchingSize, float maxBatchDelay, float minBatchDelay = 1f) 
            : base(maxBatchingSize, maxBatchDelay)
        {
            this._minBatchDelay = minBatchDelay;
        }
        
        public override bool ShouldSendBatch(IReadOnlyList<QueuedRequest> batch, float firstBatchTime)
        {
            if (batch.Count == 0)
            {
                return false;
            }
            
            var elapsedTime = Time.realtimeSinceStartup - firstBatchTime;
            
            if (elapsedTime < this._minBatchDelay)
            {
                return false;
            }
            
            if (batch.Count >= this.MaxBatchingSize)
            {
                return true;
            }
            
            return elapsedTime >= this.MaxBatchingDelay;
        }
    }
}

